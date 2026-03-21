using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using Moq;
using Newtonsoft.Json;

namespace FeatureHubTest
{
  sealed class TestUtils
  {
    public static string ClientApiKey => $"{Guid.NewGuid()}/123*456";
    public static string ServerApiKey => $"{Guid.NewGuid()}/123456";


  }

  sealed class EncodeUtils
  {
    public Guid EnvironmentId { get; } = Guid.NewGuid();

    public string ClientApiKey => $"{EnvironmentId}/123*456";
    public string ServerApiKey => $"{EnvironmentId}/123456";

    public string EncodeFeatures(object value, int varVersion = 1,
        FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      var feature = new FeatureState(id: Guid.NewGuid(), key: "1", varVersion: varVersion, value: value, type: type, environmentId: EnvironmentId);
      string val = JsonConvert.SerializeObject(new List<FeatureState>(new FeatureState[] { feature }));

      return val;
    }

    public string EncodeFeatures(int varVersion = 1, FeatureValueType type = FeatureValueType.BOOLEAN)
    {
      return EncodeFeatures(false, varVersion, type);
    }

  }

  public sealed class TestClientContext : BaseClientContext
  {
#pragma warning disable CA1051
    public readonly Mock<IFeatureRepositoryContext> repo;
    public readonly Mock<IFeatureHubConfig> config;
    public readonly IUsageProvider usage;
#pragma warning restore CA1051

    public TestClientContext(Mock<IFeatureRepositoryContext> repo,
        Mock<IFeatureHubConfig> config, IUsageProvider usage) : base(repo.Object, config.Object)
    {
      this.repo = repo;
      this.config = config;
      this.usage = usage;
    }

    public static TestClientContext Create()
    {
      var repo = new Mock<IFeatureRepositoryContext>();
      var config = new Mock<IFeatureHubConfig>();
      var usage = new BaseUsageProvider();

      repo.Setup(s => s.UsageProvider).Returns(usage);

      return new TestClientContext(repo, config, usage);
    }

    public override async Task<IClientContext> Build()
    {
      return this;
    }

    public override void Close()
    {
      throw new System.NotImplementedException();
    }
  }
}
