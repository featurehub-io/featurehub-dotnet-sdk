#nullable enable
using System;
using IO.FeatureHub.SSE.Model;

// because dependent library does

namespace FeatureHubSDK
{
  internal class FeatureStateBaseHolder : IFeature
  {
    private FeatureState? _feature;
    private readonly ApplyFeature _applyFeature;
    private IClientContext? _context;
    private readonly IFeatureRepositoryContext _repository;

    public IFeature WithContext(IClientContext context) => CopyFeatureStateHolder().SetContext(context);

    public event EventHandler<IFeature>? FeatureUpdateHandler;

    private FeatureStateBaseHolder SetContext(IClientContext context)
    {
      _context = context;
      return this;
    }

    public FeatureStateBaseHolder(FeatureStateBaseHolder? fs, ApplyFeature applyFeature, IFeatureRepositoryContext repository)
    {
      _applyFeature = applyFeature;
      _repository = repository;

      if (fs != null)
      {
        FeatureUpdateHandler = fs.FeatureUpdateHandler;
      }
    }

    public bool IsLocked => _feature?.L == true;

    private FeatureStateBaseHolder CopyFeatureStateHolder()
    {
      var fh = new FeatureStateBaseHolder(null, _applyFeature, _repository);
      if (_feature != null)
      {
        fh.FeatureState = this._feature;
      }

      return fh;
    }

    public IFeature Copy()
    {
      return CopyFeatureStateHolder();
    }

    public bool Exists => _feature != null;
    public bool BooleanFeature(bool defaultValue = false) => BooleanValue ?? defaultValue;

    public string StringFeature(string defaultValue = "") => StringValue ?? defaultValue;

    public double NumberFeature(double defaultValue = 0) => NumberValue ?? defaultValue;

    public string JsonFeature(string defaultValue = "{}") => JsonValue ?? defaultValue;

    public object? RawValue => _feature == null ? null : GetValue(_feature.Type, false);

    private object? GetValue(FeatureValueType? passedType, bool triggerUsage = true)
    {
      (bool interceptMatched, object? val) = _repository.FindIntercept(Key, _feature);

      FeatureValueType? type = passedType ?? _feature?.Type;

      if (interceptMatched)
      {
        return (triggerUsage && _feature != null) ? Used(_feature, val) : val;
      }

      if (type == null || _feature?.Type != type)
      {
        return null;
      }

      FeatureState feature = _feature;

      if (_context != null && feature.Strategies?.Count > 0)
      {
        Applied matched = _applyFeature.Apply(feature.Strategies, Key, feature.Id, _context);

        if (matched.Matched)
        {
          return triggerUsage ? Used(feature, matched.Value) : matched.Value;
        }
      }

      return triggerUsage ? Used(feature, feature.Value) : feature.Value;
    }

    private object? Used(FeatureState feature, object? value)
    {
      if (_context == null)
      {
        _repository.Used(feature, value);
      }
      else
      {
        _context.Used(feature, value);
      }

      return value;
    }

    public bool? BooleanValue
    {
      get
      {
        var val = GetValue(FeatureValueType.BOOLEAN);
        return val == null ? null : Convert.ToBoolean(val, System.Globalization.CultureInfo.InvariantCulture);
      }
    }

    public string? StringValue
    {
      get
      {
        var val = GetValue(FeatureValueType.STRING);
        return val == null ? null : Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
      }
    }

    public double? NumberValue
    {
      get
      {
        var val = GetValue(FeatureValueType.NUMBER);
        return val == null ? null : Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
      }
    }

    public string? JsonValue
    {
      get
      {
        var val = GetValue(FeatureValueType.JSON);
        return val == null ? null : Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
      }
    }

    public string Key => _feature?.Key ?? "unknown";
    public FeatureValueType? Type => _feature?.Type;
    public object? Value => GetValue(_feature?.Type);
    public object? UsageFreeValue => GetValue(_feature?.Type, false);

    public bool IsEnabled => BooleanValue == true;

    public bool IsSet => GetValue(_feature?.Type) != null;

    public long? Version => _feature?.VarVersion;

    public Guid? Id => _feature?.Id;
    public Guid? EnvironmentId => _feature?.EnvironmentId;

    public FeatureState FeatureState
    {
      set
      {
        object? oldVal = GetValue(_feature?.Type);
        _feature = value;
        object? val = GetValue(_feature?.Type);

        // did the value change? if so, tell everyone listening via event handler
        if (!ValueChanged(oldVal, val)) return;

        EventHandler<IFeature>? handler = FeatureUpdateHandler;

        try
        {
          handler?.Invoke(this, this);
        }
        catch (Exception e)
        {
          FeatureLogging.ErrorLogger(this, $"Failed to process update for feature {Key} {e.Message}");
        }
      }
    }

    public static bool ValueChanged(object? oldVal, object? value) => (value != null && !value.Equals(oldVal)) || (oldVal != null && !oldVal.Equals(value));
  }
}
