#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;


// because dependent library does

namespace FeatureHubSDK
{
  public abstract class AbstractFeatureHubRepository : IFeatureHubRepository
  {
    public abstract IFeature GetFeature(string key);

    public IFeature this[string name] => GetFeature(name);

    public bool IsEnabled(string name)
    {
      return GetFeature(name).IsEnabled;
    }

    public abstract event EventHandler<Readiness> ReadinessHandler;
    public abstract event EventHandler<IFeatureHubRepository> NewFeatureHandler;
    public abstract Readiness Readiness { get; }
    public abstract Readiness Readyness { get; }
    public abstract bool Exists(string key);
    public abstract RepositoryEventHandler RegisterUsageStream(Action<IUsageEvent> listener);
    public abstract void RegisterUsageProvider(IUsageProvider provider);
    public abstract IUsageProvider UsageProvider { get; }
    public abstract void RecordUsageEvent(IUsageEvent usageEvent);

    public abstract (bool, object?) FindIntercept(string key, FeatureState? featureState);
  }


  public class FeatureHubRepository(ApplyFeature applyFeature) : AbstractFeatureHubRepository, IFeatureRepositoryContext
  {
    private readonly ConcurrentDictionary<string, FeatureStateBaseHolder> _features =
      new ConcurrentDictionary<string, FeatureStateBaseHolder>();

    private Readiness _readiness = Readiness.NotReady;
    public override event EventHandler<Readiness> ReadinessHandler = delegate {};
    public override event EventHandler<IFeatureHubRepository> NewFeatureHandler = delegate {};
    private bool _serverSideEvaluation;

    private readonly List<IFeatureValueInterceptor> _interceptors = new List<IFeatureValueInterceptor>();

    private readonly ConcurrentDictionary<int, Action<IUsageEvent>> _usageStreams =
      new ConcurrentDictionary<int, Action<IUsageEvent>>();
    private int _usageStreamCounter;
    private IUsageProvider _usageProvider = DefaultUsageProvider.Instance;

    public override Readiness Readiness => _readiness;
    public override Readiness Readyness => _readiness;

    public FeatureHubRepository() : this(new ApplyFeature(new PercentageMurmur3Calculator(), new MatcherRegistry()))
    {
    }

    private void TriggerReadyness()
    {
      try
      {
        ReadinessHandler.Invoke(this, _readiness);
      }
      catch (Exception e)
      {
        FeatureLogging.ExceptionLogger(this,new ExceptionEvent($"Failed to indicate readyness change to {_readiness}", e));
      }
    }

    private void TriggerNewUpdate()
    {
      try
      {
        NewFeatureHandler.Invoke(this, this);
      }
      catch (Exception e)
      {
        FeatureLogging.ExceptionLogger(this,new ExceptionEvent("Failed to indicate trigger new feature change.", e));
      }
    }

    public void UpdateFeatures(IEnumerable<FeatureState>? features)
    {
      if (features == null) return;
      
      var updated = false;
      foreach (var featureState in features)
      {
        updated = FeatureUpdate(featureState) || updated;
      }

      if (_readiness != Readiness.Ready)
      {
        // are we newly ready?
        _readiness = Readiness.Ready;
        TriggerReadyness();
      }

      // we updated something, so let the folks know
      if (updated)
      {
        TriggerNewUpdate();
      }
    }

    // Notify
    public bool ServerSideEvaluation
    {
      get => _serverSideEvaluation;
      set => _serverSideEvaluation = value;
    }

    public void Notify(SSEResultState state, string? data, Guid EnvironmentId)
    {
      // Console.WriteLine($"received {state} with object {data}");

      switch (state)
      {
        case SSEResultState.Ack:
          break;
        case SSEResultState.Bye:
          // swap to not ready and let everyone know
          _readiness = Readiness.NotReady;
          TriggerReadyness();
          break;
        case SSEResultState.Failure:
          _readiness = Readiness.Failed;
          TriggerReadyness();
          break;
        case SSEResultState.Features:
          if (data != null)
          {
            var features = JsonConvert.DeserializeObject<List<FeatureState>>(data);
            if (features == null) return;
            foreach (var featureState in features)
            {
              featureState.EnvironmentId = EnvironmentId;
            } 
            UpdateFeatures(features);
          }

          break;
        case SSEResultState.Feature:
          if (data != null)
          {
            var fu = JsonConvert.DeserializeObject<FeatureState>(data);
            if (fu == null) return;
            fu.EnvironmentId = EnvironmentId;
            if (FeatureUpdate(fu))
            {
              TriggerNewUpdate();
            }
          }

          break;
        case SSEResultState.DeleteFeature:
          if (data != null)
          {
            var fu = JsonConvert.DeserializeObject<FeatureState>(data);
            if (fu == null) return;
            fu.EnvironmentId = EnvironmentId;
            DeleteFeature(fu);
          }

          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }
    }

