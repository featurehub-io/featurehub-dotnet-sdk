#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using FeatureHubSDK;
using OpenTelemetry;

namespace FeatureHubUsageOpenTelemetry;

/// <summary>
/// A <see cref="UsagePlugin"/> that propagates evaluated feature values into the current
/// OpenTelemetry Baggage under the <c>fhub</c> key, as a comma-separated list of
/// <c>feature=url-encoded-value</c> pairs. This is the mirror of
/// <see cref="OpenTelemetryFeatureValueInterceptor"/>.
///
/// <list type="bullet">
///   <item><see cref="IUsageEventWithFeature"/> — merges the single feature into the existing
///     <c>fhub</c> baggage entry, replacing the previous value for that key if present.</item>
///   <item><see cref="IUsageFeaturesCollection"/> — rebuilds the <c>fhub</c> baggage from the
///     full feature set using the <c>fhub_keys</c> index and <c>{key}_raw</c> entries from the
///     event map, preserving the original typed values without serialisation loss.</item>
///   <item>Any other <see cref="IUsageEvent"/> subtype is ignored.</item>
/// </list>
/// </summary>
public class OpenTelemetryUsagePlugin : UsagePlugin
{
    public override bool CanSendAsync => false;

    private const string BaggageKey = "fhub";
    private const string FhubKeysMapKey = "fhub_keys";

    public override void Send(IUsageEvent usageEvent)
    {
        switch (usageEvent)
        {
            case IUsageEventWithFeature withFeature:
                ApplySingleFeature(withFeature.Feature);
                break;
            case IUsageFeaturesCollection collection:
                ApplyFeatureCollection(collection);
                break;
        }
    }

    private static void ApplySingleFeature(FeatureHubUsageValue feature)
    {
        var encoded = EncodeRawValue(feature.RawValue);
        if (encoded == null) return;

        var entry = $"{feature.Key}={Uri.EscapeDataString(encoded)}";
        var current = Baggage.Current.GetBaggage(BaggageKey);
        Baggage.Current = Baggage.SetBaggage(BaggageKey, MergeEntry(current, feature.Key, entry));
    }

    private static void ApplyFeatureCollection(IUsageFeaturesCollection collection)
    {
        var map = collection.CollectUsageRecord();

        if (!map.TryGetValue(FhubKeysMapKey, out var keysObj) || keysObj == null) return;

        var keys = keysObj.ToString()!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (keys.Length == 0) return;

        var segments = new List<string>(keys.Length);
        foreach (var key in keys)
        {
            // Use the _raw entry to avoid serialisation loss (e.g. bool "on"/"off", JSON null)
            if (!map.TryGetValue(key + "_raw", out var rawVal)) continue;
            var encoded = EncodeRawValue(rawVal);
            if (encoded == null) continue;
            segments.Add($"{key}={Uri.EscapeDataString(encoded)}");
        }

        if (segments.Count == 0) return;

        segments.Sort(SegmentKeyComparer);
        Baggage.Current = Baggage.SetBaggage(BaggageKey, string.Join(",", segments));
    }

    /// <summary>
    /// Converts a raw feature value to a string that
    /// <see cref="OpenTelemetryFeatureValueInterceptor"/> can decode:
    /// <list type="bullet">
    ///   <item><c>bool</c>   → "true" / "false"</item>
    ///   <item><c>double</c> → invariant-culture decimal string</item>
    ///   <item><c>string</c> → as-is (covers both STRING and JSON feature types)</item>
    ///   <item><c>null</c>   → null (caller skips the entry)</item>
    /// </list>
    /// </summary>
    public static string? EncodeRawValue(object? rawValue) => rawValue switch
    {
        bool b => b ? "true" : "false",
        double d => d.ToString(CultureInfo.InvariantCulture),
        string s => s,
        null => null,
        // Fallback for any other numeric type (e.g. int stored by tests)
        _ => Convert.ToDouble(rawValue).ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Merges <paramref name="newEntry"/> into an existing <c>fhub</c> baggage string,
    /// replacing any existing segment whose key matches <paramref name="key"/>.
    /// </summary>
    private static string MergeEntry(string? current, string key, string newEntry)
    {
        if (string.IsNullOrEmpty(current))
            return newEntry;

        var prefix = key + "=";
        var segments = current.Split(',');
        var result = new string[segments.Length + 1];
        var count = 0;
        var found = false;

        foreach (var seg in segments)
        {
            if (seg.StartsWith(prefix, StringComparison.Ordinal))
            {
                result[count++] = newEntry;
                found = true;
            }
            else
            {
                result[count++] = seg;
            }
        }

        if (!found)
            result[count++] = newEntry;

        var list = new List<string>(result[..count]);
        list.Sort(SegmentKeyComparer);
        return string.Join(",", list);
    }

    private static int SegmentKeyComparer(string a, string b)
    {
        var keyA = a.AsSpan(0, a.IndexOf('='));
        var keyB = b.AsSpan(0, b.IndexOf('='));
        return keyA.CompareTo(keyB, StringComparison.Ordinal);
    }
}