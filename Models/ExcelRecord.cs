using System.Collections.Generic;

namespace ExcelReplacement.Models
{
    public class ExcelRecord
    {
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            return Values.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public string GenerateFileName()
        {
            return $"{GetValue("Site")} {GetValue("Building")} {GetValue("Equipment")} {GetValue("Lineup")} {GetValue("Procedure")}.xlsx";
        }
    }
} 