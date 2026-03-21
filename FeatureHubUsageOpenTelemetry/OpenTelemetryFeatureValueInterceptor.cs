#nullable enable
using System;
using System.Globalization;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;
using OpenTelemetry;

namespace FeatureHubUsageOpenTelemetry;

/// <summary>
/// Intercepts feature value lookups using OpenTelemetry Baggage.
///
/// Reads the <c>fhub</c> baggage field, which is expected to be a comma-separated list of
/// <c>feature=url-encoded-value</c> pairs, e.g.:
/// <code>
///   my-flag=true,my-string=hello%20world,my-number=3.14
/// </code>
/// If the field is absent, or the requested key is not in the list, returns (false, null).
/// The decoded value is coerced to the type indicated by the feature's <see cref="FeatureValueType"/>.
/// </summary>
public class OpenTelemetryFeatureValueInterceptor : IFeatureValueInterceptor
{
  private const string BaggageKey = "fhub";

  public (bool, object?) GetValue(string key, IFeatureRepositoryContext repository, FeatureState? featureState)
  {
    var fhub = Baggage.Current.GetBaggage(BaggageKey);
    if (string.IsNullOrEmpty(fhub))
      return (false, null);

    foreach (var segment in fhub.Split(','))
    {
      var eqIndex = segment.IndexOf('=');
      if (eqIndex < 0)
        continue;

      var segmentKey = segment.AsSpan(0, eqIndex).Trim();
      if (!segmentKey.Equals(key.AsSpan(), StringComparison.Ordinal))
        continue;

      // Key matched — no point overriding if we have no type information
      if (featureState?.Type == null)
        return (false, null);

#pragma warning disable CA1846 // Uri.UnescapeDataString has no span-based overload
      var rawValue = Uri.UnescapeDataString(segment.Substring(eqIndex + 1));
#pragma warning restore CA1846
      return ConvertValue(rawValue, featureState.Type.Value);
    }

    return (false, null);
  }

  public void Close() { }

  private static (bool, object?) ConvertValue(string raw, FeatureValueType type) =>
      type switch
      {
        FeatureValueType.BOOLEAN when raw.Equals("true", StringComparison.OrdinalIgnoreCase)
              => (true, (object)true),
        FeatureValueType.BOOLEAN when raw.Equals("false", StringComparison.OrdinalIgnoreCase)
              => (true, (object)false),
        FeatureValueType.BOOLEAN
              => (false, null), // unrecognised boolean string
        FeatureValueType.NUMBER when double.TryParse(raw, NumberStyles.Any,
              CultureInfo.InvariantCulture, out var d)
              => (true, (object)d),
        FeatureValueType.NUMBER
              => (false, null), // unparseable number
        FeatureValueType.STRING or FeatureValueType.JSON
              => (true, (object)raw),
        _ => (false, null)
      };
}
