using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;

// because dependent library does

namespace FeatureHubSDK
{
    public interface IFeature
    {
        /// <summary>
        /// if the value is not null, exists returns true
        /// </summary>
        bool Exists { get; }
        
        bool Boolean(bool defaultValue = false);
        string String(string defaultValue = "");
        double Number(double defaultValue = 0.0);
        string Json(string defaultValue = "{}");
        
        Guid? Id { get; }
        Guid? EnvironmentId { get; }

        /// <summary>
        /// Type is a bool. It will only be null if the type of Feature is not a bool.
        /// </summary>
        bool? BooleanValue { get; }

        /// <summary>
        /// the type is a string and returned as such
        /// </summary>
        string StringValue { get; }

        /// <summary>
        /// A numeric value. This could be an integer or a double.
        /// </summary>
        double? NumberValue { get; }

        /// <summary>
        /// this is just the same as StringValue, no attempt to decode into JSON is done as it is easier for the end user to decode it into
        /// the format they require.
        /// </summary>
        string JsonValue { get; }

        /// <summary>
        /// The KEY of this feature
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The type of this feature. Null if we start listening for a feature before it is Ready or it never exists.
        /// </summary>
        FeatureValueType? Type { get; }

        /// <summary>
        /// The raw value as it came down the wire.
        /// </summary>
        object Value { get; }
        
        /// <summary>
        /// The raw value as it came down the wire but without triggering usage.
        /// </summary>
        object UsageFreeValue { get; }

        /// <summary>
        /// The version of the current feature
        /// </summary>
        long? Version { get; }

        /// <summary>
        /// Determines if the feature is boolean and is true
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Determines if the feature has a value (not null)
        /// </summary>
        bool IsSet { get; }

        /// <summary>
        /// Determines if the feature is locked and can't be overridden or updated
        /// </summary>
        bool IsLocked { get; }

        IFeature WithContext(IClientContext context);

        /// <summary>
        /// Triggered when the value changes
        /// </summary>
        event EventHandler<IFeature> FeatureUpdateHandler;
        //IFeatureStateHolder Copy();
    }
}