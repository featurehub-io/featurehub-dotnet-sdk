#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FeatureHubSDK;
using FeatureHubUsageOpenTelemetry;
using IO.FeatureHub.SSE.Model;
using Moq;
using NUnit.Framework;

namespace FeatureHubUsageOpenTelemetryTest;

[TestFixture]
#pragma warning disable CA1001 // _listener is disposed in [TearDown]
public sealed class OpenTelemetryTrackerUsagePluginTest
#pragma warning restore CA1001
{
  // One source shared across all tests in this fixture
  private static readonly ActivitySource Source = new ActivitySource("FeatureHubTrackerTest");
  private static readonly string[] StringAB = { "a", "b" };
  private static readonly bool[] BoolTrueFalse = { true, false };

  private ActivityListener _listener = null!;
  private Activity? _activity;

  [SetUp]
  public void Setup()
  {
    // Wire up a listener that samples everything so Activity.Current is non-null
    _listener = new ActivityListener
    {
      ShouldListenTo = _ => true,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
          ActivitySamplingResult.AllDataAndRecorded
    };
    ActivitySource.AddActivityListener(_listener);
    _activity = Source.StartActivity("test-span");
  }

  [TearDown]
  public void TearDown()
  {
    _activity?.Dispose();
    _listener.Dispose();
  }

  // ---- Helpers ----

  private static DefaultUsageEventWithFeature FeatureEvent(string key, FeatureValueType type, object? rawValue)
  {
    var fs = new FeatureState(id: Guid.NewGuid(), key: key, type: type, environmentId: Guid.NewGuid());
    var fuv = new FeatureHubUsageValue(fs, rawValue);
    return new DefaultUsageEventWithFeature(fuv, null, null);
  }

  private static Dictionary<string, object?> TagObjects(Activity a) =>
      a.TagObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

  // ---- ConvertValue unit tests ----

  [Test]
  public void ConvertValue_Null_ReturnsNull() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(null), Is.Null);

  [Test]
  public void ConvertValue_Bool() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(true), Is.EqualTo(true));

  [Test]
  public void ConvertValue_Double() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(3.14), Is.EqualTo(3.14));

  [Test]
  public void ConvertValue_Float_PromotedToDouble() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(1.5f), Is.EqualTo(1.5));

  [Test]
  public void ConvertValue_Long() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(42L), Is.EqualTo(42L));

  [Test]
  public void ConvertValue_Int_PromotedToLong() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(7), Is.EqualTo(7L));

  [Test]
  public void ConvertValue_String() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue("hello"), Is.EqualTo("hello"));

  [Test]
  public void ConvertValue_Guid_ReturnsString()
  {
    var g = Guid.NewGuid();
    Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(g), Is.EqualTo(g.ToString()));
  }

  [Test]
  public void ConvertValue_ListOfString_ReturnsStringArray()
  {
    var list = new List<string> { "a", "b" };
    Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(list),
        Is.EqualTo(StringAB));
  }

  [Test]
  public void ConvertValue_ListOfBool_ReturnsBoolArray()
  {
    var list = new List<bool> { true, false };
    Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(list),
        Is.EqualTo(BoolTrueFalse));
  }

  [Test]
  public void ConvertValue_UnknownType_ReturnsToString() =>
      Assert.That(OpenTelemetryTrackerUsagePlugin.ConvertValue(new Uri("http://example.com")),
          Is.EqualTo("http://example.com/"));

  // ---- No current span — should not throw ----

  [Test]
  public void NoCurrentSpan_DoesNotThrow()
  {
    _activity?.Dispose();
    _activity = null;

    var plugin = new OpenTelemetryTrackerUsagePlugin();
    Assert.DoesNotThrow(() => plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true)));
  }

  // ---- Non-IUsageEventName event — ignored ----

  [Test]
  public void NonNamedEvent_NothingSetOnSpan()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin();
    plugin.Send(new Mock<IUsageEvent>().Object);

    Assert.That(_activity!.TagObjects, Is.Empty);
    Assert.That(_activity.Events, Is.Empty);
  }

  // ---- attachAsSpanEvents = false (default): set attributes on span ----

  [Test]
  public void SpanAttributes_BooleanFeature_SetsTagWithPrefix()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin();
    plugin.Send(FeatureEvent("my-flag", FeatureValueType.BOOLEAN, true));

    var tags = TagObjects(_activity!);
    Assert.That(tags["featurehub.feature"], Is.EqualTo("my-flag"));
    Assert.That(tags["featurehub.value"], Is.EqualTo("on")); // serialised Value
  }

  [Test]
  public void SpanAttributes_CustomPrefix_UsedInKeys()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(prefix: "fh.");
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, false));

    var tags = TagObjects(_activity!);
    Assert.That(tags.Keys, Has.All.StartWith("fh."));
  }

  [Test]
  public void SpanAttributes_NullMapValues_Omitted()
  {
    // JSON features produce a null "value" entry in the map
    var plugin = new OpenTelemetryTrackerUsagePlugin();
    plugin.Send(FeatureEvent("cfg", FeatureValueType.JSON, "{\"x\":1}"));

    var tags = TagObjects(_activity!);
    // "value" should not appear (it's null for JSON)
    Assert.That(tags.ContainsKey("featurehub.value"), Is.False);
    // but "feature" (the key) should
    Assert.That(tags["featurehub.feature"], Is.EqualTo("cfg"));
  }

  [Test]
  public void SpanAttributes_NoSpanEvent_Added()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin();
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    Assert.That(_activity!.Events, Is.Empty);
  }

  // ---- attachAsSpanEvents = true: add span event ----

  [Test]
  public void SpanEvent_AddedWithPrefixedName()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    var events = _activity!.Events.ToList();
    Assert.That(events, Has.Count.EqualTo(1));
    Assert.That(events[0].Name, Is.EqualTo("featurehub.feature"));
  }

  [Test]
  public void SpanEvent_CustomPrefix_InEventName()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(prefix: "fh.", attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    Assert.That(_activity!.Events.First().Name, Is.EqualTo("fh.feature"));
  }

  [Test]
  public void SpanEvent_ContainsFeatureAttribute()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("my-flag", FeatureValueType.BOOLEAN, true));

    var evt = _activity!.Events.First();
    var evtTags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
    Assert.That(evtTags["feature"], Is.EqualTo("my-flag"));
  }

  [Test]
  public void SpanEvent_EventKeyNotPrefixed()
  {
    // When attaching as span events, the map keys should NOT be prefixed
    var plugin = new OpenTelemetryTrackerUsagePlugin(prefix: "featurehub.", attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    var evt = _activity!.Events.First();
    var evtTags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
    Assert.That(evtTags.Keys, Has.None.StartWith("featurehub."));
  }

  [Test]
  public void SpanEvent_NoAttributesSetOnSpan()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    Assert.That(_activity!.TagObjects, Is.Empty);
  }

  [Test]
  public void SpanEvent_MultipleSends_MultipleEvents()
  {
    var plugin = new OpenTelemetryTrackerUsagePlugin(attachAsSpanEvents: true);
    plugin.Send(FeatureEvent("flag1", FeatureValueType.BOOLEAN, true));
    plugin.Send(FeatureEvent("flag2", FeatureValueType.BOOLEAN, false));

    Assert.That(_activity!.Events.Count(), Is.EqualTo(2));
  }
}
