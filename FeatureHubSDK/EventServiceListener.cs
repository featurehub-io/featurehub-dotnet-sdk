using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.FeatureHub.SSE.Model;
using LaunchDarkly.EventSource;
using LaunchDarkly.Logging;
using Newtonsoft.Json;

namespace FeatureHubSDK
{
  public interface IEdgeService
  {
    Task ContextChange(string header);
    bool ClientEvaluation { get; }

    bool IsRequiresReplacementOnHeaderChange { get;  }
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

  public class EventServiceListener : IEdgeService
  {
    private EventSource _eventSource;
    private readonly IFeatureHubConfig _featureHost;
    private readonly IFeatureRepositoryContext _repository;
    private string _xFeatureHubHeader;
    private bool _closed;

    public EventServiceListener(IFeatureRepositoryContext repository, IFeatureHubConfig config)
    {
      _repository = repository;
      _featureHost = config;

      // tell the repository about how evaluation works
      // this means features don't need to know about the IEdgeService
      _repository.ServerSideEvaluation = config.ServerEvaluation;
    }

    public async Task ContextChange(string newHeader)
    {
      if (_closed) return;
      
      if (_featureHost.ServerEvaluation)
      {
        if (newHeader != _xFeatureHubHeader)
        {
          _xFeatureHubHeader = newHeader;

          if (_eventSource == null || _eventSource.ReadyState == ReadyState.Open || _eventSource.ReadyState == ReadyState.Connecting)
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
    
    
    

    public bool ClientEvaluation => !_featureHost.ServerEvaluation;

    // "close" works on this events source and doesn't hang
    public bool IsRequiresReplacementOnHeaderChange => false;

    private Dictionary<string, string> BuildContextHeader()
    {
      var headers = new Dictionary<string, string>();

      if (_featureHost.ServerEvaluation && _xFeatureHubHeader != null)
      {
        headers.Add("x-featurehub", _xFeatureHubHeader);
      }

      return headers;
    }
    
    private string DefaultEnvConfig(string envVar, string defaultValue)
    {
      return Environment.GetEnvironmentVariable(envVar) ?? defaultValue;
    }

    public void Init()
    {
      if (_closed) return;

      var config = Configuration.Builder(uri: new UriBuilder(_featureHost.Url).Uri)
        .BackoffResetThreshold(
          TimeSpan.FromMinutes(int.Parse(DefaultEnvConfig("FEATUREHUB_BACKOFF_RESET_THRESHOLD", "1"))))
        .RequestHeaders(_featureHost.ServerEvaluation ? BuildContextHeader() : null)
        .InitialRetryDelay(TimeSpan.FromMilliseconds(int.Parse(DefaultEnvConfig("FEATUREHUB_DELAY_RETRY_MS", "10000"))))
        .Build();
        

      if (FeatureLogging.InfoLogger != null)
      {
        FeatureLogging.InfoLogger(this, $"Opening connection to ${_featureHost.Url}");
      }

      _eventSource = new EventSource(config);
      _eventSource.Error += (sender, ex) =>
      {
        if (!(ex.Exception is EventSourceServiceUnsuccessfulResponseException result)) return;
        if (result.StatusCode == 503) return;
        
        _repository.Notify(SSEResultState.Failure, null);
        FeatureLogging.ErrorLogger(this, "Server issued a failure, stopping.");
        _closed = true;
        _eventSource.Close();
      };
      
      _eventSource.MessageReceived += (sender, args) =>
      {
        SSEResultState? state;
        FeatureLogging.TraceLogger(this,$"received ${args.EventName} : ${args.Message.Data}");
        switch (args.EventName)
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

            if (args.Message.Data != null)
            {
              var configData = JsonConvert.DeserializeObject<ConfigData>(args.Message.Data);
              if (configData.Stale)
              {
                if (FeatureLogging.ErrorLogger != null)
                {
                  FeatureLogging.ErrorLogger(this,
                    "featurehub: environment has gone stale, closing connection and won't reopen");
                }

                _closed = true;
                _eventSource.Close();                
              }
            }
            break;
          default:
            state = null;
            break;
        }

        if (FeatureLogging.TraceLogger != null)
          FeatureLogging.TraceLogger(this, $"featurehub: The state was {state} with value {args.Message.Data}");

        if (state == null) return;

        if (state != SSEResultState.Config)
        {
          _repository.Notify(state.Value, args.Message.Data);
        }

        if (state == SSEResultState.Failure)
        {
          if (FeatureLogging.ErrorLogger != null)
          {
            FeatureLogging.ErrorLogger(this, "featurehub: received a failure so closing and not restarting");
          }

          _eventSource.Close();
        }
      };

      _eventSource.StartAsync();
    }

    public void Close()
    {
      _eventSource.Close();
    }

    public async Task Poll()
    {
      if (_eventSource == null)
      {
        var promise = new TaskCompletionSource<Readyness>();

        EventHandler<Readyness> handler = (sender, r) =>
        {
          promise.TrySetResult(r);
        };

        _repository.ReadynessHandler += handler;

        Init();

        await promise.Task;

        _repository.ReadynessHandler -= handler;
      }
    }
  }
}
