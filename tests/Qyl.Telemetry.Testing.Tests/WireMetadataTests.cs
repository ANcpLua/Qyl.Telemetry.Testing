using System.Diagnostics;
using Google.Protobuf;
using JetBrains.Annotations;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Qyl.Telemetry.SemanticConventions;
using EntityRef = OpenTelemetry.Proto.Common.V1.EntityRef;
using ProtoResource = OpenTelemetry.Proto.Resource.V1.Resource;

namespace Qyl.Telemetry.Testing.Tests;

[TestClass]
public sealed class WireMetadataTests
{
    [UsedImplicitly] public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Complete_span_resource_scope_events_and_links_survive_in_wire_order()
    {
        await using var receiver = await OtlpReceiver.StartAsync(TestContext.CancellationToken);
        var request = RichSpan();
        using var response =
            await ReceiverTests.PostAsync(receiver.Endpoint, request.ToByteArray(), TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        var span = Assert.ContainsSingle(receiver.Snapshot().Spans);
        Assert.AreEqual("11111111111111111111111111111111", span.TraceId);
        Assert.AreEqual("2222222222222222", span.SpanId);
        Assert.AreEqual("3333333333333333", span.ParentSpanId);
        Assert.AreEqual("rich.operation", span.Name);
        Assert.AreEqual(SpanKind.Consumer, span.Kind);
        Assert.AreEqual(ActivityStatusCode.Error, span.Status);
        Assert.AreEqual("failed exactly", span.StatusDescription);
        Assert.AreEqual("vendor=value", span.TraceState);
        Assert.AreEqual(0x80000301u, span.Flags);
        Assert.AreEqual(1000000001UL, span.StartTimeUnixNano);
        Assert.AreEqual(1000000099UL, span.EndTimeUnixNano);
        Assert.AreEqual("resource-schema", span.Resource.SchemaUrl);
        Assert.AreEqual(3u, span.Resource.DroppedAttributesCount);
        var entity = Assert.ContainsSingle(span.Resource.EntityReferences);
        Assert.AreEqual("service", entity.Type);
        Assert.AreSequenceEqual(["service.name"], entity.IdentityKeys);
        Assert.AreSequenceEqual(["service.version"], entity.DescriptionKeys);
        Assert.AreEqual("entity-schema", entity.SchemaUrl);
        Assert.AreEqual("rich-service", span.Service);
        Assert.AreEqual("rich-scope", span.Scope?.Name);
        Assert.AreEqual("3.2.1", span.Scope?.Version);
        Assert.AreEqual("scope-schema", span.Scope?.SchemaUrl);
        Assert.AreEqual(4u, span.Scope?.DroppedAttributesCount);
        Assert.IsTrue(Assert.IsInstanceOfType<bool>(span.Scope?.Attributes["scope.enabled"]));
        Assert.AreEqual(5u, span.DroppedAttributesCount);
        Assert.AreEqual(6u, span.DroppedEventsCount);
        Assert.AreEqual(7u, span.DroppedLinksCount);
        Assert.AreSequenceEqual(["exception", "completed"], span.Events.Select(e => e.Name));
        Assert.AreEqual(1000000002UL, span.Events[0].TimeUnixNano);
        Assert.AreEqual("keep message", span.Events[0].Attributes["exception.message"]);
        Assert.AreEqual(8u, span.Events[0].DroppedAttributesCount);
        var link = Assert.ContainsSingle(span.Links);
        Assert.AreEqual("44444444444444444444444444444444", link.TraceId);
        Assert.AreEqual("5555555555555555", link.SpanId);
        Assert.AreEqual("linked=state", link.TraceState);
        Assert.AreEqual(0x80000301u, link.Flags);
        Assert.AreEqual(9u, link.DroppedAttributesCount);
        Assert.AreEqual(42L, link.Attributes["link.value"]);
    }

    [TestMethod]
    public async Task Recursive_otlp_values_keep_their_types_without_stringification()
    {
        await using var receiver = await OtlpReceiver.StartAsync(TestContext.CancellationToken);
        var request = RichSpan();
        var input = request.ResourceSpans[0].ScopeSpans[0].Spans[0];
        input.Attributes.Add(Pair("bytes", new AnyValue { BytesValue = ByteString.CopyFrom([0, 127, 255]) }));
        input.Attributes.Add(Pair("array",
            new AnyValue
            {
                ArrayValue = new ArrayValue
                {
                    Values =
                    {
                        new AnyValue { IntValue = -7 }, new AnyValue { BoolValue = true },
                        new AnyValue { DoubleValue = 0.125 }, new AnyValue()
                    }
                }
            }));
        input.Attributes.Add(Pair("map",
            new AnyValue
            {
                KvlistValue = new KeyValueList { Values = { Pair("nested", new AnyValue { StringValue = "exact" }) } }
            }));
        using var response =
            await ReceiverTests.PostAsync(receiver.Endpoint, request.ToByteArray(), TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        var span = Assert.ContainsSingle(receiver.Snapshot().Spans);
        Assert.AreEqual("rich.operation", span.Name);
        Assert.AreEqual("11111111111111111111111111111111", span.TraceId);
        Assert.AreSequenceEqual(new byte[] { 0, 127, 255 }, Assert.IsInstanceOfType<byte[]>(span.Attributes["bytes"]));
        var array = Assert.IsInstanceOfType<IReadOnlyList<object?>>(span.Attributes["array"]);
        Assert.AreSequenceEqual([-7L, true, 0.125, null], array);
        var map = Assert.IsInstanceOfType<IReadOnlyDictionary<string, object?>>(span.Attributes["map"]);
        Assert.AreEqual("exact", map["nested"]);
    }

    internal static ExportTraceServiceRequest RichSpan()
    {
        var resource = new ProtoResource
        {
            DroppedAttributesCount = 3,
            Attributes =
            {
                Pair("service.name", new AnyValue { StringValue = "rich-service" }),
                Pair("service.version", new AnyValue { StringValue = "1.2.3" })
            },
            EntityRefs =
            {
                new EntityRef
                {
                    Type = "service", SchemaUrl = "entity-schema", IdKeys = { "service.name" },
                    DescriptionKeys = { "service.version" }
                }
            }
        };
        var scope = new ScopeSpans
        {
            SchemaUrl = "scope-schema",
            Scope = new InstrumentationScope
            {
                Name = "rich-scope", Version = "3.2.1", DroppedAttributesCount = 4,
                Attributes = { Pair("scope.enabled", new AnyValue { BoolValue = true }) }
            }
        };
        scope.Spans.Add(new Span
        {
            Name = "rich.operation", TraceId = Id("11111111111111111111111111111111"), SpanId = Id("2222222222222222"),
            ParentSpanId = Id("3333333333333333"),
            Kind = Span.Types.SpanKind.Consumer, Flags = 0x80000301, TraceState = "vendor=value",
            StartTimeUnixNano = 1000000001, EndTimeUnixNano = 1000000099,
            Status = new Status { Code = Status.Types.StatusCode.Error, Message = "failed exactly" },
            DroppedAttributesCount = 5, DroppedEventsCount = 6, DroppedLinksCount = 7,
            Events =
            {
                new Span.Types.Event
                {
                    Name = "exception", TimeUnixNano = 1000000002, DroppedAttributesCount = 8,
                    Attributes = { Pair("exception.message", new AnyValue { StringValue = "keep message" }) }
                },
                new Span.Types.Event { Name = "completed", TimeUnixNano = 1000000003 }
            },
            Links =
            {
                new Span.Types.Link
                {
                    TraceId = Id("44444444444444444444444444444444"), SpanId = Id("5555555555555555"),
                    TraceState = "linked=state", Flags = 0x80000301, DroppedAttributesCount = 9,
                    Attributes = { Pair("link.value", new AnyValue { IntValue = 42 }) }
                }
            }
        });
        return new ExportTraceServiceRequest
        {
            ResourceSpans =
                { new ResourceSpans { Resource = resource, SchemaUrl = "resource-schema", ScopeSpans = { scope } } }
        };
    }

    internal static KeyValue Pair(string key, AnyValue value)
    {
        return new KeyValue { Key = key, Value = value };
    }

    internal static ByteString Id(string hex)
    {
        return ByteString.CopyFrom(Convert.FromHexString(hex));
    }
}