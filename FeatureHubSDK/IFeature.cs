using System;
using IO.FeatureHub.SSE.Model;

// because dependent library does

namespace FeatureHubSDK
{
  public interface IFeature
  {
    /// <summary>
    /// if the value is not null, exists returns true
    /// </summary>
    public bool Exists { get; }

    public bool BooleanFeature(bool defaultValue = false);
    public string StringFeature(string defaultValue = "");
    public double NumberFeature(double defaultValue = 0.0);
    public string JsonFeature(string defaultValue = "{}");

    public Guid? Id { get; }
    public Guid? EnvironmentId { get; }

    /// <summary>
    /// Type is a bool. It will only be null if the type of Feature is not a bool.
    /// </summary>
    public bool? BooleanValue { get; }

    /// <summary>
    /// the type is a string and returned as such
    /// </summary>
    public string StringValue { get; }

    /// <summary>
    /// A numeric value. This could be an integer or a double.
    /// </summary>
    public double? NumberValue { get; }

    /// <summary>
    /// this is just the same as StringValue, no attempt to decode into JSON is done as it is easier for the end user to decode it into
    /// the format they require.
    /// </summary>
    public string JsonValue { get; }

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
