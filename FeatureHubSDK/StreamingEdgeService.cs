using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IO.FeatureHub.SSE.Model;
using LaunchDarkly.EventSource;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("FeatureHubTest")]

namespace FeatureHubSDK
{
  public interface IEdgeService
  {
    Task ContextChange(string header);
    bool ClientEvaluation { get; }

    void Close();
    Task Poll();
  }

  public class ExceptionEvent
  {
    public readonly String Message;
    public readonly Exception Exception;

    public ExceptionEvent(string message, Exception exception)
    {
      this.Message = message;
      this.Exception = exception;
    }
  }

  public static class FeatureLogging
  {
    // Attach event handler to receive Trace level logs
    public static EventHandler<string> TraceLogger = (sender, args) => { };
    // Attach event handler to receive Debug level logs
    public static EventHandler<string> DebugLogger = (sender, args) => { };
    // Attach event handler to receive Info level logs
    public static EventHandler<string> InfoLogger = (sender, args) => { };
    // Attach event handler to receive Warn level logs
    public static EventHandler<string> WarnLogger = (sender, args) => { };
    // Attach event handler to receive Error level logs
    public static EventHandler<string> ErrorLogger = (sender, args) => { };
    public static EventHandler<ExceptionEvent> ExceptionLogger = (sender, args) => { };
  }

  class ConfigData
  {
    [JsonProperty("edge.stale")]
    public Boolean Stale { get; set; }
  }

  /// <summary>
  /// Factory for creating IEventSource instances. Inject a mock in tests to avoid real SSE connections.
  /// </summary>
  public interface IEventSourceFactory
  {
    IEventSource Create(Configuration config);
  }

  internal class DefaultEventSourceFactory : IEventSourceFactory
  {
    public IEventSource Create(Configuration config) => new EventSource(config);
  }

  public class StreamingEdgeService : IEdgeService
  {
    private IEventSource _eventSource;
    private readonly IFeatureHubConfig _config;
    private readonly IFeatureRepositoryContext _repository;
    private readonly IEventSourceFactory _eventSourceFactory;
    private string _xFeatureHubHeader;
    private bool _closed;
    public EventHandler<ConfigurationBuilder> ConfigModificationHook = delegate { };

    internal bool IsClosed => _closed;

    // Exposed for testing server-eval header-change scenarios
    internal string XFeatureHubHeader
    {
      get => _xFeatureHubHeader;
      set => _xFeatureHubHeader = value;
    }

    public StreamingEdgeService(IFeatureRepositoryContext repository, IFeatureHubConfig config,
      IEventSourceFactory eventSourceFactory = null)
    {
      _repository = repository;
      _config = config;
      _eventSourceFactory = eventSourceFactory ?? new DefaultEventSourceFactory();

      // tell the repository about how evaluation works
      // this means features don't need to know about the IEdgeService
      _repository.ServerSideEvaluation = config.ServerEvaluation;
    }

    public async Task ContextChange(string newHeader)
    {
      if (_closed)
        return;

      if (_config.ServerEvaluation)
      {
        if (newHeader != _xFeatureHubHeader)
        {
          _xFeatureHubHeader = newHeader;

          if (_eventSource == null || _eventSource.ReadyState == ReadyState.Open ||
              _eventSource.ReadyState == ReadyState.Connecting)
          {
            _eventSource?.Close();
            _eventSource = null;
            await Poll();
          }
        }
      }
      else if (_eventSource == null)
      {
        Init();
      }
    }

    public bool ClientEvaluation => !_config.ServerEvaluation;

    private Dictionary<string, string> BuildContextHeader()
    {
      var headers = new Dictionary<string, string>();

      if (_config.ServerEvaluation && _xFeatureHubHeader != null)
      {
        headers.Add("x-featurehub", _xFeatureHubHeader);
      }

      return headers;
    }

    private string DefaultEnvConfig(string envVar, string defaultValue)
    {
      return Environment.GetEnvironmentVariable(envVar) ?? defaultValue;
    }

    /// <summary>
    /// Handles an HTTP error status from the event source connection.
    /// Extracted for testability — call directly in tests rather than triggering SSE errors.
    /// </summary>
    internal void ProcessError(int statusCode)
    {
      if (statusCode == 503)
        return;
      _repository.Notify(SSEResultState.Failure, null, _config.EnvironmentId);
      FeatureLogging.ErrorLogger(this, "Server issued a failure, stopping.");
      _closed = true;
      _eventSource?.Close();
    }

