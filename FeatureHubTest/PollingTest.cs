
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
        EdgeClientPoll poll;

        [SetUp]
        public void Setup()
        {
            repository = new Mock<IFeatureRepositoryContext>();
            config = new Mock<IFeatureHubConfig>();
            poll = new EdgeClientPoll(repository.Object, config.Object);
        }

        [Test]
        public void CacheControlContainsNewTimeout()
        {
            poll.DecodeCacheControl(new List<string>(new string[] {"bark, max-age=21, woof"}));
            Assert.AreEqual(21, poll.TimeoutSeconds);
            poll.DecodeCacheControl(new List<string>(new string[] {"no-store, no-age, bark, bark, bark"}));
            Assert.AreEqual(21, poll.TimeoutSeconds);
        }

        [Test]
        public void EtagHeaderContainsNewEtag()
        {
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>(HttpStatusCode.OK,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            poll.CheckForEtag(response);
            Assert.IsNull(poll.Etag);
            response.Headers["ETag"] = new List<string>(new[] {"123445"});
            poll.CheckForEtag(response);
            Assert.AreEqual(poll.Etag, "123445");
        }

        [Test]
        public void StaleEnvironmentStopsConnection()
        {
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)236,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            
            Assert.IsFalse(poll.Stopped);
            poll.DecodeResponse(response);
            Assert.IsTrue(poll.Stopped);
            
            repository.Verify(foo => foo.UpdateFeatures(It.IsAny<IEnumerable<FeatureState>>()));
        }

        [Test]
        public void ApiKey400()
        {
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)400,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            
            poll.DecodeResponse(response);
            Assert.IsTrue(poll.DeadConnection);
        }
        
        [Test]
        public void ApiKey403()
        {
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)403,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            
            poll.DecodeResponse(response);
            Assert.IsTrue(poll.DeadConnection);
        }
        
        [Test]
        public void ApiKey404()
        {
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)404,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            
            poll.DecodeResponse(response);
            Assert.IsTrue(poll.DeadConnection);
        }

        [Test]
        public void ApiKey503()
        {
            Assert.IsTrue(poll.CacheTimeout.CompareTo(DateTime.Now) < 0);
            var response = new ApiResponse<List<FeatureEnvironmentCollection>>((HttpStatusCode)503,
                new Multimap<string, string>(),
                new List<FeatureEnvironmentCollection>());
            poll.DecodeResponse(response);
            Assert.IsTrue(poll.CacheTimeout.CompareTo(DateTime.Now) > 0);
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
                    It.IsAny<System.Threading.CancellationToken >()
                ).Result).Returns(response);
            poll.SideloadApi(mockApi.Object);
            await poll.Poll();
            repository.Verify(foo => foo.UpdateFeatures(It.IsAny<IEnumerable<FeatureState>>()));
        }

        [Test]
        public async Task ErrorResponseFromApiCallStopsClient()
        {
            var mockApi = new Mock<IFeatureServiceApi>();
            var sdkKeys = new List<string>(new[] { "123" });
            config.Setup(c => c.SdkKeys).Returns(sdkKeys);
            mockApi.Setup(s => 
                s.GetFeatureStatesWithHttpInfoAsync(
                    sdkKeys, 
                    "0",
                    0,
                    It.IsAny<System.Threading.CancellationToken >()
                ).Result).Throws(new ApiException(404, "bad call"));
            poll.SideloadApi(mockApi.Object);
            await poll.Poll();
            Assert.IsTrue(poll.DeadConnection);
            repository.Verify(foo => foo.Notify(SSEResultState.Failure, null));
        }
    }
}