using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public interface ILLMService
    {
        Task<LLMResponse> GetMappingSuggestionsAsync(string placeholder, List<string> availableColumns);
        Task<LLMResponse> GenerateContentAsync(string prompt, Dictionary<string, string> context);
        Task<LLMResponse> DetectErrorsAsync(string content);
        Task<LLMResponse> ImproveContentAsync(string content, string style = "professional");
        Task<LLMResponse> TranslateContentAsync(string content, string targetLanguage);
    }

    public class LLMResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public float Confidence { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    public class LLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public LLMService(HttpClient httpClient, string apiKey, string apiUrl = "https://api.openai.com/v1/chat/completions")
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _apiUrl = apiUrl;
        }

        public async Task<LLMResponse> GetMappingSuggestionsAsync(string placeholder, List<string> availableColumns)
        {
            try
            {
                var prompt = $@"You are an expert at data mapping. Given a placeholder name and available column names, suggest the best matches.

Placeholder: {placeholder}
Available Columns: {string.Join(", ", availableColumns)}

Please suggest the top 3 best matches with confidence scores (0-1). Format as JSON:
{{
  ""suggestions"": [
    {{""column"": ""column_name"", ""confidence"": 0.95, ""reason"": ""exact match""}},
    {{""column"": ""column_name"", ""confidence"": 0.8, ""reason"": ""similar meaning""}}
  ]
}}";

                var response = await CallOpenAIAsync(prompt);
                return ParseMappingResponse(response);
            }
            catch (Exception ex)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<LLMResponse> GenerateContentAsync(string prompt, Dictionary<string, string> context)
        {
            try
            {
                var contextString = string.Join("\n", context.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                var fullPrompt = $@"Context:
{contextString}

Task: {prompt}

Please generate professional, clear content that fits the context. Be concise but comprehensive.";

                var response = await CallOpenAIAsync(fullPrompt);
                return new LLMResponse
                {
                    Success = true,
                    Content = response,
                    Confidence = 0.8f
                };
            }
            catch (Exception ex)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<LLMResponse> DetectErrorsAsync(string content)
        {
            try
            {
                var prompt = $@"Please review the following content for errors and suggest improvements:

{content}

Look for:
- Spelling mistakes
- Grammar errors
- Inconsistent formatting
- Unclear sentences
- Missing information

Format as JSON:
{{
  ""errors"": [
    {{""type"": ""spelling"", ""text"": ""incorrect word"", ""suggestion"": ""correct word"", ""position"": 10}},
    {{""type"": ""grammar"", ""text"": ""incorrect phrase"", ""suggestion"": ""correct phrase"", ""position"": 25}}
  ],
  ""overall_quality"": 0.8
}}";

                var response = await CallOpenAIAsync(prompt);
                return ParseErrorResponse(response);
            }
            catch (Exception ex)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<LLMResponse> ImproveContentAsync(string content, string style = "professional")
        {
            try
            {
                var prompt = $@"Please improve the following content to be more {style}:

{content}

Make it:
- More engaging and clear
- Professional in tone
- Better structured
- More concise if needed
- More comprehensive if needed

Return only the improved content.";

                var response = await CallOpenAIAsync(prompt);
                return new LLMResponse
                {
                    Success = true,
                    Content = response,
                    Confidence = 0.85f
                };
            }
            catch (Exception ex)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<LLMResponse> TranslateContentAsync(string content, string targetLanguage)
        {
            try
            {
                var prompt = $@"Please translate the following content to {targetLanguage}. Maintain the original formatting and tone:

{content}

Return only the translated content.";

                var response = await CallOpenAIAsync(prompt);
                return new LLMResponse
                {
                    Success = true,
                    Content = response,
                    Confidence = 0.9f
                };
            }
            catch (Exception ex)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that provides accurate, professional responses." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API error: {response.StatusCode} - {responseContent}");
            }

            var openAIResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);
            return openAIResponse.Choices[0].Message.Content;
        }

        private LLMResponse ParseMappingResponse(string response)
        {
            try
            {
                var mappingData = JsonConvert.DeserializeObject<MappingSuggestions>(response);
                return new LLMResponse
                {
                    Success = true,
                    Suggestions = mappingData.Suggestions.ConvertAll(s => $"{s.Column} (confidence: {s.Confidence:F2}) - {s.Reason}"),
                    Confidence = mappingData.Suggestions.Count > 0 ? mappingData.Suggestions[0].Confidence : 0f
                };
            }
            catch
            {
                return new LLMResponse
                {
                    Success = true,
                    Content = response,
                    Confidence = 0.5f
                };
            }
        }

        private LLMResponse ParseErrorResponse(string response)
        {
            try
            {
                var errorData = JsonConvert.DeserializeObject<ErrorDetection>(response);
                var suggestions = errorData.Errors.ConvertAll(e => $"{e.Type}: '{e.Text}' → '{e.Suggestion}'");
                
                return new LLMResponse
                {
                    Success = true,
                    Suggestions = suggestions,
                    Confidence = errorData.OverallQuality
                };
            }
            catch
            {
                return new LLMResponse
                {
                    Success = true,
                    Content = response,
                    Confidence = 0.5f
                };
            }
        }
    }

    // Response Models
    public class OpenAIResponse
    {
        public List<OpenAIChoice> Choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage Message { get; set; }
    }

    public class OpenAIMessage
    {
        public string Content { get; set; }
    }

    public class MappingSuggestions
    {
        public List<MappingSuggestion> Suggestions { get; set; }
    }

    public class MappingSuggestion
    {
        public string Column { get; set; }
        public float Confidence { get; set; }
        public string Reason { get; set; }
    }

    public class ErrorDetection
    {
        public List<ErrorItem> Errors { get; set; }
        public float OverallQuality { get; set; }
    }

    public class ErrorItem
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Suggestion { get; set; }
        public int Position { get; set; }
    }
}
