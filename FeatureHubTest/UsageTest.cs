#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using Moq;
using NUnit.Framework;

namespace FeatureHubTest
{
  // ---- helpers ----

  class RecordingPlugin : UsagePlugin
  {
    public readonly List<IUsageEvent> Received = new List<IUsageEvent>();
    public bool Throws;

    public override void Send(IUsageEvent usageEvent)
    {
      if (Throws) throw new Exception("plugin error");
      Received.Add(usageEvent);
    }
  }

  // ---- DefaultUsageProvider.Convert ----

  [TestFixture]
  class UsageConvertTest
  {
    [Test]
    public void BooleanTrueBecomesOn()
      => Assert.That(DefaultUsageProvider.DefaultConvert(true, FeatureValueType.BOOLEAN), Is.EqualTo("on"));

    [Test]
    public void BooleanFalseBecomesOff()
      => Assert.That(DefaultUsageProvider.DefaultConvert(false, FeatureValueType.BOOLEAN), Is.EqualTo("off"));

    [Test]
    public void StringPassesThrough()
      => Assert.That(DefaultUsageProvider.DefaultConvert("hello", FeatureValueType.STRING), Is.EqualTo("hello"));

    [Test]
    public void NumberBecomesString()
      => Assert.That(DefaultUsageProvider.DefaultConvert(3.14, FeatureValueType.NUMBER), Is.EqualTo("3.14"));

    [Test]
    public void JsonReturnsNull()
      => Assert.That(DefaultUsageProvider.DefaultConvert("{}", FeatureValueType.JSON), Is.Null);

    [Test]
    public void NullValueReturnsNull()
      => Assert.That(DefaultUsageProvider.DefaultConvert(null, FeatureValueType.BOOLEAN), Is.Null);

    [Test]
    public void NullTypeReturnsNull()
      => Assert.That(DefaultUsageProvider.DefaultConvert("x", null), Is.Null);
  }

  // ---- FeatureHubUsageValue ----

  [TestFixture]
  class FeatureHubUsageValueTest
  {
    private Guid _envId;
    private Guid _featureId;

    [SetUp]
    public void SetUp()
    {
      _envId = Guid.NewGuid();
      _featureId = Guid.NewGuid();
    }

    private FeatureState MakeFeatureState(FeatureValueType type, object value)
      => new FeatureState(id: _featureId, key: "flag1", varVersion: 1,
        type: type, value: value, environmentId: _envId);

    [Test]
    public void ConstructFromFeatureStateSetsAllFields()
    {
      var fs = MakeFeatureState(FeatureValueType.BOOLEAN, true);
      var uv = new FeatureHubUsageValue(fs, true);

      Assert.That(uv.Id, Is.EqualTo(_featureId));
      Assert.That(uv.Key, Is.EqualTo("flag1"));
      Assert.That(uv.RawValue, Is.EqualTo(true));
      Assert.That(uv.Value, Is.EqualTo("on"));
      Assert.That(uv.Type, Is.EqualTo(FeatureValueType.BOOLEAN));
      Assert.That(uv.EnvironmentId, Is.EqualTo(_envId));
    }

    [Test]
    public void ConstructFromFeatureStateJsonValueIsNull()
    {
      var fs = MakeFeatureState(FeatureValueType.JSON, "{}");
      var uv = new FeatureHubUsageValue(fs, "{}");

      Assert.That(uv.Value, Is.Null);
      Assert.That(uv.RawValue, Is.EqualTo("{}"));
    }

    [Test]
    public void ConstructFromFeatureStateNumberConvertsToString()
    {
      var fs = MakeFeatureState(FeatureValueType.NUMBER, 42.5);
      var uv = new FeatureHubUsageValue(fs, 42.5);

      Assert.That(uv.Value, Is.EqualTo("42.5"));
    }

