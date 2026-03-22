
using System;
using FeatureHubSDK;
using NUnit.Framework;

namespace FeatureHubTest
{
  sealed class ConfigTest
  {
    [Test]
    public void EnsureConfigCorrectlyDeterminesUrl()
    {
      var encode = new EncodeUtils();
      var cfg = new EdgeFeatureHubConfig("http://localhost:80/", encode.ClientApiKey);
      Assert.That(cfg.ServerEvaluation, Is.False);
      Assert.That(cfg.Url, Is.EqualTo($"http://localhost:80/features/{encode.ClientApiKey}"));

      cfg = new EdgeFeatureHubConfig("http://localhost:80", encode.ServerApiKey);
      Assert.That(cfg.ServerEvaluation, Is.True);
      Assert.That(cfg.Url, Is.EqualTo($"http://localhost:80/features/{encode.ServerApiKey}"));
    }

    [Test]
    public void EnsureEnvConfigWorks()
    {
      var apiKey = TestUtils.ServerApiKey;
      Environment.SetEnvironmentVariable("FEATUREHUB_API_KEY", apiKey);
      Environment.SetEnvironmentVariable("FEATUREHUB_EDGE_URL", "http://localhost");
      var cfg = new EdgeFeatureHubConfig();
      Assert.That(cfg.SdkKeys.ToArray(), Is.EqualTo(new string[] { apiKey }));
      Assert.That(cfg.EdgeUrl, Is.EqualTo("http://localhost"));
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
      Assert.Throws<FeatureHubSDK.FeatureHubKeyInvalidException>(() =>
        new EdgeFeatureHubConfig("http://localhost:80/", "123"));
    }
  }
}
