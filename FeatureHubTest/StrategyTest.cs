using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;

namespace FeatureHubTest
{
  sealed class StrategyTest
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
        environmentId: Guid.NewGuid(),
        key: "bool1", value: true, varVersion: 1, type: FeatureValueType.BOOLEAN,
          strategies: new List<FeatureRolloutStrategy>
          {
            new FeatureRolloutStrategy(id: "id", value: false, attributes: new List<FeatureRolloutStrategyAttribute>
            {
              new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.EQUALS, type: RolloutStrategyFieldType.STRING,
                fieldName: GetEnumMemberValue(StrategyAttributeWellKnownNames.Country), values: new List<object> {GetEnumMemberValue(StrategyAttributeCountryName.Turkey)})
            })
          });

      repo.UpdateFeatures(new List<FeatureState> { feature });

      var matchCC = TestClientContext.Create().Country(StrategyAttributeCountryName.Turkey);
      var unmatchCC = TestClientContext.Create().Country(StrategyAttributeCountryName.NewZealand);

      Assert.That(repo.GetFeature("bool1").WithContext(matchCC).BooleanValue, Is.EqualTo(false));
      Assert.That(repo.GetFeature("bool1").WithContext(unmatchCC).BooleanValue, Is.EqualTo(true));
      Assert.That(repo.GetFeature("bool1").BooleanValue, Is.EqualTo(true));
    }

    [Test]
    public void BasicNumberStrategy()
    {
      // given: we have a basic number feature with two custom strategies based on age
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "num1", value: 16, varVersion: 1, type: FeatureValueType.NUMBER,
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
      repo.UpdateFeatures(new List<FeatureState> { feature });

      var age27 = TestClientContext.Create().Attr("age", "27");
      var age18 = TestClientContext.Create().Attr("age", "18");
      var age43 = TestClientContext.Create().Attr("age", "43");

      // then
      Assert.That(repo.GetFeature("num1").WithContext(age27).NumberValue, Is.EqualTo(10));
      Assert.That(repo.GetFeature("num1").WithContext(age18).NumberValue, Is.EqualTo(16));
      Assert.That(repo.GetFeature("num1").WithContext(age43).NumberValue, Is.EqualTo(6));
    }

    [Test]
    public void NumberGroupStrategy()
    {
      // given: we have a grouped number feature with two custom strategies based on age
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "num1", value: 16, varVersion: 1, type: FeatureValueType.NUMBER,
        strategies: new List<FeatureRolloutStrategy>
        {
          new FeatureRolloutStrategy(id: "contractId", value: 6, attributes: new List<FeatureRolloutStrategyAttribute>
          {
            new FeatureRolloutStrategyAttribute(conditional: RolloutStrategyAttributeConditional.EQUALS, type: RolloutStrategyFieldType.NUMBER,
              fieldName: "contractId", values: new List<object> {40, 16, 23})
          }),
        });

      // when: setup repo
      repo.UpdateFeatures(new List<FeatureState> { feature });

      var oneMatch = TestClientContext.Create().Attrs("contractId", new List<String> { "3", "40", "26" });
      var noneMatch = TestClientContext.Create().Attrs("contractId", new List<String> { "3", "400", "26" });

      Assert.That(repo.GetFeature("num1").WithContext(oneMatch).NumberValue, Is.EqualTo(6));
      Assert.That(repo.GetFeature("num1").WithContext(noneMatch).NumberValue, Is.EqualTo(16));
    }

    private void StringTypeComparison(FeatureValueType ft)
    {
      // given: we have a basic string feature with two custom strategies based on age and platform
      var feature = new FeatureState(
        id: Guid.NewGuid(),
        key: "s1", value: "feature", varVersion: 1, type: ft,
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
      repo.UpdateFeatures(new List<FeatureState> { feature });

      var ccAge27Ios = TestClientContext.Create().Platform(StrategyAttributePlatformName.Ios).Attr("age", "27");
      var ccAge18Android = TestClientContext.Create().Platform(StrategyAttributePlatformName.Android).Attr("age", "18");
      var ccAge43MacOS = TestClientContext.Create().Platform(StrategyAttributePlatformName.Macos).Attr("age", "43");
      var ccAge18MacOS = TestClientContext.Create().Platform(StrategyAttributePlatformName.Macos).Attr("age", "18");
      var ccEmpty = TestClientContext.Create();

      switch (ft)
      {
        case FeatureValueType.STRING:
          // then
          Assert.That(repo.GetFeature("s1").StringValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccEmpty).StringValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge18Android).StringValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge18MacOS).StringValue, Is.EqualTo("not-mobile"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge27Ios).StringValue, Is.EqualTo("older-than-twenty"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge43MacOS).StringValue, Is.EqualTo("not-mobile"));
          break;
        case FeatureValueType.JSON:
          Assert.That(repo.GetFeature("s1").JsonValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccEmpty).JsonValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge18Android).JsonValue, Is.EqualTo("feature"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge18MacOS).JsonValue, Is.EqualTo("not-mobile"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge27Ios).JsonValue, Is.EqualTo("older-than-twenty"));
          Assert.That(repo.GetFeature("s1").WithContext(ccAge43MacOS).JsonValue, Is.EqualTo("not-mobile"));
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
