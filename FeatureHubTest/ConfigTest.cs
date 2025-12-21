
using System;
using FeatureHubSDK;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FeatureHubTest
{
  class ConfigTest
  {
    [Test]
    public void EnsureConfigCorrectlyDeterminesUrl()
    {
      var cfg = new EdgeFeatureHubConfig("http://localhost:80/", "id/123*123");
      ClassicAssert.IsTrue(!cfg.ServerEvaluation);
      ClassicAssert.AreEqual("http://localhost:80/features/id/123*123", cfg.Url);

      cfg = new EdgeFeatureHubConfig("http://localhost:80", "id/123");
      ClassicAssert.IsTrue(cfg.ServerEvaluation);
      ClassicAssert.AreEqual("http://localhost:80/features/id/123", cfg.Url);
    }

    [Test]
    public void EnsureEnvConfigWorks()
    {
      Environment.SetEnvironmentVariable("FEATUREHUB_API_KEY", "id/123");
      Environment.SetEnvironmentVariable("FEATUREHUB_EDGE_URL", "http://localhost");
      var cfg = new EdgeFeatureHubConfig();
      ClassicAssert.AreEqual(cfg.SdkKeys.ToArray(), new string[] {"id/123"});
      ClassicAssert.AreEqual(cfg.EdgeUrl, "http://localhost");
    }

    [TearDown]
    public void after()
    {
      Environment.SetEnvironmentVariable("FEATUREHUB_API_KEY", null);
      Environment.SetEnvironmentVariable("FEATUREHUB_EDGE_URL", null);
      
    }


    [Test]
    public void InvalidKeyStructureFails()
    {
      ClassicAssert.Throws<FeatureHubSDK.FeatureHubKeyInvalidException>(() => 
        new EdgeFeatureHubConfig("http://localhost:80/", "123*123"));
    } 
  }
}
