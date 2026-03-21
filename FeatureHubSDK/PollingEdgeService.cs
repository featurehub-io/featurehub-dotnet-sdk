using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IO.FeatureHub.SSE.Api;
using IO.FeatureHub.SSE.Client;
using IO.FeatureHub.SSE.Model;

/*
 * The purpose of this set of functionality is to allow the system to poll rather that use event streaming.
 *
 * PassiveRest (default): polls only when a feature is evaluated and the cache has expired.
 * ActiveRest: polls immediately on first call, then schedules a recurring one-shot timer after
 *             each HTTP response so that features stay fresh regardless of evaluation activity.
 */
namespace FeatureHubSDK
{
  public class PollingEdgeService : IEdgeService
  {
    private readonly IFeatureRepositoryContext _repositoryContext;
    private readonly IFeatureHubConfig _config;
    private readonly EdgeType _edgeType;

    /// <summary>
    /// This represents the user having closed the connection or receipt of a 236 from the server
    /// </summary>
    private bool _stopped;

    /// <summary>
    ///  this represents a misconfiguration
    /// </summary>
    private bool _deadConnection;

    private int _timeoutInSeconds;
    private readonly Configuration _configuration;
    private IFeatureServiceApi _api;
    private string _contextSha = "0";
    private bool _headerChanged;
    private bool _busy;

    // PassiveRest: poll only when this timestamp has passed
    private DateTime _cacheTimeout;

    // ActiveRest: one-shot timer that triggers the next poll
    private Timer _pollTimer;
    private volatile bool _timerActive;

    private string _oldHeader;

    public PollingEdgeService(IFeatureRepositoryContext repositoryContext, IFeatureHubConfig config,
        int timeout = 360, EdgeType edgeType = EdgeType.PassiveRest)
    {
      _repositoryContext = repositoryContext;
      _config = config;
      _timeoutInSeconds = timeout;
      _edgeType = edgeType;

      if (FeatureLogging.InfoLogger != null)
      {
        FeatureLogging.InfoLogger(this,
            $"[featurehub] using {edgeType} polling, timeout is {timeout}s");
      }

      _configuration = new Configuration
      {
        BasePath = config.EdgeUrl
      };

      ReloadApi();

      // ensure we poll straight away on the first call
      _cacheTimeout = DateTime.Now.Subtract(TimeSpan.FromSeconds(1));
    }

    // every time we change the config, we have to recreate the API client as it merges the config we provide.
    private void ReloadApi()
    {
      _api = new FeatureServiceApi(_configuration);
    }

    /// <summary>
    /// testing method, do not use
    /// </summary>
    public void SideloadApi(IFeatureServiceApi api)
    {
      _api = api;
    }

    public async Task ContextChange(string header)
    {
      if (_stopped || _deadConnection)
        return;

      if (header != _oldHeader)
      {
        if (header == null)
        {
          _configuration.DefaultHeaders.Remove("x-featurehub");
          _contextSha = "0";
        }
        else
        {
          _configuration.DefaultHeaders["x-featurehub"] = header;
          _contextSha = Sha256(header);
        }

        _configuration.DefaultHeaders.Remove("if-none-match");

        ReloadApi();

        _oldHeader = header;
        _headerChanged = true;

        await Poll();
      }
    }

    public static string Sha256(string shaString)
    {
      var crypt = new SHA256Managed();
      var hash = new StringBuilder();
      byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(shaString));
      foreach (byte theByte in crypto)
      {
        hash.Append(theByte.ToString("x2"));
      }

      return hash.ToString();
    }

    public async Task Poll()
    {
      if (_deadConnection || _stopped)
        return;

      bool shouldPoll;
      if (_edgeType == EdgeType.ActiveRest)
      {
        // While the timer is ticking, suppress external Poll() calls so we don't
        // double-fetch. Always allow a poll when the context header has changed.
        shouldPoll = !_timerActive || _headerChanged;
      }
      else
      {
        // PassiveRest: honour the cache expiry window
        shouldPoll = _timeoutInSeconds == 0 || _headerChanged ||
                     (_cacheTimeout.CompareTo(DateTime.Now) < 0);
      }

      var ask = !_busy && !_stopped && shouldPoll;

      if (ask)
      {
        // Cancel any pending timer so it cannot fire concurrently with the HTTP call.
        // StartActiveTimer() will arm a fresh one once the call completes.
        _pollTimer?.Dispose();
        _pollTimer = null;
        _timerActive = false;

        try
        {
          if (FeatureLogging.TraceLogger != null)
          {
            var keys = String.Join(",", _config.SdkKeys.ToArray());
            FeatureLogging.TraceLogger(this,
                $"featurehub: polling for {_configuration.BasePath} with keys {keys}");
          }

          _busy = true;
          _headerChanged = false;
          var result = await _api.GetFeatureStatesWithHttpInfoAsync(_config.SdkKeys, _contextSha);

          DecodeResponse(result);
        }
        catch (ApiException ae)
        {
          if (ae.ErrorCode == 400 || ae.ErrorCode == 404 || ae.ErrorCode == 403)
          {
            ApiKeyInvalid();
          }
          else
          {
            RefreshCacheTimeout();
          }
        }
        finally
        {
          _busy = false;

          // For ActiveRest, schedule the next poll via a one-shot timer so we keep
          // fetching even when no feature is being evaluated.
          if (_edgeType == EdgeType.ActiveRest && !_deadConnection && !_stopped)
          {
            StartActiveTimer();
          }
        }
      }
    }

