

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;

namespace FeatureHubTest
{
  public class ContextTest
  {
    FeatureHubRepository _repository;
    private EncodeUtils _encodeUtils = null;

    [SetUp]
    public void Setup()
    {
      _repository = new FeatureHubRepository();
      _encodeUtils = new EncodeUtils();
    }

    internal class EdgeServiceStub : IEdgeService
    {
      public string header;
      public int closeCalled = 0;
      public bool replace = false;

      public async Task ContextChange(string header)
      {
        this.header = header;
      }

      public bool ClientEvaluation => true;
      public bool IsRequiresReplacementOnHeaderChange => replace;

      public void Close()
      {
        closeCalled++;
      }

      public Task Poll()
      {
        return Task.CompletedTask;
      }
    }

    [Test]
    async public Task ChangeInContextFiresRequestToEdgeService()
    {
      var edgeStub = new EdgeServiceStub();
      var ctx = await new ServerEvalFeatureContext(_repository, null, edgeStub)
        .Attr("city", "Istanbul City")
        .Attrs("family", new List<String> { "Bambam", "DJ Elif" })
        .Country(StrategyAttributeCountryName.Turkey)
        .Platform(StrategyAttributePlatformName.Ios)
        .Device(StrategyAttributeDeviceName.Mobile)
        .UserKey("tv-show")
        .Version("6.2.3")
        .SessionKey("session-key")
        .Build();

      Assert.That(ctx.Repository, Is.EqualTo(_repository));
      Assert.That(ctx.GetAttr("city", "here"), Is.EqualTo("Istanbul City"));
      Assert.That(ctx.GetAttr("city-scape", "here"), Is.EqualTo("here"));

      Assert.That(edgeStub.header, Is.EqualTo(
        "city=Istanbul+City,country=turkey,device=mobile,family=Bambam%2cDJ+Elif,platform=ios,session=session-key,userkey=tv-show,version=6.2.3"));

      Assert.That(ctx.ToString(), Is.Not.Null);

      Assert.That(ctx["fred"], Is.Not.Null);

      await ctx.Clear().Build();
      Assert.That(edgeStub.header, Is.EqualTo(""));
    }

    [Test]
    async public Task EnabledFlagWorksIsTrueOnlyOnTrue()
    {
      var ctx = new ClientEvalFeatureContext(_repository, null);
      Assert.That(ctx.IsSet("1"), Is.EqualTo(false));
      Assert.That(ctx.IsEnabled("1"), Is.EqualTo(false));
      Guid id = Guid.NewGuid();
      _repository.Notify(SSEResultState.Features,
        _encodeUtils.EncodeFeatures(true, 2, FeatureValueType.BOOLEAN), id);
      Assert.That(ctx.IsEnabled("1"), Is.EqualTo(true));
      _repository.Notify(SSEResultState.Features, _encodeUtils.EncodeFeatures(false, 3, FeatureValueType.BOOLEAN), id);
      Assert.That(ctx.IsEnabled("1"), Is.EqualTo(false));
      Assert.That(ctx.IsSet("1"), Is.EqualTo(true));

      await ctx.Build();
      ctx.Close();
    }

  }
}
