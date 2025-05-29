using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System;

namespace OojuInteractionPlugin
{
    // Interface for LLM service abstraction
    public interface ILLMService
    {
        Task<string> RequestInteraction(string prompt);
    }

    // OpenAI implementation
    public class OpenAIService : ILLMService
    {
        private string apiKey;
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string MODEL = "gpt-4o";
        public OpenAIService(string apiKey) { this.apiKey = apiKey; }
        public async Task<string> RequestInteraction(string prompt)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("OpenAI API Key is not set");
            var requestData = new
            {
                model = MODEL,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 800,
                temperature = 0.6
            };
            string jsonData = JsonConvert.SerializeObject(requestData);
            using (UnityWebRequest request = new UnityWebRequest(OPENAI_API_URL, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                await request.SendWebRequestAsync();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                    return response?.choices?[0]?.message?.content ?? "No response generated";
                }
                else
                {
                    string errorMessage = $"OpenAI API Error: {request.error}";
                    return errorMessage;
                }
            }
        }
        // OpenAI response classes
        [Serializable]
        public class OpenAIResponse { public Choice[] choices; }
        [Serializable]
        public class Choice { public Message message; }
        [Serializable]
        public class Message { public string content; }
    }

    // Claude (Anthropic) implementation
    public class ClaudeService : ILLMService
    {
        private string apiKey;
        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-opus-4-20250514";
        public ClaudeService(string apiKey) { this.apiKey = apiKey; }
        public async Task<string> RequestInteraction(string prompt)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Claude API Key is not set");
            var requestData = new
            {
                model = MODEL,
                max_tokens = 1024,
                messages = new[] {
                    new { role = "user", content = prompt }
                }
            };
            string jsonData = JsonConvert.SerializeObject(requestData);
            using (UnityWebRequest request = new UnityWebRequest(CLAUDE_API_URL, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", apiKey);
                request.SetRequestHeader("anthropic-version", "2023-06-01");
                await request.SendWebRequestAsync();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<ClaudeResponse>(request.downloadHandler.text);
                    if (response?.content != null && response.content.Length > 0)
                        return response.content[0].text;
                    return "No response generated";
                }
                else
                {
                    string errorMessage = $"Claude API Error: {request.error}";
                    return errorMessage;
                }
            }
        }
        [Serializable]
        public class ClaudeResponse
        {
            public ClaudeContent[] content;
        }
        [Serializable]
        public class ClaudeContent
        {
            public string type;
            public string text;
        }
    }

    // Gemini (Google) implementation
    public class GeminiService : ILLMService
    {
        private string apiKey;
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        public GeminiService(string apiKey) { this.apiKey = apiKey; }
        public async Task<string> RequestInteraction(string prompt)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Gemini API Key is not set");
            var requestData = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                }
            };
            string jsonData = JsonConvert.SerializeObject(requestData);
            string url = $"{GEMINI_API_URL}?key={apiKey}";
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequestAsync();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<GeminiResponse>(request.downloadHandler.text);
                    // Gemini returns candidates[0].content.parts[0].text
                    if (response?.candidates != null && response.candidates.Length > 0 &&
                        response.candidates[0].content?.parts != null && response.candidates[0].content.parts.Length > 0)
                        return response.candidates[0].content.parts[0].text;
                    return "No response generated";
                }
                else
                {
                    string errorMessage = $"Gemini API Error: {request.error}";
                    return errorMessage;
                }
            }
        }
        [Serializable]
        public class GeminiResponse
        {
            public GeminiCandidate[] candidates;
        }
        [Serializable]
        public class GeminiCandidate
        {
            public GeminiContent content;
        }
        [Serializable]
        public class GeminiContent
        {
            public GeminiPart[] parts;
        }
        [Serializable]
        public class GeminiPart
        {
            public string text;
        }
    }

    // Extension method for UnityWebRequest to support async/await
    public static class UnityWebRequestExtensions
    {
        public static Task<UnityWebRequest> SendWebRequestAsync(this UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();
            var operation = request.SendWebRequest();
            operation.completed += _ => tcs.SetResult(request);
            return tcs.Task;
        }
    }
} 