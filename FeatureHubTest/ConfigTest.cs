
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
      var encode = new EncodeUtils();
      var cfg = new EdgeFeatureHubConfig("http://localhost:80/", encode.ClientApiKey);
      ClassicAssert.IsTrue(!cfg.ServerEvaluation);
      ClassicAssert.AreEqual($"http://localhost:80/features/{encode.ClientApiKey}", cfg.Url);

      cfg = new EdgeFeatureHubConfig("http://localhost:80", encode.ServerApiKey);
      ClassicAssert.IsTrue(cfg.ServerEvaluation);
      ClassicAssert.AreEqual($"http://localhost:80/features/{encode.ServerApiKey}", cfg.Url);
    }

    [Test]
    public void EnsureEnvConfigWorks()
    {
      var apiKey = TestUtils.ServerApiKey;
      Environment.SetEnvironmentVariable("FEATUREHUB_API_KEY", apiKey);
      Environment.SetEnvironmentVariable("FEATUREHUB_EDGE_URL", "http://localhost");
      var cfg = new EdgeFeatureHubConfig();
      ClassicAssert.AreEqual(cfg.SdkKeys.ToArray(), new string[] {apiKey});
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
        new EdgeFeatureHubConfig("http://localhost:80/", "123"));
    } 
  }
}
