using System;
using System.Collections.Generic;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace FeatureHubTest
{
  public sealed class RepositoryTest
  {
    FeatureHubRepository _repository;
    private Guid _envId;

    [SetUp]
    public void Setup()
    {
      _repository = new FeatureHubRepository();
      _envId = Guid.NewGuid();
    }

    public string EncodeFeatures(object value, int varVersion = 1,
      FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: varVersion, value: value, type: type, environmentId: _envId);
      string val = JsonConvert.SerializeObject(new List<FeatureState>(new FeatureState[] { feature }));

      return val;
    }

    public string EncodeFeatures(int varVersion = 1, FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      return EncodeFeatures(false, varVersion, type);
    }

    [Test]
    public void ABooleanIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, 1, FeatureValueType.BOOLEAN), _envId);
      Assert.That(_repository.GetFeature("1"), Is.Not.Null);
      Assert.That(_repository.GetFeature("1").BooleanValue, Is.EqualTo(true));
    }

    [Test]
    public void ANumberIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(16.3, 1, FeatureValueType.NUMBER), _envId);
      Assert.That(_repository.GetFeature("1").NumberValue, Is.EqualTo(16.3));
      Assert.That(_repository.IsEnabled("1"), Is.EqualTo(false));
      Assert.That(_repository.IsSet("1"), Is.EqualTo(true));
      Assert.That(_repository.GetFeature("1").IsEnabled, Is.EqualTo(false));
    }

    [Test]
    public void AStringIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures("some duck", 1, FeatureValueType.STRING), _envId);
      Assert.That(_repository.GetFeature("1").StringValue, Is.EqualTo("some duck"));
      Assert.That(_repository.GetFeature("1").NumberValue, Is.Null);
      Assert.That(_repository.GetFeature("1").JsonValue, Is.Null);
      Assert.That(_repository.GetFeature("1").BooleanValue, Is.Null);
      Assert.That(_repository.IsEnabled("1"), Is.EqualTo(false));
      Assert.That(_repository.GetFeature("1").IsEnabled, Is.EqualTo(false));
    }

    [Test]
    public void JsonIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures("{}", 1, FeatureValueType.JSON), _envId);
      Assert.That(_repository.GetFeature("1").JsonValue, Is.EqualTo("{}"));
    }

    [Test]
    public void ReadynessWhenFeaturesAppear()
    {
      var found = false;
      _repository.ReadinessHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      Assert.That(found, Is.EqualTo(true));
    }

    [Test]
    public void ExplodeWhenReadynessDoesntFailTest()
    {
      var found = false;
      _repository.ReadinessHandler += (sender, readyness) => throw new InvalidOperationException();
      _repository.ReadinessHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      Assert.That(found, Is.EqualTo(false));
    }

    [Test]
    public void ExplodeWhenNewFeatureDoesntFailTest()
    {
      var found = false;
      _repository.NewFeatureHandler += (sender, readyness) => throw new InvalidOperationException();
      _repository.NewFeatureHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      Assert.That(found, Is.EqualTo(false));
    }

    [Test]
    public void ExplodeWhenFeatureUpdatesDoesNotFailTest()
    {
      var found = false;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, state) => throw new InvalidOperationException();
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, state) => { found = true; };
      Assert.That(found, Is.EqualTo(false));
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
    }

    [Test]
    public void ByeTurnsOffReadyness()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      Assert.That(_repository.Readiness, Is.EqualTo(Readiness.Ready));
      _repository.Notify(SSEResultState.Bye, null, _envId);
      Assert.That(_repository.Readiness, Is.EqualTo(Readiness.NotReady));
    }

    [Test]
    public void WhenTheStreamHasFailedReadynessShouldFail()
    {
      var state = Readiness.Ready;
      _repository.ReadinessHandler += (sender, readyness) => { state = readyness; };
      _repository.Notify(SSEResultState.Failure, null, _envId);
      Assert.That(state, Is.EqualTo(Readiness.Failed));
    }

    [Test]
    public void SendingNewVersionsOfFeaturesWillTriggerThewNewFeaturesHook()
    {
      var found = false;
      _repository.NewFeatureHandler += (sender, repository) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      _repository.Notify(SSEResultState.Features, EncodeFeatures(varVersion: 2), _envId);
      Assert.That(found, Is.EqualTo(true));
    }

    [Test]
    public void SendingTheSameVersionsWillNotTriggerNewFeaturesHook()
    {
      var nfCount = 0;
      _repository.NewFeatureHandler += (sender, repository) => { nfCount++; };
      var features = EncodeFeatures();
      _repository.Notify(SSEResultState.Features, features, _envId);
      _repository.Notify(SSEResultState.Features, features, _envId);
      _repository.Notify(SSEResultState.Features, features, _envId);
      Assert.That(nfCount, Is.EqualTo(1));
    }

    [Test]
    public void ListeningForAFeatureThatDoesntExistAndThenTriggeringItTriggersHandler()
    {
      IFeature holder = null;
      var hCount = 0;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) =>
      {
        holder = fs;
        hCount++;
      };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      Assert.That(hCount, Is.EqualTo(1));
      Assert.That(holder, Is.Not.Null);
      Assert.That(holder.Key, Is.EqualTo("1"));
      Assert.That(holder.Exists, Is.EqualTo(true));
      Assert.That(holder.Version, Is.EqualTo(1));
      Assert.That(holder.BooleanValue, Is.EqualTo(false));
      Assert.That(holder.IsEnabled, Is.EqualTo(false));
    }

    [Test]
    public void ListeningForAStringValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures("fred", varVersion: 2, type: FeatureValueType.STRING), _envId);
      Assert.That(holder.Type, Is.EqualTo(FeatureValueType.STRING));
      Assert.That(holder.StringValue, Is.EqualTo("fred"));
      Assert.That(_repository.FeatureState("1").Value, Is.EqualTo("fred"));
    }

    [Test]
    public void ListeningForNumberValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(78.3, varVersion: 2, type: FeatureValueType.NUMBER), _envId);
      Assert.That(holder.Type, Is.EqualTo(FeatureValueType.NUMBER));
      Assert.That(holder.NumberValue, Is.EqualTo(78.3));
    }

    [Test]
    public void ListeningForAJsonValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures("fred", varVersion: 2, type: FeatureValueType.JSON), _envId);
      Assert.That(holder.Type, Is.EqualTo(FeatureValueType.JSON));
      Assert.That(holder.JsonValue, Is.EqualTo("fred"));
      Assert.That(holder.StringValue, Is.Null);
      Assert.That(holder.BooleanValue, Is.Null);
      Assert.That(holder.NumberValue, Is.Null);
    }

    [Test]
    public void ChangingFeatureValueFromOriginalTriggersEventHandler()
    {
      IFeature holder = null;
      var hCount = 0;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) =>
      {
        holder = fs;
        Console.WriteLine($"{fs}");
        hCount++;
      };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId); // same again
      _repository.Notify(SSEResultState.Features,
        EncodeFeatures(varVersion: 2), _envId); // same again, new version but same value
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, varVersion: 3), _envId);
      Assert.That(hCount, Is.EqualTo(2));
      Assert.That(holder, Is.Not.Null);
      Assert.That(holder.BooleanValue, Is.EqualTo(true));
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: 4, value: false,
        type: FeatureValueType.BOOLEAN);
      _repository.Notify(SSEResultState.Feature, JsonConvert.SerializeObject(feature), _envId);
      Assert.That(hCount, Is.EqualTo(3));
      Assert.That(holder.BooleanValue, Is.EqualTo(false));
    }

    [Test]
    public void ChangingFeatureValueWithSameVersionButDifferentValueTriggersEventHandler()
    {
      IFeature holder = null;
      var hCount = 0;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) =>
      {
        holder = fs;
        Console.WriteLine($"{fs}");
        hCount++;
      };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId); // false, 1, boolean
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, 1, FeatureValueType.BOOLEAN), _envId);

      Assert.That(hCount, Is.EqualTo(2));
      Assert.That(holder, Is.Not.Null);
      Assert.That(holder.BooleanValue, Is.EqualTo(true));
    }

    [Test]
    public void ANumberCanBeAnInteger()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(1L, varVersion: 1, type: FeatureValueType.NUMBER), _envId);
      Assert.That(_repository.FeatureState("1").NumberValue, Is.EqualTo(1));
      Assert.That(_repository.GetFeature("1").NumberValue, Is.EqualTo(1));
    }

    [Test]
    public void DeleteRemovesFeature()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(), _envId);
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: 2, value: true,
        type: FeatureValueType.BOOLEAN);
      Assert.That(_repository.FeatureState("1").Version, Is.EqualTo(1));
      _repository.Notify(SSEResultState.DeleteFeature, JsonConvert.SerializeObject(feature), _envId);
      Assert.That(_repository.FeatureState("1").Version, Is.Null);
    }
  }
}
