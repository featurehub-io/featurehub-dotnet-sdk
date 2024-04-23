
using FeatureHubSDK;
using NUnit.Framework;

namespace FeatureHubTest
{
  class ConfigTest
  {
    [Test]
    public void EnsureConfigCorrectlyDeterminesUrl()
    {
      var cfg = new EdgeFeatureHubConfig("http://localhost:80/", "id/123*123");
      Assert.IsTrue(!cfg.ServerEvaluation);
      Assert.AreEqual("http://localhost:80/features/id/123*123", cfg.Url);

      cfg = new EdgeFeatureHubConfig("http://localhost:80", "id/123");
      Assert.IsTrue(cfg.ServerEvaluation);
      Assert.AreEqual("http://localhost:80/features/id/123", cfg.Url);
    }


    [Test]
    public void InvalidKeyStructureFails()
    {
      Assert.Throws<FeatureHubSDK.FeatureHubKeyInvalidException>(() => 
        new EdgeFeatureHubConfig("http://localhost:80/", "123*123"));
    } 
  }
}
