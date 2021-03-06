/*
 * FeatureServiceApi
 *
 * This describes the API clients use for accessing features
 *
 * The version of the OpenAPI document: 1.1.2
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Mime;
using IO.FeatureHub.SSE.Client;
using IO.FeatureHub.SSE.Model;
using Environment = IO.FeatureHub.SSE.Model.Environment;

namespace IO.FeatureHub.SSE.Api
{

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IFeatureServiceApiSync : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Requests all features for this sdkurl and disconnects
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <returns>List&lt;Environment&gt;</returns>
        List<Environment> GetFeatureStates(List<string> sdkUrl);

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Requests all features for this sdkurl and disconnects
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <returns>ApiResponse of List&lt;Environment&gt;</returns>
        ApiResponse<List<Environment>> GetFeatureStatesWithHttpInfo(List<string> sdkUrl);
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Updates the feature state if allowed.
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <returns>object</returns>
        object SetFeatureState(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate);

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Updates the feature state if allowed.
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <returns>ApiResponse of object</returns>
        ApiResponse<object> SetFeatureStateWithHttpInfo(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate);
        #endregion Synchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IFeatureServiceApiAsync : IApiAccessor
    {
        #region Asynchronous Operations
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Requests all features for this sdkurl and disconnects
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;Environment&gt;</returns>
        System.Threading.Tasks.Task<List<Environment>> GetFeatureStatesAsync(List<string> sdkUrl, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Requests all features for this sdkurl and disconnects
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;Environment&gt;)</returns>
        System.Threading.Tasks.Task<ApiResponse<List<Environment>>> GetFeatureStatesWithHttpInfoAsync(List<string> sdkUrl, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Updates the feature state if allowed.
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of object</returns>
        System.Threading.Tasks.Task<object> SetFeatureStateAsync(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Updates the feature state if allowed.
        /// </remarks>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (object)</returns>
        System.Threading.Tasks.Task<ApiResponse<object>> SetFeatureStateWithHttpInfoAsync(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IFeatureServiceApi : IFeatureServiceApiSync, IFeatureServiceApiAsync
    {

    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class FeatureServiceApi : IFeatureServiceApi
    {
        private IO.FeatureHub.SSE.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureServiceApi"/> class.
        /// </summary>
        /// <returns></returns>
        public FeatureServiceApi() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureServiceApi"/> class.
        /// </summary>
        /// <returns></returns>
        public FeatureServiceApi(string basePath)
        {
            this.Configuration = IO.FeatureHub.SSE.Client.Configuration.MergeConfigurations(
                IO.FeatureHub.SSE.Client.GlobalConfiguration.Instance,
                new IO.FeatureHub.SSE.Client.Configuration { BasePath = basePath }
            );
            this.Client = new IO.FeatureHub.SSE.Client.ApiClient(this.Configuration.BasePath);
            this.AsynchronousClient = new IO.FeatureHub.SSE.Client.ApiClient(this.Configuration.BasePath);
            this.ExceptionFactory = IO.FeatureHub.SSE.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureServiceApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="configuration">An instance of Configuration</param>
        /// <returns></returns>
        public FeatureServiceApi(IO.FeatureHub.SSE.Client.Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Configuration = IO.FeatureHub.SSE.Client.Configuration.MergeConfigurations(
                IO.FeatureHub.SSE.Client.GlobalConfiguration.Instance,
                configuration
            );
            this.Client = new IO.FeatureHub.SSE.Client.ApiClient(this.Configuration.BasePath);
            this.AsynchronousClient = new IO.FeatureHub.SSE.Client.ApiClient(this.Configuration.BasePath);
            ExceptionFactory = IO.FeatureHub.SSE.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureServiceApi"/> class
        /// using a Configuration object and client instance.
        /// </summary>
        /// <param name="client">The client interface for synchronous API access.</param>
        /// <param name="asyncClient">The client interface for asynchronous API access.</param>
        /// <param name="configuration">The configuration object.</param>
        public FeatureServiceApi(IO.FeatureHub.SSE.Client.ISynchronousClient client, IO.FeatureHub.SSE.Client.IAsynchronousClient asyncClient, IO.FeatureHub.SSE.Client.IReadableConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (asyncClient == null) throw new ArgumentNullException("asyncClient");
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Client = client;
            this.AsynchronousClient = asyncClient;
            this.Configuration = configuration;
            this.ExceptionFactory = IO.FeatureHub.SSE.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// The client for accessing this underlying API asynchronously.
        /// </summary>
        public IO.FeatureHub.SSE.Client.IAsynchronousClient AsynchronousClient { get; set; }

        /// <summary>
        /// The client for accessing this underlying API synchronously.
        /// </summary>
        public IO.FeatureHub.SSE.Client.ISynchronousClient Client { get; set; }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public string GetBasePath()
        {
            return this.Configuration.BasePath;
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public IO.FeatureHub.SSE.Client.IReadableConfiguration Configuration { get; set; }

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public IO.FeatureHub.SSE.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        ///  Requests all features for this sdkurl and disconnects
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <returns>List&lt;Environment&gt;</returns>
        public List<Environment> GetFeatureStates(List<string> sdkUrl)
        {
            IO.FeatureHub.SSE.Client.ApiResponse<List<Environment>> localVarResponse = GetFeatureStatesWithHttpInfo(sdkUrl);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  Requests all features for this sdkurl and disconnects
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <returns>ApiResponse of List&lt;Environment&gt;</returns>
        public IO.FeatureHub.SSE.Client.ApiResponse<List<Environment>> GetFeatureStatesWithHttpInfo(List<string> sdkUrl)
        {
            // verify the required parameter 'sdkUrl' is set
            if (sdkUrl == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'sdkUrl' when calling FeatureServiceApi->GetFeatureStates");

            IO.FeatureHub.SSE.Client.RequestOptions localVarRequestOptions = new IO.FeatureHub.SSE.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.QueryParameters.Add(IO.FeatureHub.SSE.Client.ClientUtils.ParameterToMultiMap("multi", "sdkUrl", sdkUrl));


            // make the HTTP request
            var localVarResponse = this.Client.Get<List<Environment>>("/features/", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetFeatureStates", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  Requests all features for this sdkurl and disconnects
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;Environment&gt;</returns>
        public async System.Threading.Tasks.Task<List<Environment>> GetFeatureStatesAsync(List<string> sdkUrl, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            IO.FeatureHub.SSE.Client.ApiResponse<List<Environment>> localVarResponse = await GetFeatureStatesWithHttpInfoAsync(sdkUrl, cancellationToken).ConfigureAwait(false);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  Requests all features for this sdkurl and disconnects
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">A list of API keys to retrieve information about</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;Environment&gt;)</returns>
        public async System.Threading.Tasks.Task<IO.FeatureHub.SSE.Client.ApiResponse<List<Environment>>> GetFeatureStatesWithHttpInfoAsync(List<string> sdkUrl, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            // verify the required parameter 'sdkUrl' is set
            if (sdkUrl == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'sdkUrl' when calling FeatureServiceApi->GetFeatureStates");


            IO.FeatureHub.SSE.Client.RequestOptions localVarRequestOptions = new IO.FeatureHub.SSE.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.QueryParameters.Add(IO.FeatureHub.SSE.Client.ClientUtils.ParameterToMultiMap("multi", "sdkUrl", sdkUrl));


            // make the HTTP request

            var localVarResponse = await this.AsynchronousClient.GetAsync<List<Environment>>("/features/", localVarRequestOptions, this.Configuration, cancellationToken).ConfigureAwait(false);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetFeatureStates", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  Updates the feature state if allowed.
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <returns>object</returns>
        public object SetFeatureState(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate)
        {
            IO.FeatureHub.SSE.Client.ApiResponse<object> localVarResponse = SetFeatureStateWithHttpInfo(sdkUrl, featureKey, featureStateUpdate);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  Updates the feature state if allowed.
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <returns>ApiResponse of object</returns>
        public IO.FeatureHub.SSE.Client.ApiResponse<object> SetFeatureStateWithHttpInfo(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate)
        {
            // verify the required parameter 'sdkUrl' is set
            if (sdkUrl == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'sdkUrl' when calling FeatureServiceApi->SetFeatureState");

            // verify the required parameter 'featureKey' is set
            if (featureKey == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'featureKey' when calling FeatureServiceApi->SetFeatureState");

            // verify the required parameter 'featureStateUpdate' is set
            if (featureStateUpdate == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'featureStateUpdate' when calling FeatureServiceApi->SetFeatureState");

            IO.FeatureHub.SSE.Client.RequestOptions localVarRequestOptions = new IO.FeatureHub.SSE.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("sdkUrl", IO.FeatureHub.SSE.Client.ClientUtils.ParameterToString(sdkUrl)); // path parameter
            localVarRequestOptions.PathParameters.Add("featureKey", IO.FeatureHub.SSE.Client.ClientUtils.ParameterToString(featureKey)); // path parameter
            localVarRequestOptions.Data = featureStateUpdate;


            // make the HTTP request
            var localVarResponse = this.Client.Put<object>("/features/{sdkUrl}/{featureKey}", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("SetFeatureState", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        ///  Updates the feature state if allowed.
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of object</returns>
        public async System.Threading.Tasks.Task<object> SetFeatureStateAsync(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            IO.FeatureHub.SSE.Client.ApiResponse<object> localVarResponse = await SetFeatureStateWithHttpInfoAsync(sdkUrl, featureKey, featureStateUpdate, cancellationToken).ConfigureAwait(false);
            return localVarResponse.Data;
        }

        /// <summary>
        ///  Updates the feature state if allowed.
        /// </summary>
        /// <exception cref="IO.FeatureHub.SSE.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="sdkUrl">The API Key for the environment and service account</param>
        /// <param name="featureKey">The key you wish to update/action</param>
        /// <param name="featureStateUpdate"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (object)</returns>
        public async System.Threading.Tasks.Task<IO.FeatureHub.SSE.Client.ApiResponse<object>> SetFeatureStateWithHttpInfoAsync(string sdkUrl, string featureKey, FeatureStateUpdate featureStateUpdate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            // verify the required parameter 'sdkUrl' is set
            if (sdkUrl == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'sdkUrl' when calling FeatureServiceApi->SetFeatureState");

            // verify the required parameter 'featureKey' is set
            if (featureKey == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'featureKey' when calling FeatureServiceApi->SetFeatureState");

            // verify the required parameter 'featureStateUpdate' is set
            if (featureStateUpdate == null)
                throw new IO.FeatureHub.SSE.Client.ApiException(400, "Missing required parameter 'featureStateUpdate' when calling FeatureServiceApi->SetFeatureState");


            IO.FeatureHub.SSE.Client.RequestOptions localVarRequestOptions = new IO.FeatureHub.SSE.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = IO.FeatureHub.SSE.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("sdkUrl", IO.FeatureHub.SSE.Client.ClientUtils.ParameterToString(sdkUrl)); // path parameter
            localVarRequestOptions.PathParameters.Add("featureKey", IO.FeatureHub.SSE.Client.ClientUtils.ParameterToString(featureKey)); // path parameter
            localVarRequestOptions.Data = featureStateUpdate;


            // make the HTTP request

            var localVarResponse = await this.AsynchronousClient.PutAsync<object>("/features/{sdkUrl}/{featureKey}", localVarRequestOptions, this.Configuration, cancellationToken).ConfigureAwait(false);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("SetFeatureState", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

    }
}
