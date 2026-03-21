using System;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using LaunchDarkly.EventSource;
using Moq;
using NUnit.Framework;

namespace FeatureHubTest
{
    [TestFixture]
    public class StreamingEdgeServiceTest
    {
        private Mock<IFeatureRepositoryContext> _repository;
        private Mock<IFeatureHubConfig> _config;
        private Mock<IEventSourceFactory> _factory;
        private Mock<IEventSource> _eventSource;
        private Guid _envId;
        private StreamingEdgeService _service;

        [SetUp]
        public void Setup()
        {
            _repository = new Mock<IFeatureRepositoryContext>();
            _config = new Mock<IFeatureHubConfig>();
            _factory = new Mock<IEventSourceFactory>();
            _eventSource = new Mock<IEventSource>();
            _envId = Guid.NewGuid();

            _config.Setup(c => c.ServerEvaluation).Returns(false);
            _config.Setup(c => c.Url).Returns("http://localhost:8085/features/test-key");
            _config.Setup(c => c.EnvironmentId).Returns(_envId);

            _factory.Setup(f => f.Create(It.IsAny<Configuration>())).Returns(_eventSource.Object);
            _eventSource.Setup(e => e.StartAsync()).Returns(Task.CompletedTask);

            _service = new StreamingEdgeService(_repository.Object, _config.Object, _factory.Object);
        }

        // Trigger Init() so that _eventSource is set, enabling Close() verification
        private void TriggerInit()
        {
            _service.ContextChange("").GetAwaiter().GetResult();
        }

        // ---- ProcessMessage: event routing ----

        [Test]
        public void Features_NotifiesRepository()
        {
            var data = "[]";
            _service.ProcessMessage("features", data);
            _repository.Verify(r => r.Notify(SSEResultState.Features, data, _envId), Times.Once);
        }

        [Test]
        public void Feature_NotifiesRepository()
        {
            var data = "{}";
            _service.ProcessMessage("feature", data);
            _repository.Verify(r => r.Notify(SSEResultState.Feature, data, _envId), Times.Once);
        }

        [Test]
        public void DeleteFeature_NotifiesRepository()
        {
            var data = "{}";
            _service.ProcessMessage("delete_feature", data);
            _repository.Verify(r => r.Notify(SSEResultState.DeleteFeature, data, _envId), Times.Once);
        }

        [Test]
        public void Failure_NotifiesRepositoryAndClosesEventSource()
        {
            TriggerInit();
            var data = "{}";
            _service.ProcessMessage("failure", data);
            _repository.Verify(r => r.Notify(SSEResultState.Failure, data, _envId), Times.Once);
            _eventSource.Verify(e => e.Close(), Times.Once);
        }

        [Test]
        public void Bye_DoesNotNotify()
        {
            _service.ProcessMessage("bye", null);
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Test]
        public void Ack_DoesNotNotify()
        {
            _service.ProcessMessage("ack", null);
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Test]
        public void UnknownEvent_DoesNotNotify()
        {
            _service.ProcessMessage("some-unknown-event", null);
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
        }

        // ---- ProcessMessage: config event ----

        [Test]
        public void ConfigStaleTrue_SetsClosedAndClosesEventSource()
        {
            TriggerInit();
            _service.ProcessMessage("config", "{\"edge.stale\":true}");
            Assert.That(_service.IsClosed, Is.True);
            _eventSource.Verify(e => e.Close(), Times.Once);
        }

        [Test]
        public void ConfigStaleTrue_DoesNotNotifyRepository()
        {
            _service.ProcessMessage("config", "{\"edge.stale\":true}");
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Test]
        public void ConfigStaleFalse_DoesNotCloseAndIsNotClosed()
        {
            _service.ProcessMessage("config", "{\"edge.stale\":false}");
            Assert.That(_service.IsClosed, Is.False);
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Test]
        public void ConfigNullData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.ProcessMessage("config", null));
        }

        // ---- ProcessError ----

        [Test]
        public void ProcessError_503_IsIgnored()
        {
            _service.ProcessError(503);
            _repository.Verify(r => r.Notify(It.IsAny<SSEResultState>(), It.IsAny<string>(), It.IsAny<Guid>()),
                Times.Never);
            Assert.That(_service.IsClosed, Is.False);
        }

        [Test]
        public void ProcessError_400_NotifiesFailureAndSetsClosed()
        {
            _service.ProcessError(400);
            _repository.Verify(r => r.Notify(SSEResultState.Failure, null, _envId), Times.Once);
            Assert.That(_service.IsClosed, Is.True);
        }

        [Test]
        public void ProcessError_403_NotifiesFailureAndSetsClosed()
        {
            _service.ProcessError(403);
            _repository.Verify(r => r.Notify(SSEResultState.Failure, null, _envId), Times.Once);
            Assert.That(_service.IsClosed, Is.True);
        }

        [Test]
        public void ProcessError_NonHttp_ClosesEventSource()
        {
            TriggerInit();
            _service.ProcessError(404);
            _eventSource.Verify(e => e.Close(), Times.Once);
        }

        // ---- ContextChange ----

        [Test]
        public async Task ContextChange_WhenClosed_DoesNotCallFactory()
        {
            _service.ProcessError(400); // sets _closed = true
            await _service.ContextChange("any-header");
            _factory.Verify(f => f.Create(It.IsAny<Configuration>()), Times.Never);
        }

        [Test]
        public async Task ContextChange_ClientEval_FirstCall_CallsInit()
        {
            // client eval, _eventSource is null → Init() called
            await _service.ContextChange("");
            _factory.Verify(f => f.Create(It.IsAny<Configuration>()), Times.Once);
        }

        [Test]
        public async Task ContextChange_ClientEval_SecondCall_DoesNotReinit()
        {
            await _service.ContextChange(""); // sets _eventSource
            await _service.ContextChange(""); // _eventSource already set → no reinit
            _factory.Verify(f => f.Create(It.IsAny<Configuration>()), Times.Once);
        }

        [Test]
        public async Task ContextChange_ServerEval_SameHeader_DoesNotCallFactory()
        {
            _config.Setup(c => c.ServerEvaluation).Returns(true);
            _service = new StreamingEdgeService(_repository.Object, _config.Object, _factory.Object);
            _service.XFeatureHubHeader = "existing-header";

            await _service.ContextChange("existing-header");

            _factory.Verify(f => f.Create(It.IsAny<Configuration>()), Times.Never);
        }

        // ---- Close ----

        [Test]
        public void Close_WhenEventSourceIsNull_DoesNotThrow()
        {
            // _eventSource is null (Init never called)
            Assert.DoesNotThrow(() => _service.Close());
        }

        [Test]
        public void Close_WhenEventSourceExists_ClosesIt()
        {
            TriggerInit();
            _service.Close();
            _eventSource.Verify(e => e.Close(), Times.Once);
        }
    }
}