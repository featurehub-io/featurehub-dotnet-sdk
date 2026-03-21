
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Api;
using IO.FeatureHub.SSE.Client;
using IO.FeatureHub.SSE.Model;
using Moq;
using NUnit.Framework;

namespace FeatureHubTest
{
#pragma warning disable CA1001 // poll is disposed in [TearDown]
  sealed class PollingTest
#pragma warning restore CA1001
  {
    private static readonly string[] CacheControlWithMaxAge = { "bark, max-age=21, woof" };
    private static readonly string[] CacheControlNoAge = { "no-store, no-age, bark, bark, bark" };
    private static readonly string[] EtagValue = { "123445" };
    private static readonly string[] SingleSdkKey = { "123" };
    private static readonly string[] SingleKey1 = { "key1" };

    Mock<IFeatureRepositoryContext> repository;
    Mock<IFeatureHubConfig> config;
    PollingEdgeService poll;

    [SetUp]
    public void Setup()
    {
      repository = new Mock<IFeatureRepositoryContext>();
      config = new Mock<IFeatureHubConfig>();
      poll = new PollingEdgeService(repository.Object, config.Object);
    }

    [TearDown]
    public void TearDown()
    {
      poll?.Dispose();
    }

    [Test]
    public void CacheControlContainsNewTimeout()
    {
      poll.DecodeCacheControl(new List<string>(CacheControlWithMaxAge));
      Assert.That(poll.TimeoutSeconds, Is.EqualTo(21));
      poll.DecodeCacheControl(new List<string>(CacheControlNoAge));
      Assert.That(poll.TimeoutSeconds, Is.EqualTo(21));
    }

    [Test]
    public void EtagHeaderContainsNewEtag()
    {
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>(HttpStatusCode.OK,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());
      poll.CheckForEtag(response);
      Assert.That(poll.Etag, Is.Null);
      response.Headers["ETag"] = new List<string>(EtagValue);
      poll.CheckForEtag(response);
      Assert.That(poll.Etag, Is.EqualTo("123445"));
    }

    [Test]
    public void StaleEnvironmentStopsConnection()
    {
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)236,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());

      Assert.That(poll.Stopped, Is.False);
      poll.DecodeResponse(response);
      Assert.That(poll.Stopped, Is.True);

