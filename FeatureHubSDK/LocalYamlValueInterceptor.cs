#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using IO.FeatureHub.SSE.Model;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace FeatureHubSDK
{
  /// <summary>
  /// Intercepts feature value lookups using a flat key→value map from a YAML file.
  ///
  /// Expected YAML structure:
  /// <code>
  /// flagValues:
  ///   my-boolean-flag: true
  ///   my-string-flag: hello
  ///   my-number-flag: 42
  ///   my-json-flag:
  ///     nested: value
  ///     list:
  ///       - a
  ///       - b
  /// </code>
  ///
  /// Type mapping (inferred from the YAML value):
  /// <list type="bullet">
  ///   <item>unquoted <c>true</c>/<c>false</c> → returns <c>bool</c></item>
  ///   <item>unquoted integer or floating-point → returns <c>double</c></item>
  ///   <item>quoted or plain string → returns <c>string</c></item>
  ///   <item>map or sequence → serialised to a JSON string</item>
  /// </list>
  ///
  /// The file is read once at construction. <see cref="AllowLockOverride"/> is <c>false</c>.
  /// </summary>
  public class LocalYamlValueInterceptor : IFeatureValueInterceptor
  {
    private readonly Dictionary<string, object?> _overrides =
      new Dictionary<string, object?>(StringComparer.Ordinal);

    public bool AllowLockOverride => false;

    public LocalYamlValueInterceptor(string filePath)
    {
      if (!File.Exists(filePath))
        return;

      var stream = new YamlStream();
      using (var reader = new StreamReader(filePath))
        stream.Load(reader);

      if (stream.Documents.Count == 0)
        return;

      if (stream.Documents[0].RootNode is not YamlMappingNode root)
        return;

      foreach (var topLevel in root.Children)
      {
        if (topLevel.Key is not YamlScalarNode keyNode ||
            keyNode.Value != "flagValues" ||
            topLevel.Value is not YamlMappingNode flagMap)
          continue;

        foreach (var flag in flagMap.Children)
        {
          var flagKey = (flag.Key as YamlScalarNode)?.Value;
          if (string.IsNullOrEmpty(flagKey))
            continue;

          _overrides[flagKey!] = ConvertNode(flag.Value);
        }
        break; // only one flagValues block needed
      }
    }

    public (bool, object?) GetValue(string key, FeatureState? featureState)
    {
      if (_overrides.TryGetValue(key, out var value))
        return (true, value);

      return (false, null);
    }

    /// <summary>
    /// Converts a YAML node to the appropriate C# value.
    /// Scalars use the YAML tag (or plain-style heuristics) to distinguish bool/number/string.
    /// Mappings and sequences are serialised to a JSON string.
    /// </summary>
    private static object? ConvertNode(YamlNode node)
    {
      switch (node)
      {
        case YamlScalarNode scalar:
          return ConvertScalar(scalar);

        case YamlMappingNode mapping:
          return JsonConvert.SerializeObject(NormaliseMapping(mapping));

        case YamlSequenceNode sequence:
          return JsonConvert.SerializeObject(NormaliseSequence(sequence));

        default:
          return null;
      }
    }

    private static object? ConvertScalar(YamlScalarNode scalar)
    {
      var raw = scalar.Value;
      if (raw == null) return null;

      // Quoted scalars are always strings.
      if (scalar.Style == ScalarStyle.SingleQuoted || scalar.Style == ScalarStyle.DoubleQuoted)
        return raw;

      // YAML core-schema boolean tag
      if (scalar.Tag == "tag:yaml.org,2002:bool" ||
          raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
          raw.Equals("false", StringComparison.OrdinalIgnoreCase))
      {
        return raw.Equals("true", StringComparison.OrdinalIgnoreCase);
      }

      // YAML integer or float tag
      if (scalar.Tag == "tag:yaml.org,2002:int" || scalar.Tag == "tag:yaml.org,2002:float")
      {
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
          return d;
      }

      // Unquoted plain scalar: try number before falling through to string.
      if (scalar.Style == ScalarStyle.Plain &&
          double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
        return num;

      return raw;
    }

    private static Dictionary<string, object?> NormaliseMapping(YamlMappingNode mapping)
    {
      var result = new Dictionary<string, object?>();
      foreach (var child in mapping.Children)
      {
        var key = (child.Key as YamlScalarNode)?.Value ?? "";
        result[key] = NormaliseNode(child.Value);
      }
      return result;
    }

    private static List<object?> NormaliseSequence(YamlSequenceNode sequence)
    {
      var result = new List<object?>();
      foreach (var item in sequence.Children)
        result.Add(NormaliseNode(item));
      return result;
    }

    /// <summary>
    /// Produces a plain object graph (no JSON yet) suitable for Newtonsoft.Json serialisation.
    /// </summary>
    private static object? NormaliseNode(YamlNode node)
    {
      switch (node)
      {
        case YamlScalarNode scalar:
          return ConvertScalar(scalar);
        case YamlMappingNode mapping:
          return NormaliseMapping(mapping);
        case YamlSequenceNode sequence:
          return NormaliseSequence(sequence);
        default:
          return null;
      }
    }
  }
}