    /// <summary>
    /// Starts (or restarts) the one-shot timer used in ActiveRest mode.
    /// When the timer fires it clears itself and triggers the next Poll().
    /// </summary>
    private void StartActiveTimer()
    {
      _pollTimer?.Dispose();
      _timerActive = true;
      _pollTimer = new Timer(state =>
      {
        _timerActive = false;
        // Fire-and-forget: the timer callback is synchronous but Poll is async.
        _ = Poll();
      }, null, _timeoutInSeconds * 1000, Timeout.Infinite);
    }

    public void DecodeResponse(ApiResponse<List<FeatureEnvironmentCollection>> response)
    {
      var statusCodeAsInt = (int)response.StatusCode;
      switch (statusCodeAsInt)
      {
        case 200:
        case 236:
          {
            CheckForCacheControl(response);
            CheckForEtag(response);

            UpdateRepository(response.Data);

            if (statusCodeAsInt == 236)
            {
              FeatureLogging.InfoLogger(this,
                  "featurehub: this environment has gone stale and will not receive any further updates");
              _stopped = true;
            }

            break;
          }
        case 304:
          {
            if (FeatureLogging.TraceLogger != null)
            {
              FeatureLogging.TraceLogger(this, "[featurehub] received 304, no state updates");
            }

            break;
          }
        case 400:
        case 403:
        case 404:
          ApiKeyInvalid();
          break;
        default:
          FeatureLogging.WarnLogger(this, $"featurehub: unexpected result from server: {statusCodeAsInt}");
          break;
      }

      RefreshCacheTimeout();
    }

    public void CheckForEtag(ApiResponse<List<FeatureEnvironmentCollection>> response)
    {
      if (response.Headers.TryGetValue("ETag", out var etag))
      {
        FeatureLogging.TraceLogger(this, $"[featurehub] using etag to cache is {etag.First()}");
        _configuration.DefaultHeaders["if-none-match"] = etag.First();
        ReloadApi();
      }
    }

    private void ApiKeyInvalid()
    {
      FeatureLogging.ErrorLogger(this, "featurehub: there is a problem with the API key or configuration");
      _repositoryContext.Notify(SSEResultState.Failure, null, _config.EnvironmentId);
      _deadConnection = true;
    }

    private void RefreshCacheTimeout()
    {
      if (_timeoutInSeconds > 0)
      {
        _cacheTimeout = DateTime.Now.Add(TimeSpan.FromSeconds(_timeoutInSeconds));
      }
    }

    public void CheckForCacheControl(ApiResponse<List<FeatureEnvironmentCollection>> response)
    {
      if (response.Headers.TryGetValue("Cache-Control", out var header))
      {
        DecodeCacheControl(header);
      }
    }

    /// <summary>
    /// This allows the server to override the polling interval
    /// </summary>
    public void DecodeCacheControl(IList<string> cacheControlHeader)
    {
      var reg = new Regex("max-age=(\\d+)", RegexOptions.IgnoreCase);
      foreach (var header in cacheControlHeader)
      {
        var match = reg.Match(header);
        if (match.Success && match.Groups.Count > 0)
        {
          try
          {
            var cacheAge = int.Parse(match.Groups[0].Value.Substring(8));
            if (cacheAge > 0)
            {
              if (FeatureLogging.InfoLogger != null)
                FeatureLogging.InfoLogger(this, $"Server requested cache age to change to {cacheAge}s");

              _timeoutInSeconds = cacheAge;
            }
          }
          catch (Exception)
          {
            // do nothing
          }
        }
      }
    }

    private void UpdateRepository(List<FeatureEnvironmentCollection> envs)
    {
      List<FeatureState> states = new List<FeatureState>();

      envs.ForEach(e =>
      {
        e.Features.ForEach(f =>
              {
            f.EnvironmentId = e.Id;
            states.Add(f);
          });
      });

      _repositoryContext.UpdateFeatures(states);
    }

    public bool ClientEvaluation => !_config.ServerEvaluation;

    public int TimeoutSeconds => _timeoutInSeconds;

    public string Etag => _configuration.DefaultHeaders.ContainsKey("if-none-match")
        ? _configuration.DefaultHeaders["if-none-match"]
        : null;

    public bool Stopped => _stopped;

    public bool DeadConnection => _deadConnection;

    public DateTime CacheTimeout => _cacheTimeout;

    /// <summary>
    /// True while the ActiveRest timer is scheduled and has not yet fired.
    /// </summary>
    public bool TimerActive => _timerActive;

    public void Close()
    {
      _stopped = true;
      _pollTimer?.Dispose();
      _pollTimer = null;
      _timerActive = false;
    }
  }
}