      repository.Verify(foo => foo.UpdateFeatures(It.IsAny<IEnumerable<FeatureState>>()));
    }

    [Test]
    public void ApiKey400()
    {
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)400,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());

      poll.DecodeResponse(response);
      Assert.That(poll.DeadConnection, Is.True);
    }

    [Test]
    public void ApiKey403()
    {
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)403,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());

      poll.DecodeResponse(response);
      Assert.That(poll.DeadConnection, Is.True);
    }

    [Test]
    public void ApiKey404()
    {
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)404,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());

      poll.DecodeResponse(response);
      Assert.That(poll.DeadConnection, Is.True);
    }

    [Test]
    public void ApiKey503()
    {
      Assert.That(poll.CacheTimeout.CompareTo(DateTime.Now), Is.LessThan(0));
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)503,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());
      poll.DecodeResponse(response);
      Assert.That(poll.CacheTimeout.CompareTo(DateTime.Now), Is.GreaterThan(0));
    }

    [Test]
    public async Task ExpiredCacheCausesPoll()
    {
      var mockApi = new Mock<IFeatureServiceApi>();
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)236,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());
      var sdkKeys = new List<string>(SingleSdkKey);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      mockApi.Setup(s =>
          s.GetFeatureStatesWithHttpInfoAsync(
              sdkKeys,
              "0",
              0,
              It.IsAny<System.Threading.CancellationToken>()
          ).Result).Returns(response);
      poll.SideloadApi(mockApi.Object);
      await poll.Poll();
      repository.Verify(foo => foo.UpdateFeatures(It.IsAny<IEnumerable<FeatureState>>()));
    }

    [Test]
    public async Task ErrorResponseFromApiCallStopsClient()
    {
      var mockApi = new Mock<IFeatureServiceApi>();
      var encode = new EncodeUtils();
      var sdkKeys = new List<string>(new[] { encode.ClientApiKey });
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      config.Setup(c => c.EnvironmentId).Returns(encode.EnvironmentId);
      mockApi.Setup(s =>
          s.GetFeatureStatesWithHttpInfoAsync(
              sdkKeys,
              "0",
              0,
              It.IsAny<System.Threading.CancellationToken>()
          ).Result).Throws(new ApiException(404, "bad call"));
      poll.SideloadApi(mockApi.Object);
      await poll.Poll();
      Assert.That(poll.DeadConnection, Is.True);
      repository.Verify(foo => foo.Notify(SSEResultState.Failure, null, encode.EnvironmentId));
    }

    // ---- ActiveRest timer tests ----

    private PollingEdgeService ActiveRestPoll(Mock<IFeatureServiceApi> mockApi, List<string> sdkKeys,
        int timeoutSeconds = 60)
    {
      var activePoll = new PollingEdgeService(repository.Object, config.Object,
          timeoutSeconds, EdgeType.ActiveRest);
      activePoll.SideloadApi(mockApi.Object);
      return activePoll;
    }

    private static Mock<IFeatureServiceApi> OkApiMock(List<string> sdkKeys)
    {
      var mockApi = new Mock<IFeatureServiceApi>();
      var response = new ApiResponse<List<FeatureEnvironmentCollection>>(
          System.Net.HttpStatusCode.OK,
          new Multimap<string, string>(),
          new List<FeatureEnvironmentCollection>());
      mockApi.Setup(s =>
          s.GetFeatureStatesWithHttpInfoAsync(sdkKeys, "0", 0,
              It.IsAny<System.Threading.CancellationToken>()).Result).Returns(response);
      return mockApi;
    }

    [Test]
    public async Task ActiveRest_FirstPollCallsApi()
    {
      var sdkKeys = new List<string>(SingleKey1);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      var mockApi = OkApiMock(sdkKeys);
      var activePoll = ActiveRestPoll(mockApi, sdkKeys);

      await activePoll.Poll();

      mockApi.Verify(s => s.GetFeatureStatesWithHttpInfoAsync(
          sdkKeys, "0", 0, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ActiveRest_TimerIsActiveAfterFirstPoll()
    {
      var sdkKeys = new List<string>(SingleKey1);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      var mockApi = OkApiMock(sdkKeys);
      var activePoll = ActiveRestPoll(mockApi, sdkKeys);

      Assert.That(activePoll.TimerActive, Is.False);
      await activePoll.Poll();
      Assert.That(activePoll.TimerActive, Is.True);
    }

    [Test]
    public async Task ActiveRest_SecondPollIgnoredWhileTimerActive()
    {
      var sdkKeys = new List<string>(SingleKey1);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      var mockApi = OkApiMock(sdkKeys);
      var activePoll = ActiveRestPoll(mockApi, sdkKeys);

      await activePoll.Poll();  // first poll: calls API, starts timer
      await activePoll.Poll();  // second poll: timer active, should be ignored

      mockApi.Verify(s => s.GetFeatureStatesWithHttpInfoAsync(
          sdkKeys, "0", 0, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ActiveRest_TimerFiresNewPoll()
    {
      var sdkKeys = new List<string>(SingleKey1);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      var mockApi = OkApiMock(sdkKeys);
      // use a very short timeout so the timer fires quickly in the test
      var activePoll = ActiveRestPoll(mockApi, sdkKeys, timeoutSeconds: 0);

      await activePoll.Poll();  // first poll, timer fires almost immediately

      // wait long enough for the timer callback to have fired and the second poll to complete
      await Task.Delay(200);

      mockApi.Verify(s => s.GetFeatureStatesWithHttpInfoAsync(
          sdkKeys, "0", 0, It.IsAny<System.Threading.CancellationToken>()), Times.AtLeast(2));
    }

    [Test]
    public async Task ActiveRest_DeadConnectionStopsTimer()
    {
      var sdkKeys = new List<string>(SingleKey1);
      var encode = new EncodeUtils();
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      config.Setup(c => c.EnvironmentId).Returns(encode.EnvironmentId);
      var mockApi = new Mock<IFeatureServiceApi>();
      mockApi.Setup(s =>
          s.GetFeatureStatesWithHttpInfoAsync(sdkKeys, "0", 0,
              It.IsAny<System.Threading.CancellationToken>()).Result)
          .Throws(new ApiException(403, "forbidden"));
      var activePoll = ActiveRestPoll(mockApi, sdkKeys);

      await activePoll.Poll();

      Assert.That(activePoll.DeadConnection, Is.True);
      Assert.That(activePoll.TimerActive, Is.False);
    }

    [Test]
    public async Task ActiveRest_CloseDisposesTimer()
    {
      var sdkKeys = new List<string>(SingleKey1);
      config.Setup(c => c.SdkKeys).Returns(sdkKeys);
      var mockApi = OkApiMock(sdkKeys);
      var activePoll = ActiveRestPoll(mockApi, sdkKeys);

      await activePoll.Poll();
      Assert.That(activePoll.TimerActive, Is.True);

      activePoll.Close();
      Assert.That(activePoll.TimerActive, Is.False);
      Assert.That(activePoll.Stopped, Is.True);
    }
  }
}
