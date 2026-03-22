#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using IO.FeatureHub.SSE.Model;

namespace FeatureHubSDK
{
  public interface IClientContext
  {
    IClientContext UserKey(string key);
    IClientContext SessionKey(string key);
    IClientContext Device(StrategyAttributeDeviceName device);
    IClientContext Platform(StrategyAttributePlatformName platform);
    IClientContext Country(StrategyAttributeCountryName country);

    /// expects semantic version Maj.Minor.Patch
    IClientContext Version(string version);

    IClientContext Attr(string key, string value);
    IClientContext Attrs(string key, List<string> values);
    IClientContext Clear();

    string GetAttr(string key, string defaultValue);

    List<string> GetAttrs(string key);

    string? DefaultPercentageKey { get; }

    IFeature this[string name] { get; }

    /// <summary>
    /// Is this feature enabled - it has to be a boolean and true. The same as context[name].IsEnabled
    /// </summary>
    /// <param name="name">the name of the feature.</param>
    /// <returns></returns>
    bool IsEnabled(string name);

    /// <summary>
    /// Does this feature have a value? The same as context[name].IsSet
    /// </summary>
    /// <param name="name">feature name</param>
    /// <returns></returns>
    bool IsSet(string name);

    Task<IClientContext> Build();

    IFeatureHubRepository Repository { get; }

    void Close();

    void Used(FeatureState featureState, object? value);

    void RecordUsageEvent(IUsageEvent usageEvent);
  }

  public abstract class BaseClientContext(IFeatureRepositoryContext repository, IFeatureHubConfig config)
      : IClientContext
  {
#pragma warning disable CA1051
    protected readonly Dictionary<string, List<string>> _attributes = new Dictionary<string, List<string>>();
    protected readonly IFeatureRepositoryContext _repository = repository;
    protected readonly IFeatureHubConfig _config = config;
#pragma warning restore CA1051

    public IFeatureHubRepository Repository => _repository;

    public abstract void Close();

    private Dictionary<string, List<string>> UsageAttributes()
    {
      Dictionary<string, List<string>> attributes = new(_attributes);
      attributes.Remove("userkey");
      return attributes;
    }

    public void RecordUsageEvent(IUsageEvent usageEvent)
    {
      _repository.RecordUsageEvent(FillUsage(usageEvent));
    }

    public void Used(FeatureState featureState, object? value)
    {
      _repository.RecordUsageEvent(FillUsage(
          _repository.UsageProvider.CreateUsageEventWithFeature(
              new FeatureHubUsageValue(featureState, value),
              UsageAttributes(), null)));
    }

    private String? UsageUserKey()
    {
      if (_attributes.TryGetValue("userkey", out var userKey))
      {
        return userKey[0];
      }

      if (_attributes.TryGetValue("session", out var session))
      {
        return session[0];
      }

      return null;
    }

    private IUsageEvent FillUsage(IUsageEvent usageEvent)
    {
      var userKey = UsageUserKey();
      if (userKey != null)
      {
        usageEvent.UserKey = userKey;
      }

      if (usageEvent is IUsageFeaturesCollection collection)
      {
        collection.SetFeatureValues(
            _repository.AllKeys().Select(k =>
            {
              var feat = _repository.GetFeature(k);

              return new FeatureHubUsageValue(feat, feat.WithContext(this).UsageFreeValue);
            }).ToList());
      }

      if (usageEvent is IUsageFeaturesCollectionContext contextCollection)
      {
        contextCollection.SetAttributes(UsageAttributes());
      }

      return usageEvent;
    }


    public IClientContext UserKey(string key)
    {
      _attributes["userkey"] = [key];
      return this;
    }

    private static string GetEnumMemberValue(Enum enumValue)
    {
      var type = enumValue.GetType();
      var info = type.GetField(enumValue.ToString());
      var da = (EnumMemberAttribute[])(info.GetCustomAttributes(typeof(EnumMemberAttribute), false));

      return da.Length > 0 ? da[0].Value : string.Empty;
    }

    public IClientContext SessionKey(string key)
    {
      _attributes["session"] = [key];
      return this;
    }

    public IClientContext Device(StrategyAttributeDeviceName device)
    {
      _attributes["device"] = [GetEnumMemberValue(device)];
      return this;
    }

    public IClientContext Platform(StrategyAttributePlatformName platform)
    {
      _attributes["platform"] = [GetEnumMemberValue(platform)];
      return this;
    }

    public IClientContext Country(StrategyAttributeCountryName country)
    {
      _attributes["country"] = [GetEnumMemberValue(country)];
      return this;
    }

    public IClientContext Version(string version)
    {
      _attributes["version"] = [version];

      return this;
    }

    public IClientContext Attr(string key, string value)
    {
      _attributes[key] = [value];
      return this;
    }

    public IClientContext Attrs(string key, List<string> values)
    {
      _attributes[key] = values;
      return this;
    }

    public IClientContext Clear()
    {
      _attributes.Clear();
      return this;
    }

    public string GetAttr(string key, string defaultValue)
    {
      if (_attributes.TryGetValue(key, out var attrs) && attrs.Count > 0)
        return attrs[0];

      return defaultValue;
    }

    public List<string> GetAttrs(string key)
    {
      if (_attributes.TryGetValue(key, out var attrs))
      {
        return attrs;
      }

      return new List<string>(0);
    }


    public string? DefaultPercentageKey
    {
      get
      {
        if (_attributes.TryGetValue("session", out var session))
          return session[0];
        if (_attributes.TryGetValue("userkey", out var userkey))
          return userkey[0];
        return null;
      }
    }

    public IFeature this[string name] => _repository.GetFeature(name).WithContext(this);

    public bool IsEnabled(string name)
    {
      return this[name].IsEnabled;
    }

    public bool IsSet(string name)
    {
      return this[name].IsSet;
    }

    public abstract Task<IClientContext> Build();

    public override string ToString()
    {
      var s = "CONTEXT: ";
      foreach (var key in _attributes.Keys)
      {
        s += $"key: {key} = ";
        foreach (var val in _attributes[key])
        {
          s += $"`{val}`,";
        }

        s += "\n";
      }

      return s;
    }
  }

  public class ServerEvalFeatureContext(
      IFeatureRepositoryContext repository,
      IFeatureHubConfig config,
      IEdgeService edgeService)
      : BaseClientContext(repository, config)
  {
    private string _xHeader = "";

    public override async Task<IClientContext> Build()
    {
      var newHeader = string.Join(",",
          _attributes.Select((e) => e.Key + "=" +
                                    HttpUtility.UrlEncode(string.Join(",", e.Value))).OrderBy(u => u));

      if (!string.Equals(newHeader, _xHeader, StringComparison.Ordinal))
      {
        _xHeader = newHeader;
        _repository.NotReady();
      }

      await edgeService.ContextChange(_xHeader);

      return this;
    }

    public override void Close()
    {
    }
  }

  public class ClientEvalFeatureContext(IFeatureRepositoryContext repository, IFeatureHubConfig config)
      : BaseClientContext(repository, config)
  {
#pragma warning disable 1998
    public override async Task<IClientContext> Build()
#pragma warning restore 1998
    {
      return this;
    }


    public override void Close()
    {
    }
  }
}