    [Test]
    public void ConstructFromIFeatureSetsAllFields()
    {
      var mock = new Mock<IFeature>();
      mock.Setup(f => f.Id).Returns(_featureId);
      mock.Setup(f => f.Key).Returns("f1");
      mock.Setup(f => f.Type).Returns(FeatureValueType.STRING);
      mock.Setup(f => f.EnvironmentId).Returns(_envId);

      var uv = new FeatureHubUsageValue(mock.Object, "hello");

      Assert.That(uv.Id, Is.EqualTo(_featureId));
      Assert.That(uv.Key, Is.EqualTo("f1"));
      Assert.That(uv.Value, Is.EqualTo("hello"));
      Assert.That(uv.RawValue, Is.EqualTo("hello"));
      Assert.That(uv.Type, Is.EqualTo(FeatureValueType.STRING));
      Assert.That(uv.EnvironmentId, Is.EqualTo(_envId));
    }

    [Test]
    public void ConstructFromIFeatureThrowsWhenIdIsNull()
    {
      var mock = new Mock<IFeature>();
      mock.Setup(f => f.Id).Returns((Guid?)null);
      mock.Setup(f => f.Key).Returns("f1");
      mock.Setup(f => f.Type).Returns(FeatureValueType.BOOLEAN);
      mock.Setup(f => f.EnvironmentId).Returns(_envId);

      Assert.Throws<InvalidOperationException>(() => new FeatureHubUsageValue(mock.Object, true));
    }
  }

  // ---- DefaultUsageEvent ----

  [TestFixture]
  class DefaultUsageEventTest
  {
    [Test]
    public void DefaultConstructorHasNullUserKey()
    {
      var e = new DefaultUsageEvent();
      Assert.That(e.UserKey, Is.Null);
    }

    [Test]
    public void UserKeyConstructorSetsUserKey()
    {
      var e = new DefaultUsageEvent("alice");
      Assert.That(e.UserKey, Is.EqualTo("alice"));
    }

    [Test]
    public void AdditionalParamsConstructorMergesIntoMap()
    {
      var e = new DefaultUsageEvent("bob", new Dictionary<string, object> { ["x"] = 1 });
      Assert.That(e.CollectUsageRecord()["x"], Is.EqualTo(1));
    }

    [Test]
    public void SetAdditionalParamsReplacesMap()
    {
      var e = new DefaultUsageEvent("alice");
      e.SetAdditionalParams(new Dictionary<string, object> { ["y"] = "hello" });
      Assert.That(e.CollectUsageRecord()["y"], Is.EqualTo("hello"));
    }

    [Test]
    public void SetAdditionalParamsWithNullResetsToEmpty()
    {
      var e = new DefaultUsageEvent("alice", new Dictionary<string, object> { ["a"] = 1 });
      e.SetAdditionalParams(null);
      Assert.That(e.CollectUsageRecord(), Is.Empty);
    }

    [Test]
    public void CollectUsageRecordIsReadOnly()
    {
      var e = new DefaultUsageEvent();
      Assert.That(e.CollectUsageRecord(), Is.InstanceOf<IReadOnlyDictionary<string, object>>());
    }
  }

  // ---- DefaultUsageEventWithFeature ----

  [TestFixture]
  class DefaultUsageEventWithFeatureTest
  {
    private FeatureHubUsageValue _fv;
    private Guid _envId;

    [SetUp]
    public void SetUp()
    {
      _envId = Guid.NewGuid();
      var fs = new FeatureState(id: Guid.NewGuid(), key: "myFlag", varVersion: 1,
        type: FeatureValueType.BOOLEAN, value: true, environmentId: _envId);
      _fv = new FeatureHubUsageValue(fs, true);
    }

    [Test]
    public void EventNameIsFeature()
    {
      var e = new DefaultUsageEventWithFeature(_fv, null, null);
      Assert.That(e.EventName, Is.EqualTo("feature"));
    }

    [Test]
    public void UserKeyIsSetFromConstructor()
    {
      var e = new DefaultUsageEventWithFeature(_fv, null, "alice");
      Assert.That(e.UserKey, Is.EqualTo("alice"));
    }

    [Test]
    public void CollectUsageRecordContainsFeatureFields()
    {
      var e = new DefaultUsageEventWithFeature(_fv, null, null);
      var map = e.CollectUsageRecord();

      Assert.That(map["feature"], Is.EqualTo("myFlag"));
      Assert.That(map["value"], Is.EqualTo("on"));
      Assert.That(map["id"], Is.EqualTo(_fv.Id));
    }

