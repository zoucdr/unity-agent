using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Client for communicating with LLM APIs
    /// </summary>
    public class LLMClient
    {
        private ModelApiConfig config;
        private static HttpClient httpClient;

        static LLMClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        public LLMClient(ModelApiConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Generate a response from the LLM
        /// </summary>
        public async Task<string> GenerateResponse(string systemPrompt, string userPrompt)
        {
            switch (config.Provider)
            {
                case ModelProvider.Ollama:
                    return await CallOllama(systemPrompt, userPrompt);
                
                case ModelProvider.OpenAI:
                    return await CallOpenAI(systemPrompt, userPrompt);
                
                case ModelProvider.Anthropic:
                    return await CallAnthropic(systemPrompt, userPrompt);
                
                default:
                    throw new NotSupportedException($"Provider {config.Provider} is not supported");
            }
        }

        /// <summary>
        /// Call Ollama API
        /// </summary>
        private async Task<string> CallOllama(string systemPrompt, string userPrompt)
        {
            var url = $"{config.BaseUrl}/api/generate";
            
            var requestBody = new
            {
                model = config.ModelName,
                prompt = $"{systemPrompt}\n\nUser: {userPrompt}\n\nAssistant:",
                stream = false,
                options = new
                {
                    temperature = config.Temperature,
                    num_predict = config.MaxTokens
                }
            };

            var json = JsonUtility.ToJson(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                var responseText = await response.Content.ReadAsStringAsync();
                var responseObj = JsonUtility.FromJson<OllamaResponse>(responseText);
                
                return responseObj.response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMClient] Ollama API call failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Call OpenAI API
        /// </summary>
        private async Task<string> CallOpenAI(string systemPrompt, string userPrompt)
        {
            var url = $"{config.BaseUrl}/v1/chat/completions";
            
            var requestBody = $@"{{
                ""model"": ""{config.ModelName}"",
                ""messages"": [
                    {{""role"": ""system"", ""content"": ""{EscapeJson(systemPrompt)}""}},
                    {{""role"": ""user"", ""content"": ""{EscapeJson(userPrompt)}""}}
                ],
                ""temperature"": {config.Temperature},
                ""max_tokens"": {config.MaxTokens}
            }}";

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
            }
            
            try
            {
                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                var responseText = await response.Content.ReadAsStringAsync();
                var responseObj = JsonUtility.FromJson<OpenAIResponse>(responseText);
                
                return responseObj.choices[0].message.content;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMClient] OpenAI API call failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Call Anthropic API
        /// </summary>
        private async Task<string> CallAnthropic(string systemPrompt, string userPrompt)
        {
            var url = $"{config.BaseUrl}/v1/messages";
            
            var requestBody = $@"{{
                ""model"": ""{config.ModelName}"",
                ""system"": ""{EscapeJson(systemPrompt)}"",
                ""messages"": [
                    {{""role"": ""user"", ""content"": ""{EscapeJson(userPrompt)}""}}
                ],
                ""temperature"": {config.Temperature},
                ""max_tokens"": {config.MaxTokens}
            }}";

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            
            try
            {
                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                var responseText = await response.Content.ReadAsStringAsync();
                var responseObj = JsonUtility.FromJson<AnthropicResponse>(responseText);
                
                return responseObj.content[0].text;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMClient] Anthropic API call failed: {ex.Message}");
                throw;
            }
        }

        private string EscapeJson(string text)
        {
            return text.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    [Serializable]
    class OllamaResponse
    {
        public string response;
    }

    [Serializable]
    class OpenAIResponse
    {
        public Choice[] choices;
        
        [Serializable]
        public class Choice
        {
            public Message message;
        }
        
        [Serializable]
        public class Message
        {
            public string content;
        }
    }

    [Serializable]
    class AnthropicResponse
    {
        public Content[] content;
        
        [Serializable]
        public class Content
        {
            public string text;
        }
    }
}