    public void NotReady()
    {
      _readiness = Readiness.NotReady;
      TriggerReadyness();
    }

    private void DeleteFeature(FeatureState? fs)
    {
      if (fs == null) return;
      if (_features.TryRemove(fs.Key, out var _))
      {
        TriggerNewUpdate();        
      }
    }


    // update the feature if its version is greater than the version we currently store
    private bool FeatureUpdate(FeatureState? fs)
    {
      if (fs == null) return false;
      
      if (_features.TryGetValue(fs.Key, out var holder))
      {
        if (holder?.Key == null)
        {
          // key has arrived, so create a new holder but steal the internal events from the old one
          holder = new FeatureStateBaseHolder(holder, applyFeature, this);
          _features[fs.Key] = holder;
        }
        else if (holder.Version != null)
        { // its a real one, so check if the version or value actually changed. Any structural change would update the version
          if (holder.Version > fs.VarVersion || (
                holder.Version == fs.VarVersion && !FeatureStateBaseHolder.ValueChanged(holder.Value, fs.Value)))
          {
            return false;
          }
        }
      }
      else
      { // its a new feature we haven't seen before, yam it in
        holder = new FeatureStateBaseHolder(null, applyFeature, this);
        _features.TryAdd(fs.Key, holder);
      }

      holder.FeatureState = fs;

      return true;
    }

    public IFeature FeatureState(string key)
    {
      if (!_features.ContainsKey(key))
      {
        _features.TryAdd(key, new FeatureStateBaseHolder(null, applyFeature, this));
      }

      var feat = _features[key];
      return feat;
    }

    public override bool Exists(string key)
    {
      if (_features.TryGetValue(key, out var feature))
      {
        return feature.Type != null;
      }

      return false;
    }

    public override IFeature GetFeature(string key)
    {
      return FeatureState(key);
    }

    public bool IsSet(string key)
    {
      return FeatureState(key).IsSet;
    }

    public override RepositoryEventHandler RegisterUsageStream(Action<IUsageEvent> listener)
    {
      var handle = System.Threading.Interlocked.Increment(ref _usageStreamCounter);
      _usageStreams[handle] = listener;
      return new RepositoryEventHandler(() => _usageStreams.TryRemove(handle, out _));
    }

    public override void RegisterUsageProvider(IUsageProvider provider)
    {
      _usageProvider = provider;
    }

    public override IUsageProvider UsageProvider => _usageProvider;
    
    public override (bool, object?) FindIntercept(string key, FeatureState? featureState)
    {
      foreach (var interceptor in _interceptors)
      {
        var (matched, value) = interceptor.GetValue(key, this, featureState);

        if (matched)
        {
          // if we have no feature state and it therefore has no state, lets check if its a bool
          if (featureState == null && value != null)
          {
            if (value.ToString().ToLower() == "false")
            {
              return (true, false);
            }

            if (value.ToString().ToLower() == "true")
            {
              return (true, true);
            }
          }

          return (true, value);
        }
      }

      return (false, null);
    }

    public void Used(FeatureState featureState, object? value)
    {
      RecordUsageEvent(
        UsageProvider.CreateUsageEventWithFeature(new FeatureHubUsageValue(featureState, value), null, null));
    }

    public List<string> AllKeys()
    {
      return _features.Keys.ToList();
    }

    /// <summary>
    /// Fans out a usage event to all registered usage stream listeners.
    /// </summary>
    public override void RecordUsageEvent(IUsageEvent usageEvent)
    {
      foreach (var listener in _usageStreams.Values)
      {
        listener(usageEvent);
      }
    }
    
    public void AddFeatureValueInterceptor(IFeatureValueInterceptor interceptor)
    {
      _interceptors.Add(interceptor);
    }
    
    
  }
  
}