    [Test]
    public void CollectUsageRecordMergesContextAttributes()
    {
      var attrs = new Dictionary<string, List<string>>
      {
        ["country"] = new List<string> { "nz" }
      };
      var e = new DefaultUsageEventWithFeature(_fv, attrs, null);
      var map = e.CollectUsageRecord();

      Assert.That(map.ContainsKey("country"), Is.True);
      Assert.That(map["feature"], Is.EqualTo("myFlag")); // feature fields still present
    }

    [Test]
    public void FeatureFieldsWinOverContextAttributesWithSameKey()
    {
      // if a context attribute has the same key as a feature field, feature field wins
      var attrs = new Dictionary<string, List<string>>
      {
        ["feature"] = new List<string> { "overridden" }
      };
      var e = new DefaultUsageEventWithFeature(_fv, attrs, null);
      var map = e.CollectUsageRecord();

      Assert.That(map["feature"], Is.EqualTo("myFlag"));
    }

    [Test]
    public void AdditionalParamsArePresentInMap()
    {
      var e = new DefaultUsageEventWithFeature(_fv, null, null);
      e.SetAdditionalParams(new Dictionary<string, object> { ["custom"] = "data" });
      var map = e.CollectUsageRecord();

      Assert.That(map["custom"], Is.EqualTo("data"));
    }

    [Test]
    public void NullAttributesDoesNotThrow()
    {
      var e = new DefaultUsageEventWithFeature(_fv, null, null);
      Assert.DoesNotThrow(() => { var _ = e.CollectUsageRecord(); });
    }
  }

  // ---- DefaultUsageFeaturesCollection ----

  [TestFixture]
  class DefaultUsageFeaturesCollectionTest
  {
    private FeatureHubUsageValue MakeValue(string key, FeatureValueType type, object raw)
    {
      var fs = new FeatureState(id: Guid.NewGuid(), key: key, varVersion: 1,
        type: type, value: raw, environmentId: Guid.NewGuid());
      return new FeatureHubUsageValue(fs, raw);
    }

    [Test]
    public void CollectUsageRecordContainsFeatureKeyValuePairs()
    {
      var coll = new DefaultUsageFeaturesCollection();
      coll.SetFeatureValues(new List<FeatureHubUsageValue>
      {
        MakeValue("flag1", FeatureValueType.BOOLEAN, true),
        MakeValue("flag2", FeatureValueType.STRING, "hello")
      });

      var map = coll.CollectUsageRecord();
      Assert.That(map["flag1"], Is.EqualTo("on"));
      Assert.That(map["flag2"], Is.EqualTo("hello"));
    }

    [Test]
    public void AdditionalParamsAlsoPresentInMap()
    {
      var coll = new DefaultUsageFeaturesCollection("user1", new Dictionary<string, object> { ["extra"] = 99 });
      coll.SetFeatureValues(new List<FeatureHubUsageValue>());

      var map = coll.CollectUsageRecord();
      Assert.That(map["extra"], Is.EqualTo(99));
    }

    [Test]
    public void UserKeyIsSet()
    {
      var coll = new DefaultUsageFeaturesCollection("user1", null);
      Assert.That(coll.UserKey, Is.EqualTo("user1"));
    }
  }

  // ---- DefaultUsageFeaturesCollectionContext ----

  [TestFixture]
  class DefaultUsageFeaturesCollectionContextTest
  {
    private FeatureHubUsageValue MakeValue(string key, FeatureValueType type, object raw)
    {
      var fs = new FeatureState(id: Guid.NewGuid(), key: key, varVersion: 1,
        type: type, value: raw, environmentId: Guid.NewGuid());
      return new FeatureHubUsageValue(fs, raw);
    }

    [Test]
    public void CollectUsageRecordContainsBothFeaturesAndAttributes()
    {
      var coll = new DefaultUsageFeaturesCollectionContext();
      coll.SetFeatureValues(new List<FeatureHubUsageValue> { MakeValue("f1", FeatureValueType.BOOLEAN, false) });
      coll.SetAttributes(new Dictionary<string, List<string>> { ["country"] = new List<string> { "nz" } });

      var map = coll.CollectUsageRecord();
      Assert.That(map.ContainsKey("f1"), Is.True);
      Assert.That(map.ContainsKey("country"), Is.True);
    }

