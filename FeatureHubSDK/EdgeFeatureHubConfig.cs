

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeatureHubSDK
{
  public delegate IEdgeService EdgeServiceSource(IFeatureRepositoryContext repository, IFeatureHubConfig config);

  public class FeatureHubConfig
  {
    public static EdgeServiceSource defaultEdgeProvider = (repository, config) => {
      return new EventServiceListener(repository, config);
    };

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

    IClientContext NewContext(IFeatureRepositoryContext repository = null, EdgeServiceSource edgeServiceSource = null);

    // is the system ready? use this in your liveness/health check
    Readyness Readyness { get; }

    void AddAnalyticCollector(IAnalyticsCollector collector);
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

    public EdgeFeatureHubConfig(string edgeUrl, string sdkKey)
    {
      if (edgeUrl == null || sdkKey == null)
      {
        throw new FeatureHubKeyInvalidException($"The edge url or sdk key are null.");
      }
      
      _serverEvaluation = !sdkKey.Contains("*"); // two part keys are server evaluated

      if (!sdkKey.Contains("/"))
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
    }

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

    public IEdgeService EdgeService
    {
      get
      {
        if (_edgeService == null)
        {
          var pollTimeoutDefault = Environment.GetEnvironmentVariable("FEATUREHUB_POLL_TIMEOUT");
          if (pollTimeoutDefault != null)
          {
            _edgeService = new EdgeClientPoll(Repository, this,
              int.Parse(pollTimeoutDefault));
          }
          else
          {
            _edgeService = FeatureHubConfig.defaultEdgeProvider(this.Repository, this);            
          }
        }

        return _edgeService;
      }
      set
      {
        _edgeService = value;
      }
    }

    public IFeatureHubConfig UsePolling(int timeout = 360)
    {
      _edgeService = new EdgeClientPoll(Repository, this, timeout);
      return this;
    }

    private IFeatureRepositoryContext _repository;

    public IFeatureRepositoryContext Repository
    {
      get
      {
        if (_repository == null)
        {
          _repository = new FeatureHubRepository();
        }

        return _repository;
      }
      set
      {
        _repository = value;
      }
    }

    public IClientContext NewContext(IFeatureRepositoryContext repository = null, EdgeServiceSource edgeServiceSource = null)
    {
      if (repository == null)
      {
        repository = Repository;
      }

      if (edgeServiceSource == null)
      {
        if (_edgeService != null)
        {
          edgeServiceSource = (repo, config) => _edgeService;
        }
        else 
        {
          edgeServiceSource = (repo, config) => FeatureHubConfig.defaultEdgeProvider(repo, config);
        }
      }

      if (_serverEvaluation)
      {
        return new ServerEvalFeatureContext(repository, this, edgeServiceSource);
      }

      return new ClientEvalFeatureContext(repository, this, edgeServiceSource);
    }

    public Readyness Readyness => Repository.Readyness;

    public void AddAnalyticCollector(IAnalyticsCollector collector)
    {
      Repository.AddAnalyticCollector(collector);
    }

    public string Url => _url;
  }
}
