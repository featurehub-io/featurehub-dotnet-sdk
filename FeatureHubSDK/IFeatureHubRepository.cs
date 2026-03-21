#nullable enable
using System;
using System.Collections.Generic;
using IO.FeatureHub.SSE.Model;

// because dependent library does

namespace FeatureHubSDK
{
    public interface IFeatureHubRepository
    {
        IFeature GetFeature(string key);

        IFeature this[string name] { get; }
        bool IsEnabled(string name);

        event EventHandler<Readiness> ReadinessHandler;
        event EventHandler<IFeatureHubRepository> NewFeatureHandler;
        Readiness Readyness { get; }
        Readiness Readiness { get; }
        bool Exists(string key);

        /// <summary>
        /// Register a listener to receive usage events as they are recorded.
        /// Returns a RepositoryEventHandler whose Cancel() method removes the subscription.
        /// </summary>
        RepositoryEventHandler RegisterUsageStream(Action<IUsageEvent> listener);

        /// <summary>
        /// Replace the factory used to construct usage event objects for this repository.
        /// </summary>
        void RegisterUsageProvider(IUsageProvider provider);

        /// <summary>
        /// The current usage event factory. Defaults to DefaultUsageProvider.
        /// </summary>
        IUsageProvider UsageProvider { get; }

        /// <summary>
        /// Allows the recording of a usage event, which gets sent to all usage plugins.
        /// </summary>
        void RecordUsageEvent(IUsageEvent usageEvent);
    }
    
    public interface IFeatureRepositoryContext: IFeatureHubRepository
    {
        bool ServerSideEvaluation { set; get; }
        void Notify(SSEResultState state, string? data, Guid EnvironmentId);
        void NotReady();
        void UpdateFeatures(IEnumerable<FeatureState> states);
        void AddFeatureValueInterceptor(IFeatureValueInterceptor interceptor);

        (bool, object?) FindIntercept(string key, FeatureState? featureState);

        void Used(FeatureState featureState, object? value);
        List<String> AllKeys();
    }
}