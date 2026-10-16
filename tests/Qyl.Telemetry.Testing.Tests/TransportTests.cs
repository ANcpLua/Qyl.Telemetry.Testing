using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using JetBrains.Annotations;

namespace Qyl.Telemetry.Testing.Tests;

[TestClass]
public sealed class TransportTests
{
    [UsedImplicitly] public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow("unknown", "application/x-protobuf", null, HttpStatusCode.NotFound)]
    [DataRow("traces", "application/json", null, HttpStatusCode.UnsupportedMediaType)]
    [DataRow("traces", null, null, HttpStatusCode.UnsupportedMediaType)]
    [DataRow("traces", "application/x-protobuf", "br", HttpStatusCode.UnsupportedMediaType)]
    [DataRow("traces", "application/x-protobuf; charset=utf-8", null, HttpStatusCode.OK)]
    public async Task Receiver_routes_and_media_contract_return_the_exact_status(string signal, string? media,
        string? encoding, HttpStatusCode expected)
    {
        await using var receiver = await OtlpReceiver.StartAsync(TestContext.CancellationToken);
        using var content = new ByteArrayContent([]);
        content.Headers.ContentType = media is null ? null : MediaTypeHeaderValue.Parse(media);
        if (encoding is not null) content.Headers.ContentEncoding.Add(encoding);
        using var response = await TelemetryLifecycle.Http.PostAsync(new Uri(receiver.Endpoint, "v1/" + signal),
            content, TestContext.CancellationToken);
        Assert.AreEqual(expected, response.StatusCode);
    }

    [TestMethod]
    public async Task Decompressed_request_limit_is_enforced_before_protobuf_parsing()
    {
        await using var receiver = await OtlpReceiver.StartAsync(TestContext.CancellationToken);
        using var bytes = new MemoryStream();
        await using (var gzip = new GZipStream(bytes, CompressionLevel.SmallestSize, true))
        {
            await gzip.WriteAsync(new byte[16 * 1024 * 1024 + 1], TestContext.CancellationToken);
        }

        using var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        content.Headers.ContentEncoding.Add("gzip");
        using var response = await TelemetryLifecycle.Http.PostAsync(new Uri(receiver.Endpoint, "v1/traces"), content,
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.IsEmpty(receiver.Snapshot().Spans);
    }

    [TestMethod]
    public async Task Invalid_gzip_is_reported_as_bad_request_with_its_error_record()
    {
        await using var receiver = await OtlpReceiver.StartAsync(TestContext.CancellationToken);
        using var content = new ByteArrayContent([1, 2, 3]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        content.Headers.ContentEncoding.Add("gzip");
        using var response = await TelemetryLifecycle.Http.PostAsync(new Uri(receiver.Endpoint, "v1/logs"), content,
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var receipt = Assert.ContainsSingle(receiver.Snapshot().Receipts);
        Assert.AreEqual("logs", receipt.Signal);
        Assert.IsFalse(string.IsNullOrEmpty(receipt.Error));
        Assert.AreEqual(0L, receipt.Accepted);
    }
}