    [Test]
    public void ContextAttributesWinOverFeatureKeysWithSameName()
    {
      var coll = new DefaultUsageFeaturesCollectionContext();
      coll.SetFeatureValues(new List<FeatureHubUsageValue> { MakeValue("country", FeatureValueType.STRING, "au") });
      var attrVal = new List<string> { "nz" };
      coll.SetAttributes(new Dictionary<string, List<string>> { ["country"] = attrVal });

      var map = coll.CollectUsageRecord();
      Assert.That(map["country"], Is.EqualTo(attrVal));
    }

    [Test]
    public void EmptyAttributesDoesNotThrow()
    {
      var coll = new DefaultUsageFeaturesCollectionContext();
      coll.SetFeatureValues(new List<FeatureHubUsageValue>());
      coll.SetAttributes(new Dictionary<string, List<string>>());

      Assert.DoesNotThrow(() => { var _ = coll.CollectUsageRecord(); });
    }
  }

  // ---- UsagePlugin ----

  [TestFixture]
  class UsagePluginTest
  {
    [Test]
    public void DefaultEventParamsStartsEmpty()
    {
      var plugin = new RecordingPlugin();
      Assert.That(plugin.GetDefaultEventParams(), Is.Empty);
    }

    [Test]
    public void SendIsCalledWithEvent()
    {
      var plugin = new RecordingPlugin();
      var e = new DefaultUsageEvent("user1");
      plugin.Send(e);
      Assert.That(plugin.Received, Has.Count.EqualTo(1));
      Assert.That(plugin.Received[0], Is.SameAs(e));
    }
  }

  // ---- BaseUsageProvider ----

  [TestFixture]
  class BaseUsageProviderTest
  {
    private BaseUsageProvider _provider = null!;
    private FeatureHubUsageValue _fv = null!;

    [SetUp]
    public void SetUp()
    {
      _provider = new BaseUsageProvider();
      var fs = new FeatureState(id: Guid.NewGuid(), key: "k", varVersion: 1,
        type: FeatureValueType.BOOLEAN, value: true, environmentId: Guid.NewGuid());
      _fv = new FeatureHubUsageValue(fs, true);
    }

    [Test]
    public void CreateUsageFeatureReturnsCorrectType()
      => Assert.That(_provider.CreateUsageFeature(_fv, null, null),
        Is.InstanceOf<DefaultUsageEventWithFeature>());

    [Test]
    public void CreateUsageFeatureWithAttributesAndUserKey()
    {
      var attrs = new Dictionary<string, List<string>> { ["city"] = new List<string> { "london" } };
      var e = _provider.CreateUsageFeature(_fv, attrs, "user1");
      Assert.That(e.UserKey, Is.EqualTo("user1"));
      Assert.That(e.Attributes, Is.SameAs(attrs));
    }

    [Test]
    public void CreateUsageCollectionEventReturnsCorrectType()
      => Assert.That(_provider.CreateUsageCollectionEvent(),
        Is.InstanceOf<DefaultUsageFeaturesCollection>());

    [Test]
    public void CreateUsageContextCollectionEventReturnsCorrectType()
      => Assert.That(_provider.CreateUsageContextCollectionEvent(),
        Is.InstanceOf<DefaultUsageFeaturesCollectionContext>());

    [Test]
    public void CreateUsageEventReturnsCorrectType()
      => Assert.That(_provider.CreateUsageEvent(), Is.InstanceOf<DefaultUsageEvent>());

    [Test]
    public void CreateUsageEventWithUserKeySetsKey()
    {
      var e = _provider.CreateUsageEvent("alice");
      Assert.That(e.UserKey, Is.EqualTo("alice"));
    }

    [Test]
    public void CreateUsageEventWithAdditionalParams()
    {
      var e = _provider.CreateUsageEvent("bob", new Dictionary<string, object> { ["a"] = 1 });
      Assert.That(e.CollectUsageRecord()["a"], Is.EqualTo(1));
    }
  }

  // ---- UsageAdapter ----

  [TestFixture]
  class UsageAdapterTest
  {
    private FeatureHubRepository _repo;
    private UsageAdapter _adapter;

    [SetUp]
    public void SetUp()
    {
      _repo = new FeatureHubRepository();
      _adapter = new UsageAdapter(_repo);
    }

