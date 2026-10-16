using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Net;
using Google.Protobuf;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Qyl.Telemetry.SemanticConventions;

namespace Qyl.Telemetry.Testing;

[PublicAPI]
public sealed class OtlpReceiver : IAsyncDisposable
{
    private readonly Lock _gate = new();
    private readonly List<LogRecord> _logs = [];
    private readonly List<MetricRecord> _metrics = [];
    private readonly List<ExportReceipt> _receipts = [];
    private readonly List<SpanRecord> _spans = [];
    private WebApplication _application = null!;
    private TaskCompletionSource _changed = NewSignal();
    private bool _disposed;

    public Uri Endpoint { get; private set; } = null!;

    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _changed.TrySetResult();
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _application.StopAsync(timeout.Token).ConfigureAwait(false);
        }
        finally
        {
            await _application.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static async Task<OtlpReceiver> StartAsync(CancellationToken cancellationToken = default)
    {
        var receiver = new OtlpReceiver();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = 16 * 1024 * 1024;
        });
        receiver._application = builder.Build();
        receiver._application.MapPost("/v1/{signal}", receiver.ReceiveAsync);
        var transferred = false;
        try
        {
            await receiver._application.StartAsync(cancellationToken).ConfigureAwait(false);
            var addresses = receiver._application.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!;
            receiver.Endpoint = new Uri(addresses.Addresses.Single() + "/");
            transferred = true;
            return receiver;
        }
        finally
        {
            if (!transferred) await receiver._application.DisposeAsync().ConfigureAwait(false);
        }
    }

    public TelemetrySnapshot Snapshot()
    {
        lock (_gate)
        {
            return new TelemetrySnapshot(_spans.AsReadOnlySnapshot(), _logs.AsReadOnlySnapshot(),
                _metrics.AsReadOnlySnapshot(), _receipts.AsReadOnlySnapshot());
        }
    }

    public async Task<TelemetrySnapshot> AwaitAsync(Func<TelemetrySnapshot, bool> condition, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        using var deadline = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        while (true)
        {
            Task changed;
            TelemetrySnapshot snapshot;
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                snapshot = Snapshot();
                changed = _changed.Task;
            }

            if (condition(snapshot)) return snapshot;
            try
            {
                await changed.WaitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested &&
                                                     !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "The OTLP receiver did not observe the required evidence before the deadline.");
            }
        }
    }

    private async Task ReceiveAsync(HttpContext context, string signal)
    {
        if (signal is not ("traces" or "logs" or "metrics"))
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (!string.Equals(context.Request.ContentType?.Split(';')[0], "application/x-protobuf",
                StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 415;
            return;
        }

        var encoding = context.Request.Headers.ContentEncoding.ToString();
        if (encoding is not ("" or "gzip"))
        {
            context.Response.StatusCode = 415;
            return;
        }

        try
        {
            await using var input = encoding == "gzip"
                ? new GZipStream(context.Request.Body, CompressionMode.Decompress, true)
                : null;
            using var buffer = new MemoryStream();
            var source = (Stream?)input ?? context.Request.Body;
            var chunk = new byte[8192];
            int read;
            while ((read = await source.ReadAsync(chunk, context.RequestAborted).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + read > 16 * 1024 * 1024)
                {
                    context.Response.StatusCode = 413;
                    return;
                }

                buffer.Write(chunk, 0, read);
            }

            var bytes = buffer.ToArray();
            long accepted = 0, rejected = 0;
            var errors = new List<string>();

            void Add<TInput, TOutput>(TInput sourceRecord, Func<TInput, TOutput> decode, List<TOutput> destination,
                long units = 1)
            {
                try
                {
                    var value = decode(sourceRecord);
                    lock (_gate)
                    {
                        destination.Add(value);
                    }

                    accepted += units;
                }
                catch (InvalidDataException error)
                {
                    rejected += units;
                    errors.Add(error.Message);
                }
            }

            IMessage response;
            switch (signal)
            {
            case "traces":
            {
                foreach (var resource in ExportTraceServiceRequest.Parser.ParseFrom(bytes).ResourceSpans)
                foreach (var scope in resource.ScopeSpans)
                foreach (var span in scope.Spans)
                    Add(span,
                        s => OtlpDecode.Span(s, OtlpDecode.Resource(resource.Resource, resource.SchemaUrl),
                            OtlpDecode.Scope(scope.Scope, scope.SchemaUrl)), _spans);
                response = new ExportTraceServiceResponse
                {
                    PartialSuccess = rejected == 0
                        ? null
                        : new ExportTracePartialSuccess
                            { RejectedSpans = rejected, ErrorMessage = string.Join(" ", errors.Distinct()) }
                };
                break;
            }
            case "logs":
            {
                foreach (var resource in ExportLogsServiceRequest.Parser.ParseFrom(bytes).ResourceLogs)
                foreach (var scope in resource.ScopeLogs)
                foreach (var log in scope.LogRecords)
                    Add(log,
                        l => OtlpDecode.Log(l, OtlpDecode.Resource(resource.Resource, resource.SchemaUrl),
                            OtlpDecode.Scope(scope.Scope, scope.SchemaUrl)), _logs);
                response = new ExportLogsServiceResponse
                {
                    PartialSuccess = rejected == 0
                        ? null
                        : new ExportLogsPartialSuccess
                            { RejectedLogRecords = rejected, ErrorMessage = string.Join(" ", errors.Distinct()) }
                };
                break;
            }
            default:
            {
                foreach (var resource in ExportMetricsServiceRequest.Parser.ParseFrom(bytes).ResourceMetrics)
                foreach (var scope in resource.ScopeMetrics)
                foreach (var metric in scope.Metrics)
                    Add(metric,
                        m => OtlpDecode.Metric(m, OtlpDecode.Resource(resource.Resource, resource.SchemaUrl),
                            OtlpDecode.Scope(scope.Scope, scope.SchemaUrl)), _metrics, PointCount(metric));
                response = new ExportMetricsServiceResponse
                {
                    PartialSuccess = rejected == 0
                        ? null
                        : new ExportMetricsPartialSuccess
                            { RejectedDataPoints = rejected, ErrorMessage = string.Join(" ", errors.Distinct()) }
                };
                break;
            }

            }

            Publish(new ExportReceipt(signal, accepted, rejected,
                errors.Count == 0 ? null : string.Join(" ", errors.Distinct())));
            context.Response.ContentType = "application/x-protobuf";
            await context.Response.Body.WriteAsync(response.ToByteArray(), context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (Exception error) when (error is InvalidProtocolBufferException or InvalidDataException)
        {
            Publish(new ExportReceipt(signal, 0, 0, error.Message));
            context.Response.StatusCode = 400;
        }
    }

    private static int PointCount(Metric metric)
    {
        return metric.DataCase switch
        {
            Metric.DataOneofCase.Gauge => metric.Gauge.DataPoints.Count,
            Metric.DataOneofCase.Sum => metric.Sum.DataPoints.Count,
            Metric.DataOneofCase.Histogram => metric.Histogram.DataPoints.Count,
            Metric.DataOneofCase.ExponentialHistogram => metric.ExponentialHistogram
                .DataPoints.Count,
            Metric.DataOneofCase.Summary => metric.Summary.DataPoints.Count,
            _ => 0
        };
    }

    private void Publish(ExportReceipt receipt)
    {
        lock (_gate)
        {
            _receipts.Add(receipt);
            var changed = _changed;
            _changed = NewSignal();
            changed.TrySetResult();
        }
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

internal static class Snapshots
{
    internal static ReadOnlyCollection<T> AsReadOnlySnapshot<T>(this IEnumerable<T> values)
    {
        return new List<T>(values).AsReadOnly();
    }
}