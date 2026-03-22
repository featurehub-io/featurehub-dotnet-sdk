#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;

namespace FeatureHubInterceptorYamlTest
{
  [TestFixture]
  sealed class LocalYamlValueInterceptorTest
  {
    private string _yamlFile = null!;

    [SetUp]
    public void SetUp() => _yamlFile = Path.GetTempFileName();

    [TearDown]
    public void TearDown() => File.Delete(_yamlFile);

    private LocalYamlValueInterceptor WithYaml(string yaml)
    {
      File.WriteAllText(_yamlFile, yaml);
      return new LocalYamlValueInterceptor(_yamlFile);
    }

    // ---- file loading ----

    [Test]
    public void MissingFileDoesNotIntercept()
    {
      var interceptor = new LocalYamlValueInterceptor("/no/such/file.yaml");
      var (matched, _) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.False);
    }

    [Test]
    public void EmptyFileDoesNotIntercept()
    {
      var interceptor = WithYaml("");
      var (matched, _) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.False);
    }

    [Test]
    public void FileWithNoFlagValuesKeyDoesNotIntercept()
    {
      var interceptor = WithYaml("other:\n  key: value\n");
      var (matched, _) = interceptor.GetValue("key", null!, null);
      Assert.That(matched, Is.False);
    }

    // ---- unknown key ----

    [Test]
    public void UnknownKeyDoesNotIntercept()
    {
      var interceptor = WithYaml("flagValues:\n  known: true\n");
      var (matched, _) = interceptor.GetValue("unknown", null!, null);
      Assert.That(matched, Is.False);
    }

    // ---- boolean ----

    [Test]
    public void TrueValueReturnsBool()
    {
      var interceptor = WithYaml("flagValues:\n  flag: true\n");
      var (matched, value) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(true));
      Assert.That(value, Is.TypeOf<bool>());
    }

    [Test]
    public void FalseValueReturnsBool()
    {
      var interceptor = WithYaml("flagValues:\n  flag: false\n");
      var (matched, value) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(false));
      Assert.That(value, Is.TypeOf<bool>());
    }

    // ---- number ----

    [Test]
    public void IntegerValueReturnsDouble()
    {
      var interceptor = WithYaml("flagValues:\n  retries: 5\n");
      var (matched, value) = interceptor.GetValue("retries", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(5.0));
      Assert.That(value, Is.TypeOf<double>());
    }

    [Test]
    public void FloatValueReturnsDouble()
    {
      var interceptor = WithYaml("flagValues:\n  rate: 3.14\n");
      var (matched, value) = interceptor.GetValue("rate", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(3.14).Within(0.0001));
      Assert.That(value, Is.TypeOf<double>());
    }

    [Test]
    public void NegativeNumberReturnsDouble()
    {
      var interceptor = WithYaml("flagValues:\n  offset: -10\n");
      var (matched, value) = interceptor.GetValue("offset", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(-10.0));
    }

    // ---- string ----

    [Test]
    public void StringValueReturnsString()
    {
      var interceptor = WithYaml("flagValues:\n  greeting: hello\n");
      var (matched, value) = interceptor.GetValue("greeting", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo("hello"));
      Assert.That(value, Is.TypeOf<string>());
    }

    [Test]
    public void QuotedStringValueReturnsString()
    {
      var interceptor = WithYaml("flagValues:\n  msg: \"hello world\"\n");
      var (matched, value) = interceptor.GetValue("msg", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo("hello world"));
    }

    // ---- complex / JSON ----

    [Test]
    public void NestedMapReturnsJsonString()
    {
      var interceptor = WithYaml(
        "flagValues:\n  cfg:\n    key1: val1\n    key2: 42\n");
      var (matched, value) = interceptor.GetValue("cfg", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.TypeOf<string>());
      // should be valid JSON with the expected keys
      var json = (string)value!;
      Assert.That(json, Does.Contain("\"key1\""));
      Assert.That(json, Does.Contain("\"val1\""));
      Assert.That(json, Does.Contain("\"key2\""));
    }

    [Test]
    public void SequenceReturnsJsonString()
    {
      var interceptor = WithYaml("flagValues:\n  items:\n    - a\n    - b\n    - c\n");
      var (matched, value) = interceptor.GetValue("items", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.TypeOf<string>());
      var json = (string)value!;
      Assert.That(json, Does.Contain("\"a\""));
      Assert.That(json, Does.Contain("\"b\""));
      Assert.That(json, Does.Contain("\"c\""));
    }

    [Test]
    public void DeeplyNestedMapReturnsJsonString()
    {
      var interceptor = WithYaml(
        "flagValues:\n  deep:\n    outer:\n      inner: 99\n");
      var (matched, value) = interceptor.GetValue("deep", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.TypeOf<string>());
      Assert.That((string)value!, Does.Contain("\"inner\""));
    }

    // ---- multiple entries ----

    [Test]
    public void MultipleEntriesAllResolvable()
    {
      var interceptor = WithYaml(
        "flagValues:\n  enabled: true\n  count: 7\n  label: hello\n");
      var (m1, v1) = interceptor.GetValue("enabled", null!, null);
      var (m2, v2) = interceptor.GetValue("count", null!, null);
      var (m3, v3) = interceptor.GetValue("label", null!, null);
      Assert.That(m1 && m2 && m3, Is.True);
      Assert.That(v1, Is.EqualTo(true));
      Assert.That(v2, Is.EqualTo(7.0));
      Assert.That(v3, Is.EqualTo("hello"));
    }

    // ---- feature state is ignored for lookup ----

    [Test]
    public void NullFeatureStateStillMatchesByKey()
    {
      var interceptor = WithYaml("flagValues:\n  flag: true\n");
      var (matched, value) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(true));
    }

    [Test]
    public void FeatureStatePresentStillMatchesByKey()
    {
      var interceptor = WithYaml("flagValues:\n  flag: true\n");
      var fs = new FeatureState(id: Guid.NewGuid(), key: "flag", varVersion: 1,
        type: FeatureValueType.BOOLEAN, value: false, environmentId: Guid.NewGuid());
      var (matched, value) = interceptor.GetValue("flag", null!, fs);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(true));
    }

    // ---- close is idempotent for non-watching instances ----

    [Test]
    public void CloseIsIdempotent()
    {
      var interceptor = WithYaml("flagValues:\n  flag: true\n");
      interceptor.Close();
      interceptor.Close(); // should not throw
    }
  }

  [TestFixture]
  sealed class LocalYamlValueInterceptorWatcherTest
  {
    private string _yamlFile = null!;
    private LocalYamlValueInterceptor? _interceptor;

    [SetUp]
    public void SetUp() => _yamlFile = Path.GetTempFileName();

    [TearDown]
    public void TearDown()
    {
      _interceptor?.Close();
      _interceptor = null;
      if (File.Exists(_yamlFile))
        File.Delete(_yamlFile);
    }

    private LocalYamlValueInterceptor WithWatchedYaml(string yaml)
    {
      File.WriteAllText(_yamlFile, yaml);
      _interceptor = new LocalYamlValueInterceptor(_yamlFile, watch: true);
      return _interceptor;
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 3000)
    {
      var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
      while (!condition() && DateTime.UtcNow < deadline)
        await Task.Delay(50);
    }

    [Test]
    public async Task WatcherPicksUpChangedBoolValue()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  flag: true\n");
      Assert.That(interceptor.GetValue("flag", null!, null), Is.EqualTo((true, (object)true)));

      File.WriteAllText(_yamlFile, "flagValues:\n  flag: false\n");

      await WaitForAsync(() => interceptor.GetValue("flag", null!, null) is (true, false));

      var (matched, value) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(false));
    }

    [Test]
    public async Task WatcherPicksUpNewKey()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  existing: true\n");

      File.WriteAllText(_yamlFile, "flagValues:\n  existing: true\n  newKey: hello\n");

      await WaitForAsync(() => interceptor.GetValue("newKey", null!, null).Item1);

      var (matched, value) = interceptor.GetValue("newKey", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task WatcherPicksUpRemovedKey()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  flag: true\n  extra: hello\n");

      File.WriteAllText(_yamlFile, "flagValues:\n  flag: true\n");

      await WaitForAsync(() => !interceptor.GetValue("extra", null!, null).Item1);

      var (matched, _) = interceptor.GetValue("extra", null!, null);
      Assert.That(matched, Is.False);
    }

    [Test]
    public async Task WatcherPicksUpChangedStringValue()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  msg: hello\n");

      File.WriteAllText(_yamlFile, "flagValues:\n  msg: world\n");

      await WaitForAsync(() => interceptor.GetValue("msg", null!, null) is (true, "world"));

      var (matched, value) = interceptor.GetValue("msg", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo("world"));
    }

    [Test]
    public async Task AfterCloseWatcherStopsUpdating()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  flag: true\n");

      // Close BEFORE writing — the watcher is disposed so no events can fire.
      interceptor.Close();

      File.WriteAllText(_yamlFile, "flagValues:\n  flag: false\n");

      // Give it time to (incorrectly) reload if Close failed.
      await Task.Delay(700);

      var (matched, value) = interceptor.GetValue("flag", null!, null);
      Assert.That(matched, Is.True);
      Assert.That(value, Is.EqualTo(true)); // must still be old value
    }

    [Test]
    public void CloseIsIdempotentOnWatchingInstance()
    {
      var interceptor = WithWatchedYaml("flagValues:\n  flag: true\n");
      interceptor.Close();
      interceptor.Close(); // should not throw
    }
  }
}
