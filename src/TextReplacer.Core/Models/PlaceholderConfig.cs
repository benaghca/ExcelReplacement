using System.Text.RegularExpressions;

namespace TextReplacer.Core.Models;

/// <summary>
/// Configuration for placeholder syntax (prefix and suffix characters).
/// </summary>
public class PlaceholderConfig
{
    public string Prefix { get; set; } = "[";
    public string Suffix { get; set; } = "]";

    /// <summary>
    /// Builds a regex pattern that matches placeholders and captures the field name.
    /// </summary>
    public Regex BuildPattern()
    {
        var escapedPrefix = Regex.Escape(Prefix);
        var escapedSuffix = Regex.Escape(Suffix);
        // Match prefix, capture everything that's not the suffix, then suffix
        return new Regex($"{escapedPrefix}([^{escapedSuffix}]+){escapedSuffix}", RegexOptions.Compiled);
    }

    /// <summary>
    /// Wraps a field name with the configured prefix and suffix.
    /// </summary>
    public string WrapFieldName(string fieldName) => $"{Prefix}{fieldName}{Suffix}";
}