    /// <summary>
    /// Handles an incoming SSE message by name and payload.
    /// Extracted for testability — call directly in tests rather than firing SSE events.
    /// </summary>
    internal void ProcessMessage(string eventName, string data)
    {
      SSEResultState? state;
      FeatureLogging.TraceLogger(this, $"received {eventName} : {data}");
      switch (eventName)
      {
        case "features":
          state = SSEResultState.Features;
          if (FeatureLogging.TraceLogger != null)
          {
            FeatureLogging.TraceLogger(this, "featurehub: Features are available...");
          }
          break;
        case "feature":
          state = SSEResultState.Feature;
          break;
        case "failure":
          state = SSEResultState.Failure;
          break;
        case "delete_feature":
          state = SSEResultState.DeleteFeature;
          break;
        case "bye":
          state = null;
          if (FeatureLogging.TraceLogger != null)
          {
            FeatureLogging.TraceLogger(this, "featurehub: renewing connection process started");
          }
          break;
        case "config":
          state = SSEResultState.Config;
          if (data != null)
          {
            var configData = JsonConvert.DeserializeObject<ConfigData>(data);
            if (configData.Stale)
            {
              if (FeatureLogging.ErrorLogger != null)
              {
                FeatureLogging.ErrorLogger(this,
                  "featurehub: environment has gone stale, closing connection and won't reopen");
              }
              _closed = true;
              _eventSource?.Close();
            }
          }
          break;
        case "ack":
          state = null;
          break;
        default:
          FeatureLogging.ErrorLogger(this, $"featurehub: received unknown event {eventName}");
          state = null;
          break;
      }

      if (FeatureLogging.TraceLogger != null)
        FeatureLogging.TraceLogger(this, $"featurehub: The state was {state} with value {data}");

      if (state == null)
        return;

      if (state != SSEResultState.Config)
      {
        _repository.Notify(state.Value, data, _config.EnvironmentId);
      }

      if (state == SSEResultState.Failure)
      {
        if (FeatureLogging.ErrorLogger != null)
        {
          FeatureLogging.ErrorLogger(this, "featurehub: received a failure so closing and not restarting");
        }
        _eventSource?.Close();
      }
    }

    public void Init()
    {

      if (_closed)
        return;

      var configBuilder = Configuration.Builder(uri: new UriBuilder(_config.Url).Uri)
        .BackoffResetThreshold(
          TimeSpan.FromMinutes(int.Parse(DefaultEnvConfig("FEATUREHUB_BACKOFF_RESET_THRESHOLD", "1"))))
        .RequestHeaders(_config.ServerEvaluation ? BuildContextHeader() : null)
        .MaxRetryDelay(
          TimeSpan.FromMilliseconds(int.Parse(DefaultEnvConfig("FEATUREHUB_MAX_DELAY_RETRY_MS", "20000"))))
        .InitialRetryDelay(
          TimeSpan.FromMilliseconds(int.Parse(DefaultEnvConfig("FEATUREHUB_DELAY_RETRY_MS", "500"))));

      // in case the user wants to modify the config
      ConfigModificationHook(this, configBuilder);

      var eventSourceConfig = configBuilder.Build();

      if (FeatureLogging.InfoLogger != null)
      {
        FeatureLogging.InfoLogger(this, $"Opening connection to ${_config.Url}");
      }

      _eventSource = _eventSourceFactory.Create(eventSourceConfig);

      _eventSource.Error += (sender, ex) =>
      {
        if (!(ex.Exception is EventSourceServiceUnsuccessfulResponseException result))
          return;
        ProcessError(result.StatusCode);
      };

      _eventSource.MessageReceived += (sender, args) =>
      {
        ProcessMessage(args.EventName, args.Message.Data);
      };

      _eventSource.StartAsync();
    }

    public void Close()
    {
      _eventSource?.Close();
    }

    public async Task Poll()
    {
      if (_eventSource == null)
      {
        var promise = new TaskCompletionSource<Readiness>();

        EventHandler<Readiness> handler = (sender, r) =>
        {
          promise.TrySetResult(r);
        };

        _repository.ReadinessHandler += handler;

        Init();

        await promise.Task;

        _repository.ReadinessHandler -= handler;
      }
    }
  }
}
