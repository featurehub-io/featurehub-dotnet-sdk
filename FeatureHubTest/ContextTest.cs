

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
      var ctx = await new ServerEvalFeatureContext(_repository, null,  edgeStub)
        .Attr("city", "Istanbul City")
        .Attrs("family", new List<String> {"Bambam", "DJ Elif"})
        .Country(StrategyAttributeCountryName.Turkey)
        .Platform(StrategyAttributePlatformName.Ios)
        .Device(StrategyAttributeDeviceName.Mobile)
        .UserKey("tv-show")
        .Version("6.2.3")
        .SessionKey("session-key")
        .Build();

      ClassicAssert.AreEqual(_repository, ctx.Repository);
      ClassicAssert.AreEqual("Istanbul City", ctx.GetAttr("city", "here"));
      ClassicAssert.AreEqual("here", ctx.GetAttr("city-scape", "here"));

      ClassicAssert.AreEqual(
        "city=Istanbul+City,country=turkey,device=mobile,family=Bambam%2cDJ+Elif,platform=ios,session=session-key,userkey=tv-show,version=6.2.3", edgeStub.header);

      ClassicAssert.NotNull(ctx.ToString());

      ClassicAssert.NotNull(ctx["fred"]);

      await ctx.Clear().Build();
      ClassicAssert.AreEqual("", edgeStub.header);
    }
    
    [Test]
    async public Task EnabledFlagWorksIsTrueOnlyOnTrue()
    {
      var ctx = new ClientEvalFeatureContext(_repository, null);
      ClassicAssert.AreEqual(false, ctx.IsSet("1"));
      ClassicAssert.AreEqual(false, ctx.IsEnabled("1"));
      Guid id = Guid.NewGuid();
      _repository.Notify(SSEResultState.Features, 
        _encodeUtils.EncodeFeatures(true, 2, FeatureValueType.BOOLEAN), id);
      ClassicAssert.AreEqual(true, ctx.IsEnabled("1"));
      _repository.Notify(SSEResultState.Features, _encodeUtils.EncodeFeatures(false, 3, FeatureValueType.BOOLEAN), id);
      ClassicAssert.AreEqual(false, ctx.IsEnabled("1"));
      ClassicAssert.AreEqual(true, ctx.IsSet("1"));
      
      await ctx.Build();
      ctx.Close();
    }

  }
}
