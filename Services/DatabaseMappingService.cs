using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public interface IDatabaseMappingService
    {
        Task<MappingResult> AnalyzeDatabaseAsync(DataSource dataSource, string tableName);
        Task<MappingResult> SuggestMappingsAsync(string placeholder, List<DatabaseColumn> availableColumns);
        Task<MappingResult> AutoMapAllAsync(List<string> placeholders, List<DatabaseColumn> columns);
        Task<MappingResult> ValidateMappingsAsync(Dictionary<string, string> mappings, List<DatabaseColumn> columns);
    }

    public class DatabaseColumn
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public int MaxLength { get; set; }
        public List<string> SampleValues { get; set; } = new List<string>();
        public string Description { get; set; }
    }

    public class MappingSuggestion
    {
        public string Placeholder { get; set; }
        public string ColumnName { get; set; }
        public float Confidence { get; set; }
        public string Reason { get; set; }
        public List<string> SampleMatches { get; set; } = new List<string>();
    }

    public class MappingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<MappingSuggestion> Suggestions { get; set; } = new List<MappingSuggestion>();
        public List<DatabaseColumn> Columns { get; set; } = new List<DatabaseColumn>();
        public Dictionary<string, string> AutoMappings { get; set; } = new Dictionary<string, string>();
    }

    public class DatabaseMappingService : IDatabaseMappingService
    {
        private readonly ILLMService _llmService;
        private readonly IDatabaseService _databaseService;

        public DatabaseMappingService(ILLMService llmService, IDatabaseService databaseService)
        {
            _llmService = llmService;
            _databaseService = databaseService;
        }

        public async Task<MappingResult> AnalyzeDatabaseAsync(DataSource dataSource, string tableName)
        {
            try
            {
                // Get column information
                var columnResult = await _databaseService.GetColumnNamesAsync(dataSource, tableName);
                if (!columnResult.Success)
                {
                    return new MappingResult
                    {
                        Success = false,
                        ErrorMessage = columnResult.ErrorMessage
                    };
                }

                // Get sample data to understand column content
                var sampleResult = await _databaseService.GetSampleDataAsync(dataSource, tableName, 10);
                if (!sampleResult.Success)
                {
                    return new MappingResult
                    {
                        Success = false,
                        ErrorMessage = sampleResult.ErrorMessage
                    };
                }

                // Analyze columns
                var columns = new List<DatabaseColumn>();
                foreach (DataColumn col in sampleResult.Data.Columns)
                {
                    var column = new DatabaseColumn
                    {
                        Name = col.ColumnName,
                        DataType = col.DataType.Name,
                        IsNullable = col.AllowDBNull,
                        MaxLength = col.MaxLength
                    };

                    // Get sample values
                    var sampleValues = new List<string>();
                    foreach (DataRow row in sampleResult.Data.Rows)
                    {
                        if (row[col] != DBNull.Value)
                        {
                            sampleValues.Add(row[col].ToString());
                        }
                    }
                    column.SampleValues = sampleValues.Take(5).ToList();

                    // Generate description using LLM
                    var description = await GenerateColumnDescriptionAsync(column);
                    column.Description = description;

                    columns.Add(column);
                }

                return new MappingResult
                {
                    Success = true,
                    Columns = columns
                };
            }
            catch (Exception ex)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MappingResult> SuggestMappingsAsync(string placeholder, List<DatabaseColumn> availableColumns)
        {
            try
            {
                // Use LLM to analyze the placeholder and suggest best matches
                var columnNames = availableColumns.Select(c => c.Name).ToList();
                var llmResponse = await _llmService.GetMappingSuggestionsAsync(placeholder, columnNames);

                if (!llmResponse.Success)
                {
                    return new MappingResult
                    {
                        Success = false,
                        ErrorMessage = llmResponse.ErrorMessage
                    };
                }

                // Parse LLM suggestions and enhance with database context
                var suggestions = new List<MappingSuggestion>();
                foreach (var suggestion in llmResponse.Suggestions)
                {
                    var column = availableColumns.FirstOrDefault(c => c.Name == suggestion);
                    if (column != null)
                    {
                        suggestions.Add(new MappingSuggestion
                        {
                            Placeholder = placeholder,
                            ColumnName = column.Name,
                            Confidence = CalculateConfidence(placeholder, column),
                            Reason = GenerateReason(placeholder, column),
                            SampleMatches = column.SampleValues
                        });
                    }
                }

                return new MappingResult
                {
                    Success = true,
                    Suggestions = suggestions.OrderByDescending(s => s.Confidence).ToList()
                };
            }
            catch (Exception ex)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MappingResult> AutoMapAllAsync(List<string> placeholders, List<DatabaseColumn> columns)
        {
            try
            {
                var autoMappings = new Dictionary<string, string>();
                var suggestions = new List<MappingSuggestion>();

                foreach (var placeholder in placeholders)
                {
                    var suggestionResult = await SuggestMappingsAsync(placeholder, columns);
                    if (suggestionResult.Success && suggestionResult.Suggestions.Any())
                    {
                        var bestMatch = suggestionResult.Suggestions.First();
                        if (bestMatch.Confidence > 0.7f) // High confidence threshold
                        {
                            autoMappings[placeholder] = bestMatch.ColumnName;
                        }
                        suggestions.AddRange(suggestionResult.Suggestions);
                    }
                }

                return new MappingResult
                {
                    Success = true,
                    AutoMappings = autoMappings,
                    Suggestions = suggestions
                };
            }
            catch (Exception ex)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MappingResult> ValidateMappingsAsync(Dictionary<string, string> mappings, List<DatabaseColumn> columns)
        {
            try
            {
                var validationResults = new List<MappingSuggestion>();

                foreach (var mapping in mappings)
                {
                    var column = columns.FirstOrDefault(c => c.Name == mapping.Value);
                    if (column != null)
                    {
                        var confidence = CalculateConfidence(mapping.Key, column);
                        validationResults.Add(new MappingSuggestion
                        {
                            Placeholder = mapping.Key,
                            ColumnName = mapping.Value,
                            Confidence = confidence,
                            Reason = GenerateReason(mapping.Key, column),
                            SampleMatches = column.SampleValues
                        });
                    }
                }

                return new MappingResult
                {
                    Success = true,
                    Suggestions = validationResults
                };
            }
            catch (Exception ex)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GenerateColumnDescriptionAsync(DatabaseColumn column)
        {
            try
            {
                var sampleData = string.Join(", ", column.SampleValues);
                var prompt = $"Analyze this database column and provide a brief description:\n\n" +
                           $"Column Name: {column.Name}\n" +
                           $"Data Type: {column.DataType}\n" +
                           $"Sample Values: {sampleData}\n\n" +
                           $"Provide a 1-2 sentence description of what this column likely contains.";

                var response = await _llmService.GenerateContentAsync(prompt, new Dictionary<string, string>());
                return response.Success ? response.Content : "Database column";
            }
            catch
            {
                return "Database column";
            }
        }

        private float CalculateConfidence(string placeholder, DatabaseColumn column)
        {
            var confidence = 0.0f;

            // Exact match
            if (string.Equals(placeholder, column.Name, StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.4f;
            }

            // Partial match
            if (column.Name.ToLower().Contains(placeholder.ToLower()) || 
                placeholder.ToLower().Contains(column.Name.ToLower()))
            {
                confidence += 0.3f;
            }

            // Data type matching
            if (IsDataTypeMatch(placeholder, column.DataType))
            {
                confidence += 0.2f;
            }

            // Sample data analysis
            if (HasRelevantSampleData(placeholder, column.SampleValues))
            {
                confidence += 0.1f;
            }

            return Math.Min(confidence, 1.0f);
        }

        private bool IsDataTypeMatch(string placeholder, string dataType)
        {
            var placeholderLower = placeholder.ToLower();
            var dataTypeLower = dataType.ToLower();

            // Common data type patterns
            if (placeholderLower.Contains("id") && dataTypeLower.Contains("int"))
                return true;
            if (placeholderLower.Contains("date") && dataTypeLower.Contains("date"))
                return true;
            if (placeholderLower.Contains("name") && dataTypeLower.Contains("string"))
                return true;
            if (placeholderLower.Contains("number") && dataTypeLower.Contains("int"))
                return true;

            return false;
        }

        private bool HasRelevantSampleData(string placeholder, List<string> sampleValues)
        {
            var placeholderLower = placeholder.ToLower();
            
            foreach (var value in sampleValues)
            {
                var valueLower = value.ToLower();
                
                // Check if sample values contain placeholder keywords
                if (placeholderLower.Contains("unit") && valueLower.Contains("unit"))
                    return true;
                if (placeholderLower.Contains("serial") && valueLower.Contains("sn"))
                    return true;
                if (placeholderLower.Contains("model") && valueLower.Contains("model"))
                    return true;
                if (placeholderLower.Contains("building") && valueLower.Contains("building"))
                    return true;
            }

            return false;
        }

        private string GenerateReason(string placeholder, DatabaseColumn column)
        {
            var reasons = new List<string>();

            if (string.Equals(placeholder, column.Name, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add("Exact name match");
            }

            if (column.Name.ToLower().Contains(placeholder.ToLower()))
            {
                reasons.Add("Column name contains placeholder");
            }

            if (IsDataTypeMatch(placeholder, column.DataType))
            {
                reasons.Add("Data type compatibility");
            }

            if (HasRelevantSampleData(placeholder, column.SampleValues))
            {
                reasons.Add("Sample data relevance");
            }

            return reasons.Any() ? string.Join(", ", reasons) : "AI analysis";
        }
    }
}
