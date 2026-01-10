using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;

namespace FeatureHubTest
{
  public class RepositoryTest
  {
    FeatureHubRepository _repository;

    [SetUp]
    public void Setup()
    {
      _repository = new FeatureHubRepository();
    }

    public static string EncodeFeatures(object value, int varVersion = 1,
      FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: varVersion, value: value, type: type);
      string val = JsonConvert.SerializeObject(new List<FeatureState>(new FeatureState[] { feature }));

      return val;
    }

    public static string EncodeFeatures(int varVersion = 1, FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      return EncodeFeatures(false, varVersion, type);
    }

    [Test]
    public void ABooleanIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, 1, FeatureValueType.BOOLEAN));
      ClassicAssert.AreEqual(true, _repository.GetFeature("1").BooleanValue);
    }

    [Test]
    public void ANumberIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(16.3, 1, FeatureValueType.NUMBER));
      ClassicAssert.AreEqual(16.3, _repository.GetFeature("1").NumberValue);
      ClassicAssert.AreEqual(false, _repository.IsEnabled("1"));
      ClassicAssert.AreEqual(true, _repository.IsSet("1"));
      ClassicAssert.AreEqual(false, _repository.GetFeature("1").IsEnabled);
    }

    [Test]
    public void AStringIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures("some duck", 1, FeatureValueType.STRING));
      ClassicAssert.AreEqual("some duck", _repository.GetFeature("1").StringValue);
      ClassicAssert.IsNull(_repository.GetFeature("1").NumberValue);
      ClassicAssert.IsNull(_repository.GetFeature("1").JsonValue);
      ClassicAssert.IsNull(_repository.GetFeature("1").BooleanValue);
      ClassicAssert.AreEqual(false, _repository.IsEnabled("1"));
      ClassicAssert.AreEqual(false, _repository.GetFeature("1").IsEnabled);
    }

    [Test]
    public void JsonIsStoredCorrectly()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures("{}", 1, FeatureValueType.JSON));
      ClassicAssert.AreEqual("{}", _repository.GetFeature("1").JsonValue);
    }

    [Test]
    public void ReadynessWhenFeaturesAppear()
    {
      var found = false;
      _repository.ReadynessHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      ClassicAssert.AreEqual(true, found);
    }

    [Test]
    public void ExplodeWhenReadynessDoesntFailTest()
    {
      var found = false;
      _repository.ReadynessHandler += (sender, readyness) => throw new Exception();
      _repository.ReadynessHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      ClassicAssert.AreEqual(false, found);
    }

    [Test]
    public void ExplodeWhenNewFeatureDoesntFailTest()
    {
      var found = false;
      _repository.NewFeatureHandler += (sender, readyness) => throw new Exception();
      _repository.NewFeatureHandler += (sender, readyness) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      ClassicAssert.AreEqual(false, found);
    }

    [Test]
    public void ExplodeWhenFeatureUpdatesDoesNotFailTest()
    {
      var found = false;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, state) => throw new Exception();
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, state) => { found = true; };
      ClassicAssert.AreEqual(false, found);
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
    }

    [Test]
    public void ByeTurnsOffReadyness()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      ClassicAssert.AreEqual(Readyness.Ready, _repository.Readyness);
      _repository.Notify(SSEResultState.Bye, null);
      ClassicAssert.AreEqual(Readyness.NotReady, _repository.Readyness);
    }

    [Test]
    public void WhenTheStreamHasFailedReadynessShouldFail()
    {
      var state = Readyness.Ready;
      _repository.ReadynessHandler += (sender, readyness) => { state = readyness; };
      _repository.Notify(SSEResultState.Failure, null);
      ClassicAssert.AreEqual(Readyness.Failed, state);
    }

    [Test]
    public void SendingNewVersionsOfFeaturesWillTriggerThewNewFeaturesHook()
    {
      var found = false;
      _repository.NewFeatureHandler += (sender, repository) => { found = true; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      _repository.Notify(SSEResultState.Features, EncodeFeatures(varVersion: 2));
      ClassicAssert.AreEqual(true, found);
    }

    [Test]
    public void SendingTheSameVersionsWillNotTriggerNewFeaturesHook()
    {
      var nfCount = 0;
      _repository.NewFeatureHandler += (sender, repository) => { nfCount++; };
      var features = EncodeFeatures();
      _repository.Notify(SSEResultState.Features, features);
      _repository.Notify(SSEResultState.Features, features);
      _repository.Notify(SSEResultState.Features, features);
      ClassicAssert.AreEqual(1, nfCount);
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
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      ClassicAssert.AreEqual(1, hCount);
      ClassicAssert.IsNotNull(holder);
      ClassicAssert.AreEqual("1", holder.Key);
      ClassicAssert.AreEqual(true, holder.Exists);
      ClassicAssert.AreEqual(1, holder.Version);
      ClassicAssert.AreEqual(false, holder.BooleanValue);
      ClassicAssert.AreEqual(false, holder.IsEnabled);
    }

    [Test]
    public void ListeningForAStringValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures("fred", varVersion: 2, type: FeatureValueType.STRING));
      ClassicAssert.AreEqual(FeatureValueType.STRING, holder.Type);
      ClassicAssert.AreEqual("fred", holder.StringValue);
      ClassicAssert.AreEqual("fred", _repository.FeatureState("1").Value);
    }

    [Test]
    public void ListeningForNumberValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures(78.3, varVersion: 2, type: FeatureValueType.NUMBER));
      ClassicAssert.AreEqual(FeatureValueType.NUMBER, holder.Type);
      ClassicAssert.AreEqual(78.3, holder.NumberValue);
    }

    [Test]
    public void ListeningForAJsonValueWorksAsExpected()
    {
      IFeature holder = null;
      _repository.FeatureState("1").FeatureUpdateHandler += (sender, fs) => { holder = fs; };
      _repository.Notify(SSEResultState.Features, EncodeFeatures("fred", varVersion: 2, type: FeatureValueType.JSON));
      ClassicAssert.AreEqual(FeatureValueType.JSON, holder.Type);
      ClassicAssert.AreEqual("fred", holder.JsonValue);
      ClassicAssert.IsNull(holder.StringValue);
      ClassicAssert.IsNull(holder.BooleanValue);
      ClassicAssert.IsNull(holder.NumberValue);
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
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      _repository.Notify(SSEResultState.Features, EncodeFeatures()); // same again
      _repository.Notify(SSEResultState.Features,
        EncodeFeatures(varVersion: 2)); // same again, new version but same value
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, varVersion: 3));
      ClassicAssert.AreEqual(2, hCount);
      ClassicAssert.IsNotNull(holder);
      ClassicAssert.AreEqual(true, holder.BooleanValue);
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: 4, value: false,
        type: FeatureValueType.BOOLEAN);
      _repository.Notify(SSEResultState.Feature, JsonConvert.SerializeObject(feature));
      ClassicAssert.AreEqual(3, hCount);
      ClassicAssert.AreEqual(false, holder.BooleanValue);
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
      _repository.Notify(SSEResultState.Features, EncodeFeatures()); // false, 1, boolean
      _repository.Notify(SSEResultState.Features, EncodeFeatures(true, 1, FeatureValueType.BOOLEAN));

      ClassicAssert.AreEqual(2, hCount);
      ClassicAssert.IsNotNull(holder);
      ClassicAssert.AreEqual(true, holder.BooleanValue);
    }

    [Test]
    public void ANumberCanBeAnInteger()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures(1L, varVersion: 1, type: FeatureValueType.NUMBER));
      ClassicAssert.AreEqual(1, _repository.FeatureState("1").NumberValue);
      ClassicAssert.AreEqual(1, _repository.GetFeature("1").NumberValue);
    }

    [Test]
    public void DeleteRemovesFeature()
    {
      _repository.Notify(SSEResultState.Features, EncodeFeatures());
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: 2, value: true,
        type: FeatureValueType.BOOLEAN);
      ClassicAssert.AreEqual(1, _repository.FeatureState("1").Version);
      _repository.Notify(SSEResultState.DeleteFeature, JsonConvert.SerializeObject(feature));
      ClassicAssert.IsNull(_repository.FeatureState("1").Version);
    }
  }
}
