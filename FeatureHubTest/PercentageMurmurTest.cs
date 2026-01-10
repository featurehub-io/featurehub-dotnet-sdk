

using System;
using FeatureHubSDK;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FeatureHubTest
{
  class PercentageMurmurTest
  {
    [Test]
    public void BasicMumurTest()
    {
      var featureId = Guid.NewGuid().ToString();
      var calc = new PercentageMurmur3Calculator();

      var counter = 0;
      for (var count = 0; count < 1000; count++)
      {
        if (calc.DetermineClientPercentage(Guid.NewGuid().ToString(), Guid.NewGuid()) <= 200000)
        {
          counter++;
        }
      }
      Console.WriteLine($"Murmur counter is {counter}");
      ClassicAssert.LessOrEqual(160, counter);
      ClassicAssert.GreaterOrEqual(240, counter);
    }
  }
}
