using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using OojuInteractionPlugin;

namespace OojuInteractionPlugin
{
    public static class OIDescriptor
    {
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string VISION_MODEL = "gpt-4o";
        private const string SUGGESTION_MODEL = "gpt-4o";

        public static async Task<string> GenerateSceneDescription()
        {
            try
            {
                string apiKey = OISettings.Instance.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.LogError("OpenAI API Key is not set. Please set it in the CAIG Settings window.");
                    return "Error: API Key is not set. Please set it in the CAIG Settings window.";
                }

                string screenshotPath = CaptureSceneScreenshot();
                if (string.IsNullOrEmpty(screenshotPath))
                {
                    return "Failed to capture scene screenshot.";
                }

                string description = await CallOpenAIAPI(screenshotPath);
                return description ?? "Failed to generate description.";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating scene description: {e.Message}");
                return $"Error: {e.Message}";
            }
        }

        public static async Task<Dictionary<string, string[]>> GenerateInteractionSuggestions(string sceneDescription, GameObject[] selectedObjects)
        {
            try
            {
                string apiKey = OISettings.Instance.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.LogError("OpenAI API Key is not set. Please set it in the CAIG Settings window.");
                    return null;
                }

                if (string.IsNullOrEmpty(sceneDescription))
                {
                    Debug.LogError("Scene description is required to generate interaction suggestions.");
                    return null;
                }

                if (selectedObjects == null || selectedObjects.Length == 0)
                {
                    Debug.LogError("No objects selected for interaction suggestions.");
                    return null;
                }

                var suggestions = new Dictionary<string, string[]>();
                foreach (var obj in selectedObjects)
                {
                    string[] objectSuggestions = await GetSuggestionsForObject(obj.name, sceneDescription);
                    suggestions[obj.name] = objectSuggestions;
                }

                return suggestions;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating interaction suggestions: {e.Message}");
                return null;
            }
        }

        private static async Task<string[]> GetSuggestionsForObject(string objectName, string sceneDescription)
        {
            try
            {
                var requestData = new
                {
                    model = SUGGESTION_MODEL,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"Scene Context: \"{sceneDescription}\"\n" +
                                    $"Object Name: \"{objectName}\"\n" +
                                    "Task: Suggest 3 plausible, short, action-oriented interaction sentences a player could perform with this object in a game context. " +
                                    "Start each suggestion on a new line. If the object seems purely decorative, static, background scenery, or non-interactive, respond ONLY with the word: NONE"
                        }
                    },
                    max_tokens = 150,
                    temperature = 0.6
                };

                string jsonData = JsonConvert.SerializeObject(requestData);

                using (UnityWebRequest request = new UnityWebRequest(OPENAI_API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {OISettings.Instance.ApiKey}");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                        string content = response?.choices?[0]?.message?.content;

                        if (content == "NONE")
                        {
                            return new[] { "NONE" };
                        }

                        var suggestions = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < suggestions.Length; i++)
                        {
                            suggestions[i] = System.Text.RegularExpressions.Regex.Replace(suggestions[i], @"^\s*(\d+\.|\*|-)", "").Trim();
                        }

                        if (suggestions.Length < 3)
                        {
                            var paddedSuggestions = new List<string>(suggestions);
                            while (paddedSuggestions.Count < 3)
                            {
                                paddedSuggestions.Add("Interact.");
                            }
                            suggestions = paddedSuggestions.ToArray();
                        }
                        else if (suggestions.Length > 3)
                        {
                            suggestions = suggestions.Take(3).ToArray();
                        }

                        return suggestions;
                    }
                    else
                    {
                        Debug.LogError($"OpenAI API Error: {request.error}");
                        return new[] { "ERROR" };
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting suggestions for object {objectName}: {e.Message}");
                return new[] { "ERROR" };
            }
        }

        private static string CaptureSceneScreenshot()
        {
            try
            {
                string tempPath = Path.Combine(Application.temporaryCachePath, "scene_screenshot.png");
                Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
                if (sceneCamera == null)
                {
                    Debug.LogError("No active scene view camera found");
                    return null;
                }

                RenderTexture rt = new RenderTexture(1024, 768, 24);
                sceneCamera.targetTexture = rt;
                sceneCamera.Render();

                Texture2D screenShot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                screenShot.Apply();

                byte[] bytes = screenShot.EncodeToPNG();
                File.WriteAllBytes(tempPath, bytes);

                RenderTexture.active = null;
                sceneCamera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(screenShot);

                return tempPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error capturing scene screenshot: {e.Message}");
                return null;
            }
        }

        private static async Task<string> CallOpenAIAPI(string imagePath)
        {
            try
            {
                string apiKey = OISettings.Instance.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("OpenAI API Key is not set");
                }

                if (!File.Exists(imagePath))
                {
                    throw new Exception("Screenshot file not found");
                }

                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string base64Image = Convert.ToBase64String(imageBytes);

                var requestData = new
                {
                    model = VISION_MODEL,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = "Please analyze this Unity scene and provide a brief description. Focus on the main objects and their relationships." },
                                new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } }
                            }
                        }
                    },
                    max_tokens = 300
                };

                string jsonData = JsonConvert.SerializeObject(requestData);

                using (UnityWebRequest request = new UnityWebRequest(OPENAI_API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {OISettings.Instance.ApiKey}");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                        return response?.choices?[0]?.message?.content ?? "No description generated";
                    }
                    else
                    {
                        string errorMessage = $"OpenAI API Error: {request.error}";
                        if (request.responseCode == 401)
                        {
                            errorMessage = "Invalid API Key. Please check your API Key in the CAIG Settings window.";
                        }
                        else if (request.responseCode == 404)
                        {
                            errorMessage = "API endpoint not found. Please check if the model name is correct.";
                        }
                        Debug.LogError(errorMessage);
                        return errorMessage;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error calling OpenAI API: {e.Message}");
                return $"Error: {e.Message}";
            }
        }

        [Serializable]
        public class OpenAIResponse
        {
            public Choice[] choices;
        }
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

    // Minimal UIStyles class for EditorWindow UI
    public class UIStyles
    {
        public bool IsInitialized => true;
        public void Initialize() { }
        public GUIStyle headerStyle => EditorStyles.boldLabel;
        public GUIStyle sectionHeaderStyle => EditorStyles.boldLabel;
        public GUIStyle subSectionHeaderStyle => EditorStyles.label;
        public GUIStyle tabStyle => EditorStyles.toolbarButton;
        public GUIStyle dropAreaStyle => EditorStyles.helpBox;
        public GUIStyle dropAreaActiveStyle => EditorStyles.helpBox;
        public GUIStyle buttonStyle => EditorStyles.miniButton;
        public GUIStyle removeButtonStyle => EditorStyles.miniButton;
        public GUIStyle iconButtonStyle => EditorStyles.miniButton;
        public GUIStyle centeredLabelStyle => EditorStyles.centeredGreyMiniLabel;
        public Color uploadButtonColor => Color.green;
        public Color downloadButtonColor => Color.cyan;
    }
}
