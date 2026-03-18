#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IO.FeatureHub.SSE.Model;

namespace FeatureHubSDK
{
  /// <summary>
  /// Represents a subscription to a repository event stream. Call Cancel() to unsubscribe.
  /// Equivalent to Java's RepositoryEventHandler.
  /// </summary>
  public class RepositoryEventHandler
  {
    private readonly Action _cancel;

    internal RepositoryEventHandler(Action cancel)
    {
      _cancel = cancel;
    }

    public void Cancel() => _cancel.Invoke();
  }
  
  /// <summary>
  /// The serialised snapshot of a single feature at the moment it was evaluated.
  /// Equivalent to Java's FeatureHubUsageValue.
  /// </summary>
  public class FeatureHubUsageValue
  {
    public readonly Guid Id;
    public readonly string Key;
    /// <summary>
    /// The feature value serialised to a string. Boolean → "on"/"off", Number → toString,
    /// String → as-is, JSON → null.
    /// </summary>
    public readonly string? Value;
    public readonly object? RawValue;
    public readonly FeatureValueType Type;
    public readonly Guid EnvironmentId;

    

    public FeatureHubUsageValue(FeatureState fs, object? value)
    {
      Id = fs.Id;
      Key = fs.Key;
      RawValue = value;
      Value = DefaultUsageProvider.Convert(value, fs.Type);
      EnvironmentId = fs.EnvironmentId;
      Type = fs.Type ?? throw new InvalidOperationException($"Feature type must not be null for key '{fs.Key}'");
    }

    public FeatureHubUsageValue(IFeature fs, object? value)
    {
      Id = fs.Id  ?? throw new InvalidOperationException($"Feature ID must not be null for key '{fs.Key}'");
      Key = fs.Key;
      RawValue = value;
      Value = DefaultUsageProvider.Convert(value, fs.Type);
      EnvironmentId = fs.EnvironmentId  ?? throw new InvalidOperationException($"Feature EnvironmentId must not be null for key '{fs.Key}'");
      Type = fs.Type ?? throw new InvalidOperationException($"Feature type must not be null for key '{fs.Key}'");
    }
  }

  /// <summary>
  /// Base interface for all usage events passed to plugins.
  /// Equivalent to Java's UsageEvent.
  /// </summary>
  public interface IUsageEvent
  {
    string? UserKey { get; set; }
    void SetAdditionalParams(Dictionary<string, object>? additionalParams);
    
    IReadOnlyDictionary<string, object?> CopyBaseMap();
  }

  /// <summary>
  /// Mixin that tags a usage event with a string event type name.
  /// Equivalent to Java's UsageEventName.
  /// </summary>
  public interface IUsageEventName
  {
    string EventName { get; }
  }

  /// <summary>
  /// A single feature evaluation event. EventName is always "feature".
  /// Equivalent to Java's UsageEventWithFeature.
  /// </summary>
  public interface IUsageEventWithFeature : IUsageEvent, IUsageEventName
  {
    /// <summary>Context attributes from the evaluation context (may be null for no-context reads).</summary>
    Dictionary<string, List<string>>? Attributes { get; }
    FeatureHubUsageValue Feature { get; }
  }

  /// <summary>
  /// A batch of feature values (e.g. fired on readiness).
  /// Equivalent to Java's UsageFeaturesCollection.
  /// </summary>
  public interface IUsageFeaturesCollection : IUsageEvent
  {
    void SetFeatureValues(List<FeatureHubUsageValue> featureValues);
  }

  /// <summary>
  /// A batch of feature values paired with context attributes (e.g. fired on server-eval update).
  /// Equivalent to Java's UsageFeaturesCollectionContext.
  /// </summary>
  public interface IUsageFeaturesCollectionContext : IUsageFeaturesCollection
  {
    void SetAttributes(Dictionary<string, List<string>> attributes);
  }

  /// <summary>
  /// Base implementation of IUsageEvent. Holds an optional userKey and an arbitrary
  /// additional-params map that subclasses merge into ToMap().
  /// Equivalent to Java's DefaultUsageEvent.
  /// </summary>
  public class DefaultUsageEvent : IUsageEvent
  {
    private Dictionary<string, object> _additionalParams = new Dictionary<string, object>();

    public DefaultUsageEvent() { }

