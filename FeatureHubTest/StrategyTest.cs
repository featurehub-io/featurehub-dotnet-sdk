using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;

namespace FeatureHubTest
{
  class StrategyTest
  {
    private FeatureHubRepository repo;

    [SetUp]
    public void Setup()
    {
      repo = new FeatureHubRepository();
    }

    private static string GetEnumMemberValue(Enum enumValue)
    {
      var type = enumValue.GetType();
      var info = type.GetField(enumValue.ToString());
      var da = (EnumMemberAttribute[])(info.GetCustomAttributes(typeof(EnumMemberAttribute), false));

      return da.Length > 0 ? da[0].Value : string.Empty;
    }


    [Test]
    public void BasicBooleanStrategy()
    {
      // given: we have a basic boolean feature
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "bool1", value: true, version: 1, type: FeatureValueType.BOOLEAN,
          strategies: new List<FeatureRolloutStrategy>
          {
            new FeatureRolloutStrategy(id: "id", value: false, attributes: new List<FeatureRolloutStrategyAttribute>
            {
              new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.EQUALS, type: RolloutStrategyFieldType.STRING,
                fieldName: GetEnumMemberValue(StrategyAttributeWellKnownNames.Country), values: new List<object> {GetEnumMemberValue(StrategyAttributeCountryName.Turkey)})
            })
          });

      repo.UpdateFeatures(new List<FeatureState>{feature});

      var matchCC = new TestClientContext().Country(StrategyAttributeCountryName.Turkey);
      var unmatchCC = new TestClientContext().Country(StrategyAttributeCountryName.NewZealand);

      Assert.AreEqual(false, repo.GetFeature("bool1").WithContext(matchCC).BooleanValue);
      Assert.AreEqual(true, repo.GetFeature("bool1").WithContext(unmatchCC).BooleanValue);
      Assert.AreEqual(true, repo.GetFeature("bool1").BooleanValue);
    }

    [Test]
    public void BasicNumberStrategy()
    {
      // given: we have a basic number feature with two custom strategies based on age
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "num1", value: 16, version: 1, type: FeatureValueType.NUMBER,
        strategies: new List<FeatureRolloutStrategy>
        {
          new FeatureRolloutStrategy(id: "over40", value: 6, attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.GREATEREQUALS, type: RolloutStrategyFieldType.NUMBER,
              fieldName: "age", values: new List<object> {40})
          }),
          new FeatureRolloutStrategy(id: "over20", value: 10, attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.GREATEREQUALS, type: RolloutStrategyFieldType.NUMBER,
              fieldName: "age", values: new List<object> {20})
          }),
          
        });
      
      // when: setup repo
      repo.UpdateFeatures(new List<FeatureState>{feature});

      var age27 = new TestClientContext().Attr("age", "27");
      var age18 = new TestClientContext().Attr("age", "18");
      var age43 = new TestClientContext().Attr("age", "43");

      // then
      Assert.AreEqual(10, repo.GetFeature("num1").WithContext(age27).NumberValue);
      Assert.AreEqual(16, repo.GetFeature("num1").WithContext(age18).NumberValue);
      Assert.AreEqual(6, repo.GetFeature("num1").WithContext(age43).NumberValue);
    }

    [Test]
    public void NumberGroupStrategy()
    {
      // given: we have a grouped number feature with two custom strategies based on age
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "num1", value: 16, version: 1, type: FeatureValueType.NUMBER,
        strategies: new List<FeatureRolloutStrategy>
        {
          new FeatureRolloutStrategy(id: "contractId", value: 6, attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.EQUALS, type: RolloutStrategyFieldType.NUMBER,
              fieldName: "contractId", values: new List<object> {40, 16, 23})
          }),
        });
      
      // when: setup repo
      repo.UpdateFeatures(new List<FeatureState>{feature});

      var oneMatch = new TestClientContext().Attrs("contractId", new List<String> { "3", "40", "26" });
      var noneMatch = new TestClientContext().Attrs("contractId", new List<String> { "3", "400", "26" });
      
      Assert.AreEqual(6, repo.GetFeature("num1").WithContext(oneMatch).NumberValue);
      Assert.AreEqual(16, repo.GetFeature("num1").WithContext(noneMatch).NumberValue);
    }

    private void StringTypeComparison(FeatureValueType ft)
    {
      // given: we have a basic string feature with two custom strategies based on age and platform
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "s1", value: "feature", version: 1, type: ft,
        strategies: new List<FeatureRolloutStrategy>
        {
          new FeatureRolloutStrategy(id: "notmobile", value: "not-mobile", attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.EXCLUDES, type: RolloutStrategyFieldType.STRING,
              fieldName: GetEnumMemberValue(StrategyAttributeWellKnownNames.Platform), 
              values: new List<object> {GetEnumMemberValue(StrategyAttributePlatformName.Android), GetEnumMemberValue(StrategyAttributePlatformName.Ios)})
          }),
          new FeatureRolloutStrategy(id: "old-than-twenty", value: "older-than-twenty", attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.GREATEREQUALS, type: RolloutStrategyFieldType.NUMBER,
              fieldName: "age", values: new List<object> {20})
          }),
          
        });
      
      // when: setup repo
      repo.UpdateFeatures(new List<FeatureState>{feature});

      var ccAge27Ios = new TestClientContext().Platform(StrategyAttributePlatformName.Ios).Attr("age", "27");
      var ccAge18Android = new TestClientContext().Platform(StrategyAttributePlatformName.Android).Attr("age", "18");
      var ccAge43MacOS = new TestClientContext().Platform(StrategyAttributePlatformName.Macos).Attr("age", "43");
      var ccAge18MacOS = new TestClientContext().Platform(StrategyAttributePlatformName.Macos).Attr("age", "18");
      var ccEmpty = new TestClientContext();

      switch (ft)
      {
        case FeatureValueType.STRING:
          // then
          Assert.AreEqual("feature", repo.GetFeature("s1").StringValue);
          Assert.AreEqual("feature", repo.GetFeature("s1").WithContext(ccEmpty).StringValue);
          Assert.AreEqual("feature", repo.GetFeature("s1").WithContext(ccAge18Android).StringValue);
          Assert.AreEqual("not-mobile", repo.GetFeature("s1").WithContext(ccAge18MacOS).StringValue);
          Assert.AreEqual("older-than-twenty", repo.GetFeature("s1").WithContext(ccAge27Ios).StringValue);
          Assert.AreEqual("not-mobile", repo.GetFeature("s1").WithContext(ccAge43MacOS).StringValue);
          break;
        case FeatureValueType.JSON:
          Assert.AreEqual("feature", repo.GetFeature("s1").JsonValue);
          Assert.AreEqual("feature", repo.GetFeature("s1").WithContext(ccEmpty).JsonValue);
          Assert.AreEqual("feature", repo.GetFeature("s1").WithContext(ccAge18Android).JsonValue);
          Assert.AreEqual("not-mobile", repo.GetFeature("s1").WithContext(ccAge18MacOS).JsonValue);
          Assert.AreEqual("older-than-twenty", repo.GetFeature("s1").WithContext(ccAge27Ios).JsonValue);
          Assert.AreEqual("not-mobile", repo.GetFeature("s1").WithContext(ccAge43MacOS).JsonValue);
          break;
      }
    }

    [Test]
    public void BasicStringStrategy()
    {
      StringTypeComparison(FeatureValueType.STRING);
    }

    [Test]
    public void BasicJson()
    {
      StringTypeComparison(FeatureValueType.JSON);
    }
  }
}
