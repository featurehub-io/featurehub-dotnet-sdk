#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
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
  /// The file is read once at construction. When <paramref name="watch"/> is <c>true</c>, a
  /// <see cref="FileSystemWatcher"/> is started and the overrides are atomically reloaded
  /// whenever the file changes. Call <see cref="Close"/> to stop watching.
  /// </summary>
#pragma warning disable CA1001 // FileSystemWatcher and Timer are disposed in Close()
  public class LocalYamlValueInterceptor : IFeatureValueInterceptor
#pragma warning restore CA1001
  {
    private volatile Dictionary<string, object?> _overrides;
    private readonly string _filePath;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private readonly object _debounceLock = new object();
    private volatile bool _disposed;

    private const int DebounceMs = 300;

    /// <summary>
    /// Creates a new interceptor that reads overrides from <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Path to the YAML file containing a <c>flagValues</c> map.</param>
    /// <param name="watch">
    /// When <c>true</c>, a <see cref="FileSystemWatcher"/> monitors the file for changes and
    /// reloads overrides automatically. Call <see cref="Close"/> to stop watching.
    /// </param>
    public LocalYamlValueInterceptor(string filePath, bool watch = false)
    {
      _filePath = filePath;
      _overrides = LoadFile(filePath);

      if (!watch)
        return;

      var dir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".";
      var file = Path.GetFileName(filePath);

      _watcher = new FileSystemWatcher(dir, file)
      {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
        EnableRaisingEvents = true,
      };
      _watcher.Changed += OnFileChanged;
      _watcher.Created += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
      lock (_debounceLock)
      {
        _debounceTimer?.Dispose();

        if (_disposed)
          return;

        _debounceTimer = new Timer(_ => Reload(), null, DebounceMs, Timeout.Infinite);
      }
    }

    private void Reload()
    {
      if (_disposed)
        return;

      try
      {
        _overrides = LoadFile(_filePath);
      }
      catch (IOException)
      {
        // Keep current values if the file is temporarily locked during a write.
      }
    }

    public (bool, object?) GetValue(string key, IFeatureRepositoryContext repository, FeatureState? featureState)
    {
      if (_overrides.TryGetValue(key, out var value))
        return (true, value);

      return (false, null);
    }

    /// <summary>
    /// Stops watching the file for changes and releases all resources.
    /// Safe to call multiple times.
    /// </summary>
    public void Close()
    {
      if (_disposed)
        return;

      _disposed = true;
      _watcher?.Dispose();
      _watcher = null;

      lock (_debounceLock)
      {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
      }
    }

    private static Dictionary<string, object?> LoadFile(string filePath)
    {
      var overrides = new Dictionary<string, object?>(StringComparer.Ordinal);

      if (!File.Exists(filePath))
        return overrides;

      var stream = new YamlStream();
      using (var reader = new StreamReader(filePath))
        stream.Load(reader);

      if (stream.Documents.Count == 0)
        return overrides;

      if (stream.Documents[0].RootNode is not YamlMappingNode root)
        return overrides;

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

          overrides[flagKey!] = ConvertNode(flag.Value);
        }
        break; // only one flagValues block needed
      }

      return overrides;
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
      if (raw == null)
        return null;

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
