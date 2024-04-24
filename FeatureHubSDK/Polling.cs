using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IO.FeatureHub.SSE.Api;
using IO.FeatureHub.SSE.Client;
using IO.FeatureHub.SSE.Model;

/*
 * The purpose of this set of functionality is to allow the system to poll rather that use event streaming. It's
 * default behaviour is to poll after an expiry timeout if a request is made for feature values. It will not poll if
 * that does not happen.
 */
namespace FeatureHubSDK
{
    public class EdgeClientPoll : IEdgeService
    {
        private readonly IFeatureRepositoryContext _repositoryContext;
        private readonly IFeatureHubConfig _config;

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
        private DateTime _cacheTimeout;
        private string _oldHeader;

        public EdgeClientPoll(IFeatureRepositoryContext repositoryContext, IFeatureHubConfig config, int timeout = 360)
        {
            _repositoryContext = repositoryContext;
            _config = config;
            _timeoutInSeconds = timeout;

            if (FeatureLogging.InfoLogger != null)
            {
                FeatureLogging.InfoLogger(this, $"[featurehub] using polling, timeout is {timeout}s");
            }
            
            _configuration = new Configuration
            {
                BasePath = config.EdgeUrl
            };
            
            ReloadApi();
            
            // ensure we poll straight away
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
        /// <param name="api"></param>
        public void SideloadApi(IFeatureServiceApi api)
        {
            _api = api;
        }

        public async Task ContextChange(string header)
        {
            if (_stopped || _deadConnection) return;

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
            var crypt = new System.Security.Cryptography.SHA256Managed();
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
            if (_deadConnection || _stopped) return;
            var breakCache = _timeoutInSeconds == 0 || _headerChanged || (_cacheTimeout.CompareTo(DateTime.Now) < 0);

            // we can only actually ask for state if we aren't already asking, we aren't stopped => it is time to break the cache
            var ask = !_busy && !_stopped && breakCache;

            if (ask)
            {
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
                }
            }
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
            _repositoryContext.Notify(SSEResultState.Failure, null);
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
        /// <param name="cacheControlHeader"></param>
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

        public bool IsRequiresReplacementOnHeaderChange => false;
        public int TimeoutSeconds => _timeoutInSeconds;
        public string Etag => _configuration.DefaultHeaders.ContainsKey("if-none-match") ? _configuration.DefaultHeaders["if-none-match"] : null;

        public bool Stopped => _stopped;

        public bool DeadConnection => _deadConnection;

        public DateTime CacheTimeout => _cacheTimeout;

        public void Close()
        {
            _stopped = true;
        }
    }
}