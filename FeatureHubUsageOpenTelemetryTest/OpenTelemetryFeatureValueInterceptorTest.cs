#nullable enable
using FeatureHubUsageOpenTelemetry;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;
using OpenTelemetry;

namespace FeatureHubUsageOpenTelemetryTest;

[TestFixture]
public class OpenTelemetryFeatureValueInterceptorTest
{
    private OpenTelemetryFeatureValueInterceptor _interceptor = null!;

    [SetUp]
    public void Setup()
    {
        _interceptor = new OpenTelemetryFeatureValueInterceptor();
        Baggage.Current = default;
    }

    [TearDown]
    public void TearDown()
    {
        Baggage.Current = default;
    }

    private static void SetFhubBaggage(string value) =>
        Baggage.Current = Baggage.SetBaggage("fhub", value);

    private static FeatureState StateOf(FeatureValueType type) =>
        new FeatureState(key: "test-key", type: type);

    // ---- No / empty baggage ----

    [Test]
    public void NoBaggage_ReturnsFalse()
    {
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void EmptyFhubField_ReturnsFalse()
    {
        SetFhubBaggage("");
        var (matched, _) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.False);
    }

    // ---- Key lookup ----

    [Test]
    public void KeyNotInList_ReturnsFalse()
    {
        SetFhubBaggage("other-flag=true");
        var (matched, _) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.False);
    }

    [Test]
    public void KeyMatch_IsCaseSensitive()
    {
        SetFhubBaggage("Flag=true");
        var (matched, _) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.False);
    }

    [Test]
    public void MatchesCorrectKeyAmongMultiple()
    {
        SetFhubBaggage("alpha=true,beta=hello,gamma=42");
        var (matched, value) = _interceptor.GetValue("beta", null!, StateOf(FeatureValueType.STRING));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public void NullFeatureState_ReturnsFalse()
    {
        SetFhubBaggage("flag=true");
        var (matched, _) = _interceptor.GetValue("flag", null!, null);
        Assert.That(matched, Is.False);
    }

    [Test]
    public void NullFeatureStateType_ReturnsFalse()
    {
        SetFhubBaggage("flag=true");
        var state = new FeatureState(key: "test-key"); // Type is null
        var (matched, _) = _interceptor.GetValue("flag", null!, state);
        Assert.That(matched, Is.False);
    }

    // ---- BOOLEAN ----

    [Test]
    public void Boolean_True_LowerCase()
    {
        SetFhubBaggage("flag=true");
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(true));
    }

    [Test]
    public void Boolean_True_UpperCase()
    {
        SetFhubBaggage("flag=TRUE");
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(true));
    }

    [Test]
    public void Boolean_False_LowerCase()
    {
        SetFhubBaggage("flag=false");
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(false));
    }

    [Test]
    public void Boolean_False_MixedCase()
    {
        SetFhubBaggage("flag=False");
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(false));
    }

    [Test]
    public void Boolean_InvalidString_ReturnsFalse()
    {
        SetFhubBaggage("flag=yes");
        var (matched, _) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.False);
    }

    // ---- NUMBER ----

    [Test]
    public void Number_Integer()
    {
        SetFhubBaggage("count=42");
        var (matched, value) = _interceptor.GetValue("count", null!, StateOf(FeatureValueType.NUMBER));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(42.0));
    }

    [Test]
    public void Number_Float()
    {
        SetFhubBaggage("rate=3.14");
        var (matched, value) = _interceptor.GetValue("rate", null!, StateOf(FeatureValueType.NUMBER));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(3.14));
    }

    [Test]
    public void Number_Negative()
    {
        SetFhubBaggage("offset=-7");
        var (matched, value) = _interceptor.GetValue("offset", null!, StateOf(FeatureValueType.NUMBER));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(-7.0));
    }

    [Test]
    public void Number_InvalidString_ReturnsFalse()
    {
        SetFhubBaggage("count=abc");
        var (matched, _) = _interceptor.GetValue("count", null!, StateOf(FeatureValueType.NUMBER));
        Assert.That(matched, Is.False);
    }

    // ---- STRING ----

    [Test]
    public void String_PlainValue()
    {
        SetFhubBaggage("greeting=hello");
        var (matched, value) = _interceptor.GetValue("greeting", null!, StateOf(FeatureValueType.STRING));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public void String_UrlEncodedValue_IsDecoded()
    {
        SetFhubBaggage("greeting=hello%20world");
        var (matched, value) = _interceptor.GetValue("greeting", null!, StateOf(FeatureValueType.STRING));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo("hello world"));
    }

    [Test]
    public void String_UrlEncodedCommaInValue_IsDecoded()
    {
        SetFhubBaggage("csv=a%2Cb%2Cc");
        var (matched, value) = _interceptor.GetValue("csv", null!, StateOf(FeatureValueType.STRING));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo("a,b,c"));
    }

    // ---- JSON ----

    [Test]
    public void Json_ReturnedAsString()
    {
        var encoded = "%7B%22key%22%3A%22val%22%7D"; // {"key":"val"}
        SetFhubBaggage($"config={encoded}");
        var (matched, value) = _interceptor.GetValue("config", null!, StateOf(FeatureValueType.JSON));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo("{\"key\":\"val\"}"));
    }

    // ---- Malformed segments ----

    [Test]
    public void SegmentWithNoEquals_IsSkipped()
    {
        SetFhubBaggage("malformed,flag=true");
        var (matched, value) = _interceptor.GetValue("flag", null!, StateOf(FeatureValueType.BOOLEAN));
        Assert.That(matched, Is.True);
        Assert.That(value, Is.EqualTo(true));
    }
}