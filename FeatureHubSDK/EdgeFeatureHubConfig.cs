

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeatureHubSDK
{
  public delegate IEdgeService EdgeServiceSource(IFeatureRepositoryContext repository, IFeatureHubConfig config);

  public enum EdgeType
  {
    Streaming, ActiveRest, PassiveRest
  }

  public class FeatureHubConfig
  {
  }

  public interface IFeatureHubConfig
  {

    /// <summary>
    /// This is the fully constructed EventSource url
    /// </summary>
    string Url { get; }
    /// <summary>
    ///  this is the URL of the GET edit service
    /// </summary>
    string EdgeUrl { get;  }
    List<string> SdkKeys { get;  }
    
    bool ServerEvaluation { get; }

    /// <summary>
    /// Tells the client to use Polling. Can also be assumed if FEATUREHUB_POLL_TIMEOUT env var is set
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    IFeatureHubConfig UsePolling(int timeout = 360);
    
    /*
     * Initialise the configuration. This will kick off the event source to connect and attempt to start
     * pushing data into the FeatureHub repository for use in contexts.
     */
    Task Init();

    IFeatureRepositoryContext Repository { get; set; }
    IEdgeService EdgeService { get; set; }
    int Timeout { get; }
    
    Guid EnvironmentId { get; }

    IClientContext NewContext();

    // is the system ready? use this in your liveness/health check
    Readiness Readiness { get; }

    void AddFeatureValueInterceptor(IFeatureValueInterceptor interceptor);
  }
  
  public class FeatureHubKeyInvalidException : Exception
  {
    public FeatureHubKeyInvalidException(string message)
      : base(message)
    {
    }

    public FeatureHubKeyInvalidException(string message, Exception innerException)
      : base(message, innerException)
    {
    }
  }

  public class EdgeFeatureHubConfig : IFeatureHubConfig
  {
    private readonly string _url;
    private readonly bool _serverEvaluation;
    private readonly string _edgeUrl;
    private readonly List<string> _sdkKeys = new List<string>();
    private EdgeType _edgeType = EdgeType.Streaming;
    private readonly Guid _environmentId;
    private int _timeout;

    public EdgeFeatureHubConfig(string edgeUrl, string sdkKey)
    {
      if (edgeUrl == null || sdkKey == null)
      {
        throw new FeatureHubKeyInvalidException($"The edge url or sdk key are null.");
      }
      
      _serverEvaluation = !sdkKey.Contains("*"); // two part keys are server evaluated

      if (!sdkKey.Contains("/") || sdkKey.StartsWith("\""))
      {
        throw new FeatureHubKeyInvalidException($"The SDK key `{sdkKey}` is invalid");
      }
      
      _sdkKeys.Add(sdkKey);

      if (edgeUrl.EndsWith("/"))
      {
        edgeUrl = edgeUrl.Substring(0, edgeUrl.Length - 1);
      }

      if (edgeUrl.EndsWith("/features"))
      {
        edgeUrl = edgeUrl.Substring(0, edgeUrl.Length - "/features".Length);
      }

      _edgeUrl = edgeUrl; // the API client automatically adds the /features, etc on

      _url = edgeUrl + "/features/" + sdkKey;
      
      // extract the environment id from the sdk key
      string []parts = sdkKey.Split('/');
      _environmentId = parts.Length > 2 ? Guid.Parse(parts[1]) : Guid.Parse(parts[0]);
      
      DetermineEdgeType();
    }

    private void DetermineEdgeType()
    {
      var pollTimeout = Environment.GetEnvironmentVariable("FEATUREHUB_POLL_TIMEOUT");
      if (pollTimeout != null)
      {
        _edgeType = Environment.GetEnvironmentVariable("FEATUREHUB_POLLING_PASSIVE") != null ? EdgeType.PassiveRest : EdgeType.ActiveRest;

        _timeout = int.Parse(pollTimeout);
      }
      else
      {
        _edgeType = EdgeType.Streaming;
      }
    }
    
    public int Timeout => _timeout;
    
    public Guid EnvironmentId => _environmentId;

    /// <summary>
    /// Use this constructor if you set the environment variables.
    /// </summary>
    public EdgeFeatureHubConfig() : this(Environment.GetEnvironmentVariable("FEATUREHUB_EDGE_URL"),
      Environment.GetEnvironmentVariable("FEATUREHUB_API_KEY"))
    {
      
    } 

    public string EdgeUrl => _edgeUrl;
    public List<string> SdkKeys => _sdkKeys;

    public async Task Init()
    {
      await EdgeService.Poll();
    }

    public bool ServerEvaluation => _serverEvaluation;

    private IEdgeService _edgeService;

    private void CheckEdgeService()
    {
      CheckRepository();
      
      if (_edgeService == null)
      {
        switch (_edgeType)
        {
          case EdgeType.ActiveRest:
          case EdgeType.PassiveRest:
            FeatureLogging.TraceLogger(this, $"using a poll timeout of $_timeout");
            _edgeService = new PollingEdgeService(Repository, this,
              _timeout);
            break;
          case EdgeType.Streaming:
            FeatureLogging.TraceLogger(this, $"connecting via SSE");
            _edgeService = new StreamingEdgeService(Repository, this);
            break;
        }
      }
    }

    public IEdgeService EdgeService
    {
      get
      {
        CheckEdgeService();
        
        return _edgeService;
      }
      set => _edgeService = value;
    }

    
    public IFeatureHubConfig UsePolling(int timeout = 360)
    {
      _edgeType = EdgeType.PassiveRest;
      _timeout = timeout;
      return this;
    }

    private IFeatureRepositoryContext _repository;

    private void CheckRepository()
    {
      if (_repository == null)
      {
        _repository = new FeatureHubRepository();
        _repository.RegisterUsageStream((@event =>
        {
          // a feature evaluation came in and we are using passive rest, so tell the poller in case it needs
          // to break its cache and perform a new poll
          if (@event is IUsageEventWithFeature && _edgeType == EdgeType.PassiveRest && _edgeService != null)
          {
            _edgeService.Poll();
          }
        }));
      }
    }
    
    public IFeatureRepositoryContext Repository
    {
      get
      {
        CheckRepository();
        
        return _repository;
      }
      set => _repository = value;
    }

    public IClientContext NewContext()
    {
      CheckEdgeService();

      // kick off if it hasn't already
      _edgeService.Poll();

      if (_serverEvaluation)
      {
        return new ServerEvalFeatureContext(_repository, this, _edgeService);
      }

      return new ClientEvalFeatureContext(_repository, this);
    }


    public Readiness Readyness => Repository.Readiness;
    public Readiness Readiness => Repository.Readiness;
    
    
    public string Url => _url;
    
    public void AddFeatureValueInterceptor(IFeatureValueInterceptor interceptor)
    {
      CheckRepository();
      
      _repository.AddFeatureValueInterceptor(interceptor);
    }
  }
  
}