    public DefaultUsageEvent(string userKey)
    {
      UserKey = userKey;
    }

    public DefaultUsageEvent(string userKey, Dictionary<string, object>? additionalParams)
    {
      UserKey = userKey;
      if (additionalParams != null)
        _additionalParams = additionalParams;
    }

    public string? UserKey { get; set; }

    public void SetAdditionalParams(Dictionary<string, object>? additionalParams)
      => _additionalParams = additionalParams ?? new Dictionary<string, object>();

    public virtual IReadOnlyDictionary<string, object> ToMap() => _additionalParams;

    /// <summary>Returns a mutable copy of the base additional-params map for subclass use.</summary>
    public IReadOnlyDictionary<string, object?> CopyBaseMap()
      => new ReadOnlyDictionary<string, object?>(_additionalParams!);
  }

  /// <summary>
  /// Single-feature evaluation event. ToMap() merges context attributes then feature fields
  /// (feature fields win). EventName is always "feature".
  /// Equivalent to Java's DefaultUsageEventWithFeature.
  /// </summary>
  public class DefaultUsageEventWithFeature : DefaultUsageEvent, IUsageEventWithFeature
  {
    public Dictionary<string, List<string>>? Attributes { get; }
    public FeatureHubUsageValue Feature { get; }
    public string EventName => "feature";

    public DefaultUsageEventWithFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>>? attributes, string? userKey)
    {
      Feature = feature;
      Attributes = attributes;
      UserKey = userKey;
    }

