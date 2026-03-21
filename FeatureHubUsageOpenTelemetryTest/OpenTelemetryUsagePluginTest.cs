#nullable enable
using System;
using System.Collections.Generic;
using FeatureHubSDK;
using FeatureHubUsageOpenTelemetry;
using IO.FeatureHub.SSE.Model;
using Moq;
using NUnit.Framework;
using OpenTelemetry;

namespace FeatureHubUsageOpenTelemetryTest;

[TestFixture]
public sealed class OpenTelemetryUsagePluginTest
{
  private OpenTelemetryUsagePlugin _plugin = null!;

  [SetUp]
  public void Setup()
  {
    _plugin = new OpenTelemetryUsagePlugin();
    Baggage.Current = default;
  }

  [TearDown]
  public void TearDown()
  {
    Baggage.Current = default;
  }

  // ---- Helpers ----

  private static FeatureState MakeState(string key, FeatureValueType type) =>
      new FeatureState(id: Guid.NewGuid(), key: key, type: type, environmentId: Guid.NewGuid());

  private static DefaultUsageEventWithFeature FeatureEvent(string key, FeatureValueType type, object? rawValue)
  {
    var fs = MakeState(key, type);
    var fuv = new FeatureHubUsageValue(fs, rawValue);
    return new DefaultUsageEventWithFeature(fuv, null, null);
  }

  private static DefaultUsageFeaturesCollection CollectionEvent(params (string key, FeatureValueType type, object? rawValue)[] features)
  {
    var values = new List<FeatureHubUsageValue>();
    foreach (var (key, type, rawValue) in features)
    {
      var fs = MakeState(key, type);
      values.Add(new FeatureHubUsageValue(fs, rawValue));
    }
    var coll = new DefaultUsageFeaturesCollection();
    coll.SetFeatureValues(values);
    return coll;
  }

  private static string? GetFhub() => Baggage.Current.GetBaggage("fhub");

  // ---- EncodeRawValue unit tests ----

  [Test]
  public void EncodeRawValue_BoolTrue() =>
      Assert.That(OpenTelemetryUsagePlugin.EncodeRawValue(true), Is.EqualTo("true"));

  [Test]
  public void EncodeRawValue_BoolFalse() =>
      Assert.That(OpenTelemetryUsagePlugin.EncodeRawValue(false), Is.EqualTo("false"));

  [Test]
  public void EncodeRawValue_Double() =>
      Assert.That(OpenTelemetryUsagePlugin.EncodeRawValue(3.14), Is.EqualTo("3.14"));

  [Test]
  public void EncodeRawValue_String() =>
      Assert.That(OpenTelemetryUsagePlugin.EncodeRawValue("hello"), Is.EqualTo("hello"));

  [Test]
  public void EncodeRawValue_Null() =>
      Assert.That(OpenTelemetryUsagePlugin.EncodeRawValue(null), Is.Null);

  // ---- IUsageEventWithFeature: single feature ----

  [Test]
  public void SingleFeature_Boolean_True_SetsBaggage()
  {
    _plugin.Send(FeatureEvent("my-flag", FeatureValueType.BOOLEAN, true));
    Assert.That(GetFhub(), Is.EqualTo("my-flag=true"));
  }

  [Test]
  public void SingleFeature_Boolean_False_SetsBaggage()
  {
    _plugin.Send(FeatureEvent("my-flag", FeatureValueType.BOOLEAN, false));
    Assert.That(GetFhub(), Is.EqualTo("my-flag=false"));
  }

  [Test]
  public void SingleFeature_Number_SetsBaggage()
  {
    _plugin.Send(FeatureEvent("rate", FeatureValueType.NUMBER, 42.5));
    Assert.That(GetFhub(), Is.EqualTo("rate=42.5"));
  }

  [Test]
  public void SingleFeature_String_SetsBaggage()
  {
    _plugin.Send(FeatureEvent("greeting", FeatureValueType.STRING, "hello"));
    Assert.That(GetFhub(), Is.EqualTo("greeting=hello"));
  }

  [Test]
  public void SingleFeature_String_WithSpaces_UrlEncoded()
  {
    _plugin.Send(FeatureEvent("msg", FeatureValueType.STRING, "hello world"));
    Assert.That(GetFhub(), Is.EqualTo("msg=hello%20world"));
  }

  [Test]
  public void SingleFeature_Json_SetsBaggage()
  {
    _plugin.Send(FeatureEvent("cfg", FeatureValueType.JSON, "{\"a\":1}"));
    Assert.That(GetFhub(), Is.EqualTo("cfg=%7B%22a%22%3A1%7D"));
  }

  [Test]
  public void SingleFeature_NullRawValue_DoesNotSetBaggage()
  {
    _plugin.Send(FeatureEvent("flag", FeatureValueType.STRING, null));
    Assert.That(GetFhub(), Is.Null);
  }

