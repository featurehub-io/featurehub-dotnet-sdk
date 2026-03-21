

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using Moq;
using NUnit.Framework;

namespace FeatureHubTest
{
  class TestPercentageCalculator : IPercentageCalculator
  {
    public int pc = 21;

    public int DetermineClientPercentage(string percentageText, Guid featureId)
    {
      return pc;
    }
  }



  class ApplyFeatureTest
  {
    private ApplyFeature _applyFeature;
    private TestPercentageCalculator _percentageCalculator;


    [SetUp]
    public void Setup()
    {
      _percentageCalculator = new TestPercentageCalculator();
      _applyFeature = new ApplyFeature(_percentageCalculator, new MatcherRegistry());
    }

    [Test]
    public void NullContextDefaultValue()
    {
      // given: we have a rollout strategy that is percentage based
      var rs = new FeatureRolloutStrategy("id");
      rs.Percentage = 21;
      rs.Value = "blue";

      // and: we have a context
      var cc = TestClientContext.Create().UserKey("mary@mary.com");

      var val = _applyFeature.Apply(new List<FeatureRolloutStrategy> { rs }, "fred", Guid.NewGuid(), null);

      Assert.That(val.Matched, Is.EqualTo(false));
    }

    [Test, TestCaseSource("BasicPercentProvider")]
    public void MatchPercentageToCalculation(int underPercent, string expected, bool matched)
    {
      // given: we have a rollout strategy that is percentage based
      var rs = new FeatureRolloutStrategy("id");
      rs.Percentage = underPercent;
      rs.Value = "blue";

      // and: we have a context
      var cc = TestClientContext.Create().UserKey("mary@mary.com");

      var val = _applyFeature.Apply(new List<FeatureRolloutStrategy> { rs }, "fred", Guid.NewGuid(), cc);

      Assert.That(val.Value, Is.EqualTo(expected));
      Assert.That(val.Matched, Is.EqualTo(matched));
    }

    public static IEnumerable<TestCaseData> BasicPercentProvider()
    {
      yield return new TestCaseData(22, "blue", true);
      yield return new TestCaseData(75, "blue", true);
      yield return new TestCaseData(15, null, false);
      yield return new TestCaseData(20, null, false);
    }

    [Test, TestCaseSource("NoStrategyPercentProvider")]
    public void NoRolloutStrategy(int underPercent, string expected, bool matched)
    {
      // and: we have a context
      var cc = TestClientContext.Create().UserKey("mary@mary.com");

      var val = _applyFeature.Apply(new List<FeatureRolloutStrategy> { }, "fred", Guid.NewGuid(), cc);

      Assert.That(val.Value, Is.EqualTo(expected));
      Assert.That(val.Matched, Is.EqualTo(matched));
    }

    public static IEnumerable<TestCaseData> NoStrategyPercentProvider()
    {
      yield return new TestCaseData(22, null, false);
      yield return new TestCaseData(75, null, false);
      yield return new TestCaseData(15, null, false);
    }
  }
}
