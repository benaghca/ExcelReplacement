using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExcelReplacement.Models
{
    public class ExcelRecord
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            return Values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public Dictionary<string, string> GetReplacements()
        {
            return Values.ToDictionary(
                kvp => $"[{kvp.Key}]",
                kvp => kvp.Value
            );
        }

        public string GenerateFileName(string pattern, string extension = ".xlsx")
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                // Default pattern if none provided
                pattern = "[Location] [Facility] [Equipment] [Procedure]";
            }

            var fileName = pattern;
            // Replace all placeholders in the pattern
            var placeholders = Regex.Matches(pattern, "\\[[^\\]]+\\]")
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct();
            foreach (var placeholder in placeholders)
            {
                var key = placeholder.Trim('[', ']');
                var value = Values.ContainsKey(key) ? Values[key] : string.Empty;
                fileName = fileName.Replace(placeholder, value);
            }

            // Remove any double spaces and trim
            fileName = string.Join(" ", fileName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            fileName = fileName.Trim();

            // Sanitize filename for Windows filesystem
            fileName = SanitizeFileName(fileName);

            return fileName + extension;
        }

        private string SanitizeFileName(string fileName)
        {
            // Characters not allowed in Windows filenames
            var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
            
            foreach (var invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '-');
            }
            
            // Remove any leading/trailing dots and spaces
            fileName = fileName.Trim('.', ' ');
            
            // Ensure filename is not empty
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "GeneratedFile";
            }
            
            // Limit filename length (Windows has a 255 character limit for filenames)
            if (fileName.Length > 200)
            {
                fileName = fileName.Substring(0, 200);
            }
            
            return fileName;
        }
    }
} 