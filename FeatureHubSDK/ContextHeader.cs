using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FeatureHubSDK
{
  /// <summary>
  /// The x-featurehub header and contextSha exactly as featurehub-protocol.yaml encoding.evaluation_context
  /// specifies them, so every FeatureHub SDK sends the same values for the same context.
  /// </summary>
  public static class ContextHeader
  {
    private const string Hex = "0123456789ABCDEF";

    /// <summary>
    /// Each attribute with at least one non-null value becomes EncodeUriComponent(key) + "=" +
    /// EncodeUriComponent(values joined by ","); the encoded strings are sorted by ordinal comparison and
    /// joined with ",". Returns an empty string when no attribute has a value.
    /// </summary>
    public static string XFeatureHubHeader(IDictionary<string, List<string>> attributes)
    {
      if (attributes == null)
      {
        return "";
      }

      var parts = new List<string>();

      foreach (var attribute in attributes)
      {
        var values = attribute.Value?.Where(v => v != null).ToList();

        if (values != null && values.Count > 0)
        {
          parts.Add(EncodeUriComponent(attribute.Key) + "=" + EncodeUriComponent(string.Join(",", values)));
        }
      }

      // a culture-sensitive sort (the OrderBy default) would order these differently on different machines
      parts.Sort(StringComparer.Ordinal);

      return string.Join(",", parts);
    }

    /// <summary>
    /// The SHA-256 of the header as 64 lowercase hex characters, or "0" when there is no header.
    /// </summary>
    public static string ContextSha(string header)
    {
      if (string.IsNullOrEmpty(header))
      {
        return "0";
      }

      using (var sha = SHA256.Create())
      {
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(header));
        var hex = new StringBuilder(hash.Length * 2);

        foreach (var b in hash)
        {
          hex.Append(b.ToString("x2"));
        }

        return hex.ToString();
      }
    }

    /// <summary>
    /// Encodes as JavaScript's encodeURIComponent does: UTF-8, then every byte except A-Z a-z 0-9 - _ . ! ~ * ' ( )
    /// as %XX with uppercase hex. HttpUtility.UrlEncode is form encoding (a space as "+", lowercase hex) and
    /// Uri.EscapeDataString encodes ! * ' ( ), so neither is used.
    /// </summary>
    public static string EncodeUriComponent(string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value ?? "");
      var encoded = new StringBuilder(bytes.Length);

      foreach (var b in bytes)
      {
        var c = (char) b;

        if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
            c == '-' || c == '_' || c == '.' || c == '!' || c == '~' || c == '*' || c == '\'' || c == '(' || c == ')')
        {
          encoded.Append(c);
        }
        else
        {
          encoded.Append('%').Append(Hex[b >> 4]).Append(Hex[b & 0x0f]);
        }
      }

      return encoded.ToString();
    }
  }
}
