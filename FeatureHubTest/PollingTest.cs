
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
    class PollingTest
    {
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

        [Test]
        public void CacheControlContainsNewTimeout()
        {
            poll.DecodeCacheControl(new List<string>(new string[] {"bark, max-age=21, woof"}));
            Assert.That(poll.TimeoutSeconds, Is.EqualTo(21));
            poll.DecodeCacheControl(new List<string>(new string[] {"no-store, no-age, bark, bark, bark"}));
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
            response.Headers["ETag"] = new List<string>(new[] {"123445"});
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
            var sdkKeys = new List<string>(new[] { "123" });
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
    }
}