  // ---- IUsageEventWithFeature: merge behaviour ----

  [Test]
  public void SingleFeature_MergesWithExistingBaggage_AlphabeticalOrder()
  {
    Baggage.Current = Baggage.SetBaggage("fhub", "other=val");
    _plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));
    Assert.That(GetFhub(), Is.EqualTo("flag=true,other=val"));
  }

  [Test]
  public void SingleFeature_ReplacesExistingKeyInBaggage_OrderPreserved()
  {
    Baggage.Current = Baggage.SetBaggage("fhub", "flag=false,other=val");
    _plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));
    Assert.That(GetFhub(), Is.EqualTo("flag=true,other=val"));
  }

  [Test]
  public void SingleFeature_SecondSend_UpdatesKey()
  {
    _plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));
    _plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, false));
    Assert.That(GetFhub(), Is.EqualTo("flag=false"));
  }

  [Test]
  public void SingleFeature_MultipleAdds_AlwaysAlphabetical()
  {
    _plugin.Send(FeatureEvent("zebra", FeatureValueType.BOOLEAN, true));
    _plugin.Send(FeatureEvent("alpha", FeatureValueType.STRING, "a"));
    _plugin.Send(FeatureEvent("mango", FeatureValueType.NUMBER, 1.0));
    Assert.That(GetFhub(), Is.EqualTo("alpha=a,mango=1,zebra=true"));
  }

  // ---- IUsageFeaturesCollection ----

  [Test]
  public void Collection_MultipleFeatures_StoredAlphabetically()
  {
    _plugin.Send(CollectionEvent(
        ("rate", FeatureValueType.NUMBER, 9.99),
        ("flag", FeatureValueType.BOOLEAN, true),
        ("msg", FeatureValueType.STRING, "hi")));

    Assert.That(GetFhub(), Is.EqualTo("flag=true,msg=hi,rate=9.99"));
  }

  [Test]
  public void Collection_Boolean_EncodesAsTrueFalseNotOnOff()
  {
    // Verifies raw value is used, not the serialised "on"/"off"
    _plugin.Send(CollectionEvent(("flag", FeatureValueType.BOOLEAN, false)));
    Assert.That(GetFhub(), Is.EqualTo("flag=false"));
  }

  [Test]
  public void Collection_Json_IsIncluded()
  {
    _plugin.Send(CollectionEvent(("cfg", FeatureValueType.JSON, "{\"x\":1}")));
    Assert.That(GetFhub(), Is.EqualTo("cfg=%7B%22x%22%3A1%7D"));
  }

  [Test]
  public void Collection_NullRawValue_KeyOmittedFromBaggage()
  {
    _plugin.Send(CollectionEvent(
        ("good", FeatureValueType.STRING, "yes"),
        ("nulled", FeatureValueType.STRING, null)));

    // "nulled" has a null raw value and should be omitted
    Assert.That(GetFhub(), Is.EqualTo("good=yes"));
  }

  [Test]
  public void Collection_AllNullRawValues_BaggageNotSet()
  {
    _plugin.Send(CollectionEvent(("flag", FeatureValueType.STRING, null)));
    Assert.That(GetFhub(), Is.Null);
  }

  [Test]
  public void Collection_EmptyFeatureList_BaggageNotSet()
  {
    var coll = new DefaultUsageFeaturesCollection();
    coll.SetFeatureValues(new List<FeatureHubUsageValue>());
    _plugin.Send(coll);
    Assert.That(GetFhub(), Is.Null);
  }

  // ---- Unrecognised event type ----

  [Test]
  public void UnknownEventType_BaggageNotSet()
  {
    _plugin.Send(new Mock<IUsageEvent>().Object);
    Assert.That(GetFhub(), Is.Null);
  }

  // ---- Round-trip with interceptor ----

  [Test]
  public void RoundTrip_SingleBoolean()
  {
    _plugin.Send(FeatureEvent("flag", FeatureValueType.BOOLEAN, true));

    var interceptor = new OpenTelemetryFeatureValueInterceptor();
    var state = new FeatureState(key: "flag", type: FeatureValueType.BOOLEAN);
    var (matched, value) = interceptor.GetValue("flag", null!, state);

    Assert.That(matched, Is.True);
    Assert.That(value, Is.EqualTo(true));
  }

  [Test]
  public void RoundTrip_CollectionNumber()
  {
    _plugin.Send(CollectionEvent(("rate", FeatureValueType.NUMBER, 1.5)));

    var interceptor = new OpenTelemetryFeatureValueInterceptor();
    var state = new FeatureState(key: "rate", type: FeatureValueType.NUMBER);
    var (matched, value) = interceptor.GetValue("rate", null!, state);

    Assert.That(matched, Is.True);
    Assert.That(value, Is.EqualTo(1.5));
  }
}