    [TearDown]
    public void TearDown() => _adapter.Close();

    [Test]
    public void RegisteredPluginReceivesEvents()
    {
      var plugin = new RecordingPlugin();
      _adapter.RegisterPlugin(plugin);

      var evt = new DefaultUsageEvent("user1");
      _repo.RecordUsageEvent(evt);

      Assert.That(plugin.Received, Has.Count.EqualTo(1));
      Assert.That(plugin.Received[0], Is.SameAs(evt));
    }

    [Test]
    public void MultiplePluginsAllReceiveEvent()
    {
      var p1 = new RecordingPlugin();
      var p2 = new RecordingPlugin();
      _adapter.RegisterPlugin(p1);
      _adapter.RegisterPlugin(p2);

      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(p1.Received, Has.Count.EqualTo(1));
      Assert.That(p2.Received, Has.Count.EqualTo(1));
    }

    [Test]
    public void ThrowingPluginDoesNotStopOtherPlugins()
    {
      var throwing = new RecordingPlugin { Throws = true };
      var good = new RecordingPlugin();
      _adapter.RegisterPlugin(throwing);
      _adapter.RegisterPlugin(good);

      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(good.Received, Has.Count.EqualTo(1));
    }

    [Test]
    public void CloseStopsDeliveryToPlugins()
    {
      var plugin = new RecordingPlugin();
      _adapter.RegisterPlugin(plugin);

      _adapter.Close();
      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(plugin.Received, Is.Empty);
    }

    [Test]
    public void NoPluginsRegisteredDoesNotThrow()
    {
      Assert.DoesNotThrow(() => _repo.RecordUsageEvent(new DefaultUsageEvent()));
    }
  }

  // ---- FeatureHubRepository usage stream integration ----

  [TestFixture]
  class RepositoryUsageStreamTest
  {
    private FeatureHubRepository _repo;

    [SetUp]
    public void SetUp() => _repo = new FeatureHubRepository();

    [Test]
    public void RegisteredStreamReceivesEvent()
    {
      IUsageEvent? received = null;
      _repo.RegisterUsageStream(e => received = e);

      var evt = new DefaultUsageEvent("alice");
      _repo.RecordUsageEvent(evt);

      Assert.That(received, Is.SameAs(evt));
    }

    [Test]
    public void MultipleStreamsAllReceiveEvent()
    {
      var count = 0;
      _repo.RegisterUsageStream(_ => count++);
      _repo.RegisterUsageStream(_ => count++);

      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void CancelledStreamNoLongerReceivesEvents()
    {
      var count = 0;
      var handler = _repo.RegisterUsageStream(_ => count++);

      handler.Cancel();
      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void CancelOneStreamLeavesOtherIntact()
    {
      var count = 0;
      var handler = _repo.RegisterUsageStream(_ => count++);
      _repo.RegisterUsageStream(_ => count++);

      handler.Cancel();
      _repo.RecordUsageEvent(new DefaultUsageEvent());

      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void DefaultUsageProviderIsBaseUsageProvider()
    {
      Assert.That(_repo.UsageProvider, Is.InstanceOf<BaseUsageProvider>());
    }

    [Test]
    public void RegisterUsageProviderReplacesProvider()
    {
      var custom = new Mock<IUsageProvider>().Object;
      _repo.RegisterUsageProvider(custom);
      Assert.That(_repo.UsageProvider, Is.SameAs(custom));
    }

    [Test]
    public void UsedFiresUsageEventToStreams()
    {
      IUsageEvent? received = null;
      _repo.RegisterUsageStream(e => received = e);

      var fs = new FeatureState(id: Guid.NewGuid(), key: "flag", varVersion: 1,
        type: FeatureValueType.BOOLEAN, value: true, environmentId: Guid.NewGuid());
      _repo.Used(fs, true);

      Assert.That(received, Is.Not.Null);
      Assert.That(received, Is.InstanceOf<IUsageEventWithFeature>());
      var withFeature = (IUsageEventWithFeature)received;
      Assert.That(withFeature.Feature.Key, Is.EqualTo("flag"));
      Assert.That(withFeature.Feature.Value, Is.EqualTo("on"));
    }
  }
}
