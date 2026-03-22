#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FeatureHubSDK;

namespace FeatureHubSDK;

/// <summary>
/// A <see cref="UsagePlugin"/> that forwards feature usage events to the current OpenTelemetry
/// span via <see cref="Activity.Current"/>.
///
/// Only events that also implement <see cref="IUsageEventName"/> are processed; others are ignored.
///
/// <list type="bullet">
///   <item>
///     <term><paramref name="attachAsSpanEvents"/> = false (default)</term>
///     <description>Sets attributes on the current span. Each attribute key is prefixed with
///     <paramref name="prefix"/>.</description>
///   </item>
///   <item>
///     <term><paramref name="attachAsSpanEvents"/> = true</term>
///     <description>Adds a span event whose name is <c>prefix + EventName</c> and whose
///     attributes are the map entries (keys are <em>not</em> prefixed — the event name carries
///     the context).</description>
///   </item>
/// </list>
///
/// Map values are converted to OTel-compatible types:
/// <c>bool</c>, <c>long</c>/<c>int</c>, <c>double</c>/<c>float</c>, <c>string</c>, <c>Guid</c>,
/// and <c>IEnumerable&lt;string&gt;</c> / <c>IEnumerable&lt;bool&gt;</c> /
/// <c>IEnumerable&lt;double&gt;</c>. Anything else is serialised via <c>ToString()</c>.
/// Null values are omitted.
/// </summary>
public class OpenTelemetryTrackerUsagePlugin : UsagePlugin
{
  private readonly string _prefix;
  private readonly bool _attachAsSpanEvents;

  public override bool CanSendAsync => false;

  public OpenTelemetryTrackerUsagePlugin(string prefix = "featurehub.", bool attachAsSpanEvents = false)
  {
    _prefix = prefix;
    _attachAsSpanEvents = attachAsSpanEvents;
  }

  public override void Send(IUsageEvent usageEvent)
  {
    if (usageEvent is not IUsageEventName named)
      return;

    var activity = Activity.Current;
    if (activity == null)
      return;

    var map = usageEvent.CollectUsageRecord();
    var eventName = named.EventName;

    if (_attachAsSpanEvents)
    {
      var tags = new ActivityTagsCollection();
      foreach (var kvp in map)
      {
        var converted = ConvertValue(kvp.Value);
        if (converted != null)
          tags[kvp.Key] = converted;
      }
      activity.AddEvent(new ActivityEvent(_prefix + eventName, tags: tags));
    }
    else
    {
      foreach (var kvp in map)
      {
        var converted = ConvertValue(kvp.Value);
        if (converted != null)
          activity.SetTag(_prefix + kvp.Key, converted);
      }
    }
  }

  /// <summary>
  /// Converts a map value to an OTel-supported attribute type.
  /// Returns null for null inputs (caller omits those entries).
  /// </summary>
  public static object? ConvertValue(object? value) => value switch
  {
    null => null,
    bool b => b,
    double d => d,
    float f => (double)f,
    long l => l,
    int i => (long)i,
    string s => s,
    Guid g => g.ToString(),
    IEnumerable<bool> bools => bools.ToArray(),
    IEnumerable<double> doubles => doubles.ToArray(),
    IEnumerable<long> longs => longs.ToArray(),
    IEnumerable<int> ints => ints.Select(i => (long)i).ToArray(),
    IEnumerable<string> strings => strings.ToArray(),
    // Generic enumerable — convert each element to string
    System.Collections.IEnumerable enumerable => enumerable.Cast<object?>()
        .Select(o => o?.ToString())
        .ToArray(),
    _ => value.ToString()
  };
}