    public new IReadOnlyDictionary<string, object?> CopyBaseMap()
    {
      var m = base.CopyBaseMap().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      
      if (Attributes != null)
      {
        foreach (var kvp in Attributes)
          m[kvp.Key] = kvp.Value;
      }
      m["feature"] = Feature.Key;
      m["value"] = Feature.Value;
      m["id"] = Feature.Id;
      return new ReadOnlyDictionary<string, object?>(m);
    }
  }

  /// <summary>
  /// A batch of feature values. ToMap() adds each feature as key → serialised value.
  /// Equivalent to Java's DefaultUsageFeaturesCollection.
  /// </summary>
  public class DefaultUsageFeaturesCollection : DefaultUsageEvent, IUsageFeaturesCollection
  {
    protected List<FeatureHubUsageValue> FeatureValues = new List<FeatureHubUsageValue>();

    public DefaultUsageFeaturesCollection() { }

    public DefaultUsageFeaturesCollection(string userKey, Dictionary<string, object>? additionalParams)
      : base(userKey, additionalParams) { }

    public void SetFeatureValues(List<FeatureHubUsageValue> featureValues)
      => FeatureValues = featureValues;

    public new IReadOnlyDictionary<string, object?> CopyBaseMap()
    {
      var m = base.CopyBaseMap().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      foreach (var fv in FeatureValues)
        m[fv.Key] = fv.Value;
      return new ReadOnlyDictionary<string, object?>(m);
    }
  }

  /// <summary>
  /// A batch of feature values with context attributes. ToMap() merges features then
  /// context attributes on top (attributes win over feature keys of the same name).
  /// Equivalent to Java's DefaultUsageFeaturesCollectionContext.
  /// </summary>
  public class DefaultUsageFeaturesCollectionContext : DefaultUsageFeaturesCollection,
    IUsageFeaturesCollectionContext
  {
    private Dictionary<string, List<string>> _attributes = new();

    public DefaultUsageFeaturesCollectionContext() { }

    public DefaultUsageFeaturesCollectionContext(string userKey, Dictionary<string, object>? additionalParams)
      : base(userKey, additionalParams) { }

    public void SetAttributes(Dictionary<string, List<string>> attributes)
      => _attributes = attributes;

    public new IReadOnlyDictionary<string, object?> CopyBaseMap()
    {
      var m = base.CopyBaseMap().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      foreach (var kvp in _attributes)
        m[kvp.Key] = kvp.Value;
      return m;
    }
  }

  /// <summary>
  /// Analytics plugin base class. Subclass and implement Send() to receive usage events.
  /// Equivalent to Java's UsagePlugin.
  /// </summary>
  public abstract class UsagePlugin
  {
    protected readonly Dictionary<string, object> DefaultEventParams =
      new Dictionary<string, object>();

    public Dictionary<string, object> GetDefaultEventParams() => DefaultEventParams;

    public abstract void Send(IUsageEvent usageEvent);
  }

  /// <summary>
  /// Factory for constructing usage event objects. Implement and set on the repository
  /// to customise the event hierarchy.
  /// Equivalent to Java's UsageProvider.
  /// </summary>
  public interface IUsageProvider
  {
    IUsageEventWithFeature CreateUsageFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>> attributes);

    IUsageEventWithFeature CreateUsageFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>> attributes, string userKey);

    IUsageFeaturesCollection CreateUsageCollectionEvent();

    IUsageFeaturesCollectionContext CreateUsageContextCollectionEvent();

    IUsageEvent CreateUsageEvent();

    IUsageEvent CreateUsageEvent(string userKey);

    IUsageEvent CreateUsageEvent(string userKey, Dictionary<string, object>? additionalParams);

    IUsageEventWithFeature CreateUsageEventWithFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>>? attributes, string? userKey);
  }
  

  public class DefaultUsageProvider
  {
    // replace this value if you wish to globally replace the default usage provider
    public static IUsageProvider Instance = new BaseUsageProvider();
    
    // this allows you to replace the conversion method for outgoing feature values
    public static Func<object?, FeatureValueType?, string?> Convert = DefaultConvert;
    
    public static string? DefaultConvert(object? value, FeatureValueType? type)
    {
      if (type == null || value == null) return null;
      switch (type)
      {
        case FeatureValueType.BOOLEAN:
          return true.Equals(value) ? "on" : "off";
        case FeatureValueType.STRING:
        case FeatureValueType.NUMBER:
          return value.ToString();
        default:
          return null; // JSON → null
      }
    }
  }
  
  /// <summary>
  /// Default implementation of IUsageProvider — constructs the standard event types.
  /// Equivalent to Java's UsageProvider.DefaultUsageProvider.
  /// </summary>
  public class BaseUsageProvider : IUsageProvider
  {
    public IUsageEventWithFeature CreateUsageFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>> attributes)
      => new DefaultUsageEventWithFeature(feature, attributes, null);

    public IUsageEventWithFeature CreateUsageFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>> attributes, string userKey)
      => new DefaultUsageEventWithFeature(feature, attributes, userKey);

    public IUsageFeaturesCollection CreateUsageCollectionEvent()
      => new DefaultUsageFeaturesCollection();

    public IUsageFeaturesCollectionContext CreateUsageContextCollectionEvent()
      => new DefaultUsageFeaturesCollectionContext();

    public IUsageEvent CreateUsageEvent()
      => new DefaultUsageEvent();

    public IUsageEvent CreateUsageEvent(string userKey)
      => new DefaultUsageEvent(userKey);

    public IUsageEvent CreateUsageEvent(string userKey, Dictionary<string, object>? additionalParams)
      => new DefaultUsageEvent(userKey, additionalParams);

    public IUsageEventWithFeature CreateUsageEventWithFeature(FeatureHubUsageValue feature,
      Dictionary<string, List<string>>? attributes, string? userKey)
      => new DefaultUsageEventWithFeature(feature, attributes, userKey);
  }

  /// <summary>
  /// Wires the repository's usage event stream to a list of registered plugins.
  /// Create one per EdgeFeatureHubConfig; call Close() when tearing down.
  /// Equivalent to Java's UsageAdapter.
  /// </summary>
  public class UsageAdapter
  {
    private readonly List<UsagePlugin> _plugins = new List<UsagePlugin>();
    private readonly RepositoryEventHandler _usageHandlerSub;

    public UsageAdapter(IFeatureHubRepository repository)
    {
      _usageHandlerSub = repository.RegisterUsageStream(Process);
    }

    public void Close() => _usageHandlerSub.Cancel();

    public void Process(IUsageEvent usageEvent)
    {
      foreach (var plugin in _plugins)
      {
        try
        {
          plugin.Send(usageEvent);
        }
        catch (Exception e)
        {
          FeatureLogging.ExceptionLogger(this,
            new ExceptionEvent("Usage plugin failed to process event", e));
        }
      }
    }

    public void RegisterPlugin(UsagePlugin plugin) => _plugins.Add(plugin);
  }
}
