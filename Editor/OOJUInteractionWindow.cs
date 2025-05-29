using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using OojuInteractionPlugin;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;

namespace OojuInteractionPlugin
{
    public class OOJUInteractionWindow : EditorWindow
    {
        // Interaction Tab
        private Vector2 mainScrollPosition = Vector2.zero;
        private Vector2 analyzerScrollPosition = Vector2.zero;
        private Vector2 descriptionScrollPosition = Vector2.zero;
        private string sceneDescription = "";
        private bool isGeneratingDescription = false;
        private Dictionary<string, string[]> interactionSuggestions = null;        private string caigApiKey = null;
        private string caigApiKeyTemp = null;

        private enum InteractionTab { Tools, Settings }
        private InteractionTab currentInteractionTab = InteractionTab.Tools;

        private UIStyles styles;
        private AnimationUI animationUI;

        private string userInteractionInput = "";

        // Stores the result from Sentence-to-Interaction
        private string sentenceToInteractionResult = "";

        // Stores the found objects from the last interaction
        private List<GameObject> foundSuggestedObjects = new List<GameObject>();
        private string lastGeneratedScriptPath = "";
        private string lastSuggestedObjectNames = "";

        // Stores the summary of the generated script

        private Vector2 lastScriptSummaryScroll = Vector2.zero;

        // Stores user input for each object
        private Dictionary<string, string> userObjectInput = new Dictionary<string, string>();

        // Stores the last generated class name for assignment
        private string lastGeneratedClassName = "";

        // Unified blue-gray color for all buttons and section titles
        private Color unifiedButtonColor = new Color(0.22f, 0.32f, 0.39f, 1f);

        // Use a lighter, less saturated blue-gray for all main action buttons
        private Color mainActionButtonColor = new Color(0.38f, 0.50f, 0.58f, 1f);

        // 색상 정의
        private readonly Color32 SectionTitleColor = new Color32(0xFC, 0xFC, 0xFC, 0xFF);
        private readonly Color32 DescriptionTextColor = new Color32(0xFC, 0xFC, 0xFC, 0xFF);
        private readonly Color32 ButtonBgColor = new Color32(0x67, 0x67, 0x67, 0xFF);
        private readonly Color32 ButtonTextColor = new Color32(0xDA, 0xDA, 0xDA, 0xFF);
        private readonly Color32 DisabledButtonBgColor = new Color32(0xB8, 0xB8, 0xB8, 0xFF);
        private readonly Color32 InputTextColor = new Color32(0xDA, 0xDA, 0xDA, 0xFF);

        // EditorPrefs 키 정의
        private const string PrefKey_SceneDescription = "OOJU_Pref_SceneDescription";
        private const string PrefKey_InteractionSuggestions = "OOJU_Pref_InteractionSuggestions";
        private const string PrefKey_UserInteractionInput = "OOJU_Pref_UserInteractionInput";
        private const string PrefKey_SentenceToInteractionResult = "OOJU_Pref_SentenceToInteractionResult";
        private const string PrefKey_LastGeneratedScriptPath = "OOJU_Pref_LastGeneratedScriptPath";
        private const string PrefKey_LastGeneratedClassName = "OOJU_Pref_LastGeneratedClassName";
        private const string PrefKey_LastSuggestedObjectNames = "OOJU_Pref_LastSuggestedObjectNames";
        private const string PrefKey_FoundSuggestedObjects = "OOJU_Pref_FoundSuggestedObjects";

        // Stores the last generated suggestion for each object
        private Dictionary<string, string> lastGeneratedSuggestionPerObject = new Dictionary<string, string>();

        [MenuItem("OOJU/Interaction")]
        public static void ShowWindow()
        {
            GetWindow<OOJUInteractionWindow>("OOJU Interaction");
        }

        private void OnEnable()
        {
            styles = new UIStyles();
            animationUI = new AnimationUI();
            caigApiKey = OISettings.Instance.ApiKey;
            caigApiKeyTemp = caigApiKey;
            // Set minimum window size
            minSize = new Vector2(500, 700);
            // Restore persistent data
            sceneDescription = EditorPrefs.GetString(PrefKey_SceneDescription, "");
            string suggestionsJson = EditorPrefs.GetString(PrefKey_InteractionSuggestions, "");
            if (!string.IsNullOrEmpty(suggestionsJson))
            {
                try
                {
                    interactionSuggestions = JsonUtility.FromJson<SerializableDict>(suggestionsJson)?.ToDictionary();
                }
                catch { interactionSuggestions = null; }
            }
            userInteractionInput = EditorPrefs.GetString(PrefKey_UserInteractionInput, "");
            sentenceToInteractionResult = EditorPrefs.GetString(PrefKey_SentenceToInteractionResult, "");
            lastGeneratedScriptPath = EditorPrefs.GetString(PrefKey_LastGeneratedScriptPath, "");
            lastGeneratedClassName = EditorPrefs.GetString(PrefKey_LastGeneratedClassName, "");
            lastSuggestedObjectNames = EditorPrefs.GetString(PrefKey_LastSuggestedObjectNames, "");
            // Restore foundSuggestedObjects from names
            foundSuggestedObjects = new List<GameObject>();
            string foundNames = EditorPrefs.GetString(PrefKey_FoundSuggestedObjects, "");
            if (!string.IsNullOrEmpty(foundNames))
            {
                foreach (var n in foundNames.Split(','))
                {
                    var obj = GameObject.Find(n.Trim());
                    if (obj != null) foundSuggestedObjects.Add(obj);
                }
            }
        }

        private void OnDisable()
        {
            SavePersistentData();
        }

        private void SavePersistentData()
        {
            EditorPrefs.SetString(PrefKey_SceneDescription, sceneDescription ?? "");
            if (interactionSuggestions != null)
            {
                var serializable = new SerializableDict(interactionSuggestions);
                string json = JsonUtility.ToJson(serializable);
                EditorPrefs.SetString(PrefKey_InteractionSuggestions, json);
            }
            else
            {
                EditorPrefs.SetString(PrefKey_InteractionSuggestions, "");
            }
            EditorPrefs.SetString(PrefKey_UserInteractionInput, userInteractionInput ?? "");
            EditorPrefs.SetString(PrefKey_SentenceToInteractionResult, sentenceToInteractionResult ?? "");
            EditorPrefs.SetString(PrefKey_LastGeneratedScriptPath, lastGeneratedScriptPath ?? "");
            EditorPrefs.SetString(PrefKey_LastGeneratedClassName, lastGeneratedClassName ?? "");
            EditorPrefs.SetString(PrefKey_LastSuggestedObjectNames, lastSuggestedObjectNames ?? "");
            // Save foundSuggestedObjects as comma-separated names
            if (foundSuggestedObjects != null && foundSuggestedObjects.Count > 0)
            {
                string names = string.Join(",", foundSuggestedObjects.Select(o => o != null ? o.name : ""));
                EditorPrefs.SetString(PrefKey_FoundSuggestedObjects, names);
            }
            else
            {
                EditorPrefs.SetString(PrefKey_FoundSuggestedObjects, "");
            }
        }

        // Serializable dictionary for JSON
        [Serializable]
        private class SerializableDict
        {
            public List<string> keys = new List<string>();
            public List<string[]> values = new List<string[]>();
            public SerializableDict() { }
            public SerializableDict(Dictionary<string, string[]> dict)
            {
                foreach (var kv in dict)
                {
                    keys.Add(kv.Key);
                    values.Add(kv.Value);
                }
            }
            public Dictionary<string, string[]> ToDictionary()
            {
                var dict = new Dictionary<string, string[]>();
                for (int i = 0; i < keys.Count; i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }

        private void OnGUI()
        {
            if (styles != null && !styles.IsInitialized)
            {
                styles.Initialize();
            }
            float contentWidth = position.width - 40f;
            float buttonWidth = Mathf.Min(250f, contentWidth * 0.7f);
            // Internal tab UI
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentInteractionTab == InteractionTab.Tools, "Tools", EditorStyles.toolbarButton))
                currentInteractionTab = InteractionTab.Tools;
            if (GUILayout.Toggle(currentInteractionTab == InteractionTab.Settings, "Settings", EditorStyles.toolbarButton))
                currentInteractionTab = InteractionTab.Settings;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            switch (currentInteractionTab)
            {
                case InteractionTab.Tools:
                    DrawInteractionToolsTab(contentWidth, buttonWidth);
                    break;
                case InteractionTab.Settings:
                    DrawSettingsTab();
                    break;
            }
        }

        // Draws the main interaction tools tab UI
        private void DrawInteractionToolsTab(float contentWidth, float buttonWidth)
        {
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Space(20);
            DrawDescriptionSection(buttonWidth);
            GUILayout.Space(20);
            DrawSentenceToInteractionSection(buttonWidth);
            GUILayout.Space(20);
            DrawAddPlayerSection(buttonWidth);
            GUILayout.Space(20);
            DrawAnimationSection();
            if (isGeneratingDescription)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Generating... Please wait.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // Draws the scene description and analysis section
        private void DrawDescriptionSection(float buttonWidth)
        {
            // Section icon and header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow"), GUILayout.Width(22), GUILayout.Height(22));
            GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionTitleStyle.fontSize = 14;
            sectionTitleStyle.normal.textColor = SectionTitleColor;
            GUIStyle descLabelStyle = new GUIStyle(EditorStyles.label);
            descLabelStyle.normal.textColor = DescriptionTextColor;
            EditorGUILayout.LabelField("Suggestion", sectionTitleStyle);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Suggest appropriate interactions for selected objects based on the scene context.", descLabelStyle);
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // Suggest Interactions button with lighter blue-gray color
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;
            GUI.backgroundColor = ButtonBgColor;
            GUI.contentColor = ButtonTextColor;
            EditorGUI.BeginDisabledGroup(isGeneratingDescription);
            if (GUILayout.Button(new GUIContent("Suggest Interactions", "Suggest appropriate interactions for selected objects based on the scene context."), GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                try
                {
                    AnalyzeSceneAndSuggestInteractions();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in AnalyzeSceneAndSuggestInteractions: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", $"Error in AnalyzeSceneAndSuggestInteractions: {ex.Message}", "OK");
                }
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(sceneDescription))
            {
                GUILayout.Space(2);
                // Thin divider
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
                GUILayout.Space(2);
            }
            if (interactionSuggestions != null && interactionSuggestions.Count > 0)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Interaction Suggestions:", EditorStyles.boldLabel);
                bool hasAnyValid = false;
                foreach (var kvp in interactionSuggestions)
                {
                    string objName = kvp.Key;
                    EditorGUILayout.LabelField($"- {objName}", EditorStyles.miniBoldLabel);
                    bool validFound = false;
                    foreach (var suggestion in kvp.Value)
                    {
                        string cleanSuggestion = suggestion;
                        if (!string.IsNullOrWhiteSpace(cleanSuggestion) && cleanSuggestion != "NONE" && cleanSuggestion != "ERROR" && !cleanSuggestion.Contains("No valid suggestions found"))
                        {
                            // Remove bold markdown (**) from suggestion
                            cleanSuggestion = Regex.Replace(cleanSuggestion, @"\*\*(.*?)\*\*", "$1");
                            // Show suggestion as a word-wrapped label with max width
                            EditorGUILayout.LabelField(cleanSuggestion, EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(400));
                            // Generate button centered
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            prevBg = GUI.backgroundColor;
                            prevContent = GUI.contentColor;
                            GUI.backgroundColor = ButtonBgColor;
                            GUI.contentColor = ButtonTextColor;
                            if (GUILayout.Button(new GUIContent("Generate", "Generate this suggestion and create the script."), GUILayout.Width(80)))
                            {
                                // Save the last generated suggestion for this object
                                lastGeneratedSuggestionPerObject[objName] = cleanSuggestion;
                                userInteractionInput = cleanSuggestion;
                                SavePersistentData();
                                GenerateSentenceToInteraction();
                            }
                            GUI.backgroundColor = prevBg;
                            GUI.contentColor = prevContent;
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            validFound = true;
                            hasAnyValid = true;
                        }
                    }
                    if (!validFound)
                    {
                        // Only show HelpBox (icon is included automatically)
                        EditorGUILayout.HelpBox("No valid suggestions found for this object. Please provide a description and desired interaction for this object below.", MessageType.Warning);
                        // Show user input area for custom description
                        if (!userObjectInput.ContainsKey(objName)) userObjectInput[objName] = "";
                        userObjectInput[objName] = EditorGUILayout.TextArea(userObjectInput[objName], GUILayout.Height(40), GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField("This may help if the object is not mentioned in the scene description or is not relevant to the current scene context.", EditorStyles.wordWrappedMiniLabel);
                    }
                }
                if (!hasAnyValid)
                {
                    // Do not show duplicate message; warning above is sufficient
                }
                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                // Regenerate button with blue-gray color
                prevBg = GUI.backgroundColor;
                prevContent = GUI.contentColor;
                GUI.backgroundColor = ButtonBgColor;
                GUI.contentColor = ButtonTextColor;
                EditorGUI.BeginDisabledGroup(isGeneratingDescription);
                if (GUILayout.Button(new GUIContent("Regenerate Interaction Suggestions", "Generate interaction suggestions for the currently selected objects based on the existing scene description and your input."), GUILayout.Width(buttonWidth), GUILayout.Height(22)))
                {
                    try
                    {
                        RegenerateInteractionSuggestionsOnly();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}");
                        EditorUtility.DisplayDialog("Error", $"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}", "OK");
                    }
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = prevBg;
                GUI.contentColor = prevContent;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (!string.IsNullOrEmpty(sceneDescription))
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("No interaction suggestions available.", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                prevBg = GUI.backgroundColor;
                prevContent = GUI.contentColor;
                GUI.backgroundColor = ButtonBgColor;
                GUI.contentColor = ButtonTextColor;
                EditorGUI.BeginDisabledGroup(isGeneratingDescription);
                if (GUILayout.Button(new GUIContent("Regenerate Interaction Suggestions", "Generate interaction suggestions for the currently selected objects based on the existing scene description and your input."), GUILayout.Width(buttonWidth), GUILayout.Height(22)))
                {
                    try
                    {
                        RegenerateInteractionSuggestionsOnly();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}");
                        EditorUtility.DisplayDialog("Error", $"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}", "OK");
                    }
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = prevBg;
                GUI.contentColor = prevContent;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
        }

        // Draws the sentence-to-interaction section
        private void DrawSentenceToInteractionSection(float buttonWidth)
        {
            // Section icon and header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow"), GUILayout.Width(22), GUILayout.Height(22));
            GUIStyle sectionTitleStyle2 = new GUIStyle(EditorStyles.boldLabel);
            sectionTitleStyle2.fontSize = 14;
            sectionTitleStyle2.normal.textColor = SectionTitleColor;
            EditorGUILayout.LabelField("Sentence-to-Interaction", sectionTitleStyle2);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Describe the interaction you want to create as a single sentence", EditorStyles.miniLabel);
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            // TextArea with placeholder
            var wordWrapStyle = new GUIStyle(EditorStyles.textField) { wordWrap = true };
            wordWrapStyle.normal.textColor = InputTextColor;
            if (string.IsNullOrEmpty(userInteractionInput))
            {
                EditorGUILayout.LabelField("e.g. Make the object spin when clicked.", EditorStyles.wordWrappedMiniLabel);
            }
            userInteractionInput = EditorGUILayout.TextArea(userInteractionInput, wordWrapStyle, GUILayout.Height(60), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(800));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // Generate button with lighter blue-gray color
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;
            GUI.backgroundColor = ButtonBgColor;
            GUI.contentColor = ButtonTextColor;
            if (GUILayout.Button(new GUIContent("Generate Interaction", "Generate a Unity C# script for the described interaction."), GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                try
                {
                    GenerateSentenceToInteraction();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in GenerateSentenceToInteraction: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", $"Error in GenerateSentenceToInteraction: {ex.Message}", "OK");
                }
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(lastGeneratedClassName) && lastGeneratedClassName != "No code block found.")
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                // Assign button with lighter blue-gray color
                prevBg = GUI.backgroundColor;
                prevContent = GUI.contentColor;
                GUI.backgroundColor = ButtonBgColor;
                GUI.contentColor = ButtonTextColor;
                if (GUILayout.Button(new GUIContent("Assign Script to Selected Object(s)", "Assign the generated script to the selected objects."), GUILayout.Width(buttonWidth), GUILayout.Height(28)))
                {
                    AssignScriptToSelectedObjects();
                }
                GUI.backgroundColor = prevBg;
                GUI.contentColor = prevContent;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            if (!string.IsNullOrEmpty(sentenceToInteractionResult))
            {
                if (!string.IsNullOrEmpty(lastGeneratedScriptPath))
                {
                    EditorGUILayout.HelpBox($"Generated script saved to: {lastGeneratedScriptPath}", MessageType.Info);
                }
                if (!string.IsNullOrEmpty(lastSuggestedObjectNames))
                {
                    EditorGUILayout.LabelField("Suggested Object Name(s):", EditorStyles.boldLabel);
                    EditorGUILayout.TextField(lastSuggestedObjectNames);
                }
                if (foundSuggestedObjects != null && foundSuggestedObjects.Count > 0)
                {
                    EditorGUILayout.LabelField("Found in Scene:", EditorStyles.boldLabel);
                    foreach (var obj in foundSuggestedObjects)
                    {
                        EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    }
                }
            }
        }

        // Draws the animation section
        private void DrawAnimationSection()
        {
            try
            {
                if (animationUI == null)
                {
                    EditorGUILayout.HelpBox("AnimationUI is not initialized.", MessageType.Error);
                    return;
                }
                // Null check for AnimationSettings.Instance
                if (OojuInteractionPlugin.AnimationSettings.Instance == null)
                {
                    EditorGUILayout.HelpBox("AnimationSettings is not initialized.", MessageType.Error);
                    return;
                }
                animationUI.DrawAnimationUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in DrawAnimationSection: {ex.Message}");
                EditorGUILayout.HelpBox($"Error in DrawAnimationSection: {ex.Message}", MessageType.Error);
            }
        }

        private bool CheckLLMApiKeyAndShowError()
        {
            var settings = OISettings.Instance;
            string errorMsg = null;
            switch (settings.SelectedLLMType)
            {
                case "OpenAI":
                    if (string.IsNullOrEmpty(settings.ApiKey))
                        errorMsg = "OpenAI API Key is not set. Please set it in the Settings tab.";
                    break;
                case "Claude":
                    if (string.IsNullOrEmpty(settings.ClaudeApiKey))
                        errorMsg = "Claude API Key is not set. Please set it in the Settings tab.";
                    break;
                case "Gemini":
                    if (string.IsNullOrEmpty(settings.GeminiApiKey))
                        errorMsg = "Gemini API Key is not set. Please set it in the Settings tab.";
                    break;
            }
            if (errorMsg != null)
            {
                EditorUtility.DisplayDialog("Error", errorMsg, "OK");
                return false;
            }
            return true;
        }

        // New method: Analyze scene and suggest interactions in one step
        private async void AnalyzeSceneAndSuggestInteractions()
        {
            if (!CheckLLMApiKeyAndShowError()) return;
            try
            {
                isGeneratingDescription = true;
                EditorUtility.DisplayProgressBar("Analyzing Scene & Generating Suggestions", "Please wait while the scene is being analyzed and suggestions are generated...", 0.5f);
                sceneDescription = await OIDescriptor.GenerateSceneDescription();
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.ClearProgressBar();
                    isGeneratingDescription = false;
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    interactionSuggestions = null;
                    SavePersistentData();
                    return;
                }
                string extraPrompt = "Prioritize interactions that can be implemented with Unity scripts only, and avoid suggestions that require the user to prepare extra resources such as sound or animation files.";
                var suggestions = new Dictionary<string, string[]>();
                foreach (var obj in selectedObjects)
                {
                    string objName = obj.name;
                    string prompt = $"Scene Description:\n{sceneDescription}\n\nObject Name: {objName}\n\nSuggest 3 realistic, Unity-implementable interactions for this object, considering the scene context. Only suggest interactions that make sense for this object in Unity. {extraPrompt} If the object is not interactive, respond ONLY with the word: NONE.";
                    string result = await OIDescriptor.RequestLLMInteraction(prompt);
                    string[] arr = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        arr[i] = Regex.Replace(arr[i], @"^\s*(\d+\.|\*|-)\s*", "").Trim();
                    }
                    suggestions[objName] = arr.Length > 0 ? arr : new[] { "NONE" };
                }
                interactionSuggestions = suggestions;
                SavePersistentData();
                EditorUtility.DisplayDialog("Analyze & Suggest", "Scene analyzed and interaction suggestions generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in AnalyzeSceneAndSuggestInteractions: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error in AnalyzeSceneAndSuggestInteractions: {ex.Message}", "OK");
                sceneDescription = $"Error: {ex.Message}";
                interactionSuggestions = new Dictionary<string, string[]> { { "Error", new string[] { ex.Message } } };
                SavePersistentData();
            }
            finally
            {
                isGeneratingDescription = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        // Optimized async method for generating sentence-to-interaction
        private async void GenerateSentenceToInteraction()
        {
            if (!CheckLLMApiKeyAndShowError()) return;
            try
            {
                if (string.IsNullOrEmpty(sceneDescription))
                {
                    EditorUtility.DisplayDialog("Error", "Please generate a scene description first.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(userInteractionInput))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter an interaction description.", "OK");
                    return;
                }
                isGeneratingDescription = true;
                EditorUtility.DisplayProgressBar("Generating Interaction", "Please wait while the interaction is being generated...", 0.5f);
                string prompt = $"Scene Description:\n{sceneDescription}\n\nUser Request (Sentence):\n{userInteractionInput}\n\n1. Generate a Unity C# script for this interaction.\n2. The script must define only one class, and the class name must be unique (for example, append a timestamp or a random string).\n3. The generated class must inherit from UnityEngine.MonoBehaviour.\n4. Do not define the same class or method more than once.\n5. If you need to implement Update, Start, or other Unity methods, each should appear only once in the class.\n6. All comments in the script must be written in English.\n7. Output only the code block.\n8. Prioritize interactions that can be implemented with Unity scripts only, and avoid suggestions that require the user to prepare extra resources such as sound or animation files.";
                sentenceToInteractionResult = await OIDescriptor.RequestLLMInteraction(prompt);
                string code = ExtractCodeBlock(sentenceToInteractionResult);
                // Safely insert : MonoBehaviour after the first class declaration
                if (!string.IsNullOrEmpty(code) && !code.Contains(": MonoBehaviour"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(code, @"class\s+([A-Za-z_][A-Za-z0-9_]*)");
                    if (match.Success)
                    {
                        string original = match.Value;
                        string replacement = original + " : MonoBehaviour";
                        code = code.Replace(original, replacement);
                    }
                }
                if (!string.IsNullOrEmpty(code))
                {
                    lastGeneratedScriptPath = SaveGeneratedScript(code);
                    lastGeneratedClassName = ExtractClassNameFromCode(code);
                    UnityEditor.AssetDatabase.Refresh();
                }
                else
                {
                    lastGeneratedScriptPath = "No code block found.";
                    lastGeneratedClassName = "";
                }
                lastSuggestedObjectNames = ExtractSuggestedObjectNames(sentenceToInteractionResult);
                foundSuggestedObjects = FindObjectsInSceneByNames(lastSuggestedObjectNames);
                SavePersistentData();
                EditorUtility.DisplayDialog("Sentence-to-Interaction", "Interaction generated successfully. You can now assign the script to the selected object(s).", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating Sentence-to-Interaction: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error generating interaction: {ex.Message}", "OK");
                sentenceToInteractionResult = $"Error: {ex.Message}";
                lastGeneratedScriptPath = "";
                lastGeneratedClassName = "";
                lastSuggestedObjectNames = "";
                foundSuggestedObjects.Clear();
                SavePersistentData();
            }
            finally
            {
                isGeneratingDescription = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        // Button handler to assign the generated script to selected objects
        private void AssignScriptToSelectedObjects()
        {
            if (string.IsNullOrEmpty(lastGeneratedClassName))
            {
                EditorUtility.DisplayDialog("Assign Script", "No generated script to assign.", "OK");
                return;
            }
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Assign Script", "No objects selected.", "OK");
                return;
            }
            var scriptType = GetTypeByName(lastGeneratedClassName);
            if (scriptType == null)
            {
                EditorUtility.DisplayDialog("Assign Script", $"Could not find compiled script type: {lastGeneratedClassName}. Please recompile scripts and try again.", "OK");
                return;
            }
            int addedCount = 0;
            foreach (var obj in selectedObjects)
            {
                if (obj != null && obj.GetComponent(scriptType) == null)
                {
                    Undo.AddComponent(obj, scriptType);
                    addedCount++;
                }
            }
            EditorUtility.DisplayDialog("Assign Script", $"Script assigned to {addedCount} object(s).", "OK");
        }

        // Helper: Find a Type by class name in loaded assemblies
        private Type GetTypeByName(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(className);
                if (type != null)
                    return type;
                // Try with namespace wildcard
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                if (type != null)
                    return type;
            }
            return null;
        }

        // Extracts the first C# code block from the LLM result
        private string ExtractCodeBlock(string result)
        {
            int start = result.IndexOf("```csharp");
            if (start == -1) start = result.IndexOf("```cs");
            if (start == -1) start = result.IndexOf("```");
            if (start == -1) return null;

            int codeStart = result.IndexOf('\n', start);
            int end = result.IndexOf("```", codeStart + 1);
            if (codeStart == -1 || end == -1) return null;

            return result.Substring(codeStart + 1, end - codeStart - 1).Trim();
        }

        // Extracts suggested object names from the LLM result (simple heuristic)
        private string ExtractSuggestedObjectNames(string result)
        {
            // Look for a line like 'Object(s): ...' or 'Object: ...'
            using (StringReader reader = new StringReader(result))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Object(s):", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Object:", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = line.IndexOf(":");
                        if (idx != -1 && idx + 1 < line.Length)
                        {
                            return line.Substring(idx + 1).Trim();
                        }
                    }
                }
            }
            return string.Empty;
        }

        // Finds GameObjects in the scene by comma-separated names
        private List<GameObject> FindObjectsInSceneByNames(string names)
        {
            List<GameObject> found = new List<GameObject>();
            if (string.IsNullOrEmpty(names)) return found;
            string[] split = names.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawName in split)
            {
                string name = rawName.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                GameObject obj = GameObject.Find(name);
                if (obj != null && !found.Contains(obj))
                    found.Add(obj);
            }
            return found;
        }

        // Saves the generated script code to a new C# file in the project, returns the file path
        private string SaveGeneratedScript(string scriptCode, string className = null)
        {
            string directory = "Assets/OOJU/Interaction/Generated";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Generate a file name from the interaction sentence if not provided
            if (string.IsNullOrEmpty(className))
            {
                className = GenerateClassNameFromSentence(userInteractionInput);
            }

            // Add timestamp to class name to avoid duplicates
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            className = $"{className}_{timestamp}";

            // Replace the class name inside the script code as well
            scriptCode = ReplaceClassNameInScript(scriptCode, className);

            // Remove duplicate class definitions and duplicate Unity methods (Update, Start, etc.)
            scriptCode = RemoveDuplicateClassAndMethods(scriptCode, className);

            string filePath = Path.Combine(directory, $"{className}.cs");
            File.WriteAllText(filePath, scriptCode);
            AssetDatabase.Refresh();
            return filePath;
        }

        // Replaces the first class name in the script code with the given class name
        private string ReplaceClassNameInScript(string scriptCode, string newClassName)
        {
            if (string.IsNullOrEmpty(scriptCode) || string.IsNullOrEmpty(newClassName)) return scriptCode;
            // Find 'class <Name>' and replace with newClassName
            var regex = new System.Text.RegularExpressions.Regex(@"class\\s+([A-Za-z_][A-Za-z0-9_]*)");
            return regex.Replace(scriptCode, $"class {newClassName}", 1);
        }

        // Removes duplicate class definitions and duplicate Unity methods (Update, Start, etc.)
        private string RemoveDuplicateClassAndMethods(string scriptCode, string className)
        {
            // Remove duplicate class definitions
            var lines = scriptCode.Split(new[] { '\n' });
            var newLines = new List<string>();
            var methodSet = new HashSet<string>();
            bool insideClass = false;
            bool insideMethod = false;
            string currentMethod = null;
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                // Only keep the first class definition
                if (trimmed.StartsWith($"class {className}"))
                {
                    if (insideClass)
                        continue;
                    insideClass = true;
                }
                // Detect method signature
                if (insideClass && (trimmed.StartsWith("void ") || trimmed.StartsWith("public void ") || trimmed.StartsWith("private void ")))
                {
                    int paren = trimmed.IndexOf('(');
                    if (paren > 0)
                    {
                        string methodSig = trimmed.Substring(0, paren).Trim();
                        if (methodSet.Contains(methodSig))
                        {
                            insideMethod = true;
                            currentMethod = methodSig;
                            continue;
                        }
                        else
                        {
                            methodSet.Add(methodSig);
                        }
                    }
                }
                // Skip lines inside duplicate method
                if (insideMethod)
                {
                    if (trimmed == "}")
                    {
                        insideMethod = false;
                        currentMethod = null;
                    }
                    continue;
                }
                newLines.Add(line);
            }
            return string.Join("\n", newLines);
        }

        // Generates a valid C# class/file name from the interaction sentence
        private string GenerateClassNameFromSentence(string sentence)
        {
            if (string.IsNullOrEmpty(sentence)) return "GeneratedInteractionScript";
            // Remove non-alphanumeric characters, replace spaces with underscores, limit length
            string name = new string(sentence.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
            name = name.Trim().Replace(' ', '_');
            if (name.Length > 32) name = name.Substring(0, 32);
            if (string.IsNullOrEmpty(name)) name = "GeneratedInteractionScript";
            // Ensure it starts with a letter
            if (!char.IsLetter(name[0])) name = "Script_" + name;
            return name;
        }

        // Extract the class name from the generated code using regex
        private string ExtractClassNameFromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(code, @"class\s+([A-Za-z_][A-Za-z0-9_]*)");
            if (match.Success)
                return match.Groups[1].Value.Trim().TrimEnd('.');
            return null;
        }

        // Settings internal tab UI (minimal skeleton)
        private void DrawSettingsTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI Model Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            string[] llmTypes = new[] { "OpenAI", "Claude", "Gemini" };
            int selectedIdx = Array.IndexOf(llmTypes, OISettings.Instance.SelectedLLMType);
            if (selectedIdx < 0) selectedIdx = 0;
            selectedIdx = EditorGUILayout.Popup("Models", selectedIdx, llmTypes);
            OISettings.Instance.SelectedLLMType = llmTypes[selectedIdx];
            EditorGUILayout.Space();
            switch (OISettings.Instance.SelectedLLMType)
            {
                case "OpenAI":
                    OISettings.Instance.ApiKey = EditorGUILayout.PasswordField("OpenAI API Key", OISettings.Instance.ApiKey);
                    break;
                case "Claude":
                    OISettings.Instance.ClaudeApiKey = EditorGUILayout.PasswordField("Claude API Key", OISettings.Instance.ClaudeApiKey);
                    break;
                case "Gemini":
                    OISettings.Instance.GeminiApiKey = EditorGUILayout.PasswordField("Gemini API Key", OISettings.Instance.GeminiApiKey);
                    break;
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Save Settings"))
            {
                OISettings.Instance.SaveSettings();
                EditorUtility.SetDirty(OISettings.Instance);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Saved", "Settings have been saved.", "OK");
            }
        }

        // Scene Description이 이미 생성된 경우, 선택된 오브젝트에 대해 Interaction Suggestion만 다시 생성하는 함수
        private async void RegenerateInteractionSuggestionsOnly()
        {
            if (!CheckLLMApiKeyAndShowError()) return;
            try
            {
                isGeneratingDescription = true;
                EditorUtility.DisplayProgressBar("Generating Suggestions", "Please wait while suggestions are being generated...", 0.5f);
                if (string.IsNullOrEmpty(sceneDescription))
                {
                    EditorUtility.ClearProgressBar();
                    isGeneratingDescription = false;
                    EditorUtility.DisplayDialog("Error", "Scene description is not available.", "OK");
                    return;
                }
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.ClearProgressBar();
                    isGeneratingDescription = false;
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    interactionSuggestions = null;
                    SavePersistentData();
                    return;
                }
                // Add extra instruction to the prompt to avoid requiring extra resources
                string extraPrompt = "Prioritize interactions that can be implemented with Unity scripts only, and avoid suggestions that require the user to prepare extra resources such as sound or animation files.";
                Dictionary<string, string> customObjectDescriptions = new Dictionary<string, string>();
                foreach (var obj in selectedObjects)
                {
                    string objName = obj.name;
                    if (userObjectInput.ContainsKey(objName) && !string.IsNullOrWhiteSpace(userObjectInput[objName]))
                    {
                        customObjectDescriptions[objName] = userObjectInput[objName];
                    }
                }
                if (customObjectDescriptions.Count > 0)
                {
                    var suggestions = new Dictionary<string, string[]>();
                    foreach (var obj in selectedObjects)
                    {
                        string objName = obj.name;
                        string prompt;
                        if (customObjectDescriptions.ContainsKey(objName))
                        {
                            prompt = $"Scene Description:\n{sceneDescription}\n\nObject Name: {objName}\nUser Description: {customObjectDescriptions[objName]}\n\nSuggest 3 realistic, Unity-implementable interactions for this object, considering the user description and scene context. Only suggest interactions that make sense for this object in Unity. {extraPrompt} If the object is not interactive, respond ONLY with the word: NONE.";
                        }
                        else
                        {
                            prompt = $"Scene Description:\n{sceneDescription}\n\nObject Name: {objName}\n\nSuggest 3 realistic, Unity-implementable interactions for this object, considering the scene context. Only suggest interactions that make sense for this object in Unity. {extraPrompt} If the object is not interactive, respond ONLY with the word: NONE.";
                        }
                        string result = await OIDescriptor.RequestLLMInteraction(prompt);
                        string[] arr = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = Regex.Replace(arr[i], @"^\s*(\d+\.|\*|-)\s*", "").Trim();
                        }
                        suggestions[objName] = arr.Length > 0 ? arr : new[] { "NONE" };
                    }
                    interactionSuggestions = suggestions;
                }
                else
                {
                    // Use the extra prompt for all objects
                    var suggestions = new Dictionary<string, string[]>();
                    foreach (var obj in selectedObjects)
                    {
                        string objName = obj.name;
                        string prompt = $"Scene Description:\n{sceneDescription}\n\nObject Name: {objName}\n\nSuggest 3 realistic, Unity-implementable interactions for this object, considering the scene context. Only suggest interactions that make sense for this object in Unity. {extraPrompt} If the object is not interactive, respond ONLY with the word: NONE.";
                        string result = await OIDescriptor.RequestLLMInteraction(prompt);
                        string[] arr = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = Regex.Replace(arr[i], @"^\s*(\d+\.|\*|-)\s*", "").Trim();
                        }
                        suggestions[objName] = arr.Length > 0 ? arr : new[] { "NONE" };
                    }
                    interactionSuggestions = suggestions;
                }
                SavePersistentData();
                EditorUtility.DisplayDialog("Interaction Suggestions", "Suggestions generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error in RegenerateInteractionSuggestionsOnly: {ex.Message}", "OK");
                interactionSuggestions = new Dictionary<string, string[]> { { "Error", new string[] { ex.Message } } };
                SavePersistentData();
            }
            finally
            {
                isGeneratingDescription = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        // Draws the Add Player section (UI + logic)
        private void DrawAddPlayerSection(float buttonWidth)
        {
            // Section icon and header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("Avatar Icon"), GUILayout.Width(22), GUILayout.Height(22));
            GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionTitleStyle.fontSize = 14;
            sectionTitleStyle.normal.textColor = SectionTitleColor;
            EditorGUILayout.LabelField("Add Player", sectionTitleStyle);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Add a player controller to your scene.", EditorStyles.miniLabel);
            GUILayout.Space(8);
            // Add First-person Player button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;
            GUI.backgroundColor = ButtonBgColor;
            GUI.contentColor = ButtonTextColor;
            if (GUILayout.Button(new GUIContent("Add First-person Player", "Add a first-person player controller to the scene."), GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                AddFirstPersonPlayerToScene();
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(16);
            // Add Ground button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            prevBg = GUI.backgroundColor;
            prevContent = GUI.contentColor;
            GUI.backgroundColor = ButtonBgColor;
            GUI.contentColor = ButtonTextColor;
            if (GUILayout.Button(new GUIContent("Add Ground", "Add a large ground plane (cube) at y=0."), GUILayout.Width(buttonWidth), GUILayout.Height(28)))
            {
                AddGroundToScene();
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
            // Set Selected as Ground button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            prevBg = GUI.backgroundColor;
            prevContent = GUI.contentColor;
            GUI.backgroundColor = ButtonBgColor;
            GUI.contentColor = ButtonTextColor;
            if (GUILayout.Button(new GUIContent("Set Selected as Ground", "Add a MeshCollider to the selected object(s) and set their layer to Default."), GUILayout.Width(buttonWidth), GUILayout.Height(28)))
            {
                SetSelectedAsGround();
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Adds a large ground cube at y=0
        private void AddGroundToScene()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(30, 1, 30);
            ground.layer = LayerMask.NameToLayer("Default");
            // Ensure it has a BoxCollider
            BoxCollider col = ground.GetComponent<BoxCollider>();
            if (col == null) ground.AddComponent<BoxCollider>();
            Selection.activeGameObject = ground;
            EditorGUIUtility.PingObject(ground);
            EditorUtility.DisplayDialog("Ground Added", "A large ground cube has been added at y=0.", "OK");
        }

        // Adds a MeshCollider to selected objects and sets their layer to Default
        private void SetSelectedAsGround()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one object in the scene.", "OK");
                return;
            }
            int count = 0;
            foreach (var obj in selected)
            {
                if (obj == null) continue;
                if (obj.GetComponent<Collider>() == null)
                    obj.AddComponent<MeshCollider>();
                obj.layer = LayerMask.NameToLayer("Default");
                count++;
            }
            EditorUtility.DisplayDialog("Set as Ground", $"MeshCollider added and layer set to Default for {count} object(s).", "OK");
        }

        // Adds a simple first-person player controller to the scene (WASD/arrow keys + Space to jump)
        private void AddFirstPersonPlayerToScene()
        {
            // Check if a FirstPersonPlayer script exists in the project
            var scriptAsset = AssetDatabase.FindAssets("FirstPersonPlayer t:Script");
            string scriptPath = null;
            string directory = "Assets/OOJU/Interaction/Player";
            if (scriptAsset.Length > 0)
            {
                scriptPath = AssetDatabase.GUIDToAssetPath(scriptAsset[0]);
            }
            else
            {
                // If not found, create a simple FirstPersonPlayer script in Player folder
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                scriptPath = System.IO.Path.Combine(directory, "FirstPersonPlayer.cs");
                System.IO.File.WriteAllText(scriptPath, GetFirstPersonPlayerScriptCode());
                AssetDatabase.Refresh();
            }

            // Check if the script type is available (compiled)
            var scriptType = GetTypeByName("FirstPersonPlayer");
            if (scriptType == null)
            {
                EditorUtility.DisplayDialog("Script Compile Needed", "FirstPersonPlayer.cs script was created or updated. Please wait for Unity to compile, then try again.", "OK");
                return;
            }

            // Prevent duplicate player in the scene
            if (GameObject.FindObjectOfType(scriptType) != null)
            {
                EditorUtility.DisplayDialog("Already Exists", "A FirstPersonPlayer object already exists in the scene.", "OK");
                return;
            }

            // Create a new GameObject for the player
            GameObject player = new GameObject("FirstPersonPlayer");
            // Add CharacterController if available
            var charCtrlType = typeof(CharacterController);
            if (charCtrlType != null)
                player.AddComponent<CharacterController>();
            // Add the FirstPersonPlayer script
            player.AddComponent(scriptType);
            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(player);
            EditorUtility.DisplayDialog("First-person Player", "First-person player has been added to the scene!\nUse WASD or arrow keys and Space to jump in Play mode.", "OK");
        }

        // Returns the code for a simple FirstPersonPlayer script (WASD/arrow keys + Space to jump)
        private string GetFirstPersonPlayerScriptCode()
        {
            return "using UnityEngine;\n" +
                   "// Simple first-person player controller (WASD/arrow keys + Space to jump)\n" +
                   "public class FirstPersonPlayer : MonoBehaviour\n" +
                   "{\n" +
                   "    public float speed = 5f;\n" +
                   "    public float mouseSensitivity = 2f;\n" +
                   "    public float jumpHeight = 2f;\n" +
                   "    public float gravity = -9.81f;\n" +
                   "    private float rotationY = 0f;\n" +
                   "    private CharacterController controller;\n" +
                   "    private Vector3 velocity;\n" +
                   "    private bool isGrounded;\n" +
                   "    void Start()\n" +
                   "    {\n" +
                   "        controller = GetComponent<CharacterController>();\n" +
                   "        // Add a camera if not present\n" +
                   "        if (GetComponentInChildren<Camera>() == null)\n" +
                   "        {\n" +
                   "            GameObject camObj = new GameObject(\"PlayerCamera\");\n" +
                   "            camObj.transform.SetParent(transform);\n" +
                   "            camObj.transform.localPosition = new Vector3(0, 1.6f, 0);\n" +
                   "            camObj.AddComponent<Camera>();\n" +
                   "        }\n" +
                   "    }\n" +
                   "    void Update()\n" +
                   "    {\n" +
                   "        // Move\n" +
                   "        float h = Input.GetAxis(\"Horizontal\");\n" +
                   "        float v = Input.GetAxis(\"Vertical\");\n" +
                   "        Vector3 move = transform.right * h + transform.forward * v;\n" +
                   "        if (controller != null)\n" +
                   "            controller.Move(move * speed * Time.deltaTime);\n" +
                   "        else\n" +
                   "            transform.position += move * speed * Time.deltaTime;\n" +
                   "        // Mouse look (only in play mode)\n" +
                   "        if (Application.isPlaying)\n" +
                   "        {\n" +
                   "            float mouseX = Input.GetAxis(\"Mouse X\") * mouseSensitivity;\n" +
                   "            float mouseY = Input.GetAxis(\"Mouse Y\") * mouseSensitivity;\n" +
                   "            transform.Rotate(0, mouseX, 0);\n" +
                   "            rotationY -= mouseY;\n" +
                   "            rotationY = Mathf.Clamp(rotationY, -90f, 90f);\n" +
                   "            Camera cam = GetComponentInChildren<Camera>();\n" +
                   "            if (cam)\n" +
                   "                cam.transform.localEulerAngles = new Vector3(rotationY, 0, 0);\n" +
                   "        }\n" +
                   "        // Jump & Gravity\n" +
                   "        if (controller != null)\n" +
                   "        {\n" +
                   "            isGrounded = controller.isGrounded;\n" +
                   "            if (isGrounded && velocity.y < 0)\n" +
                   "                velocity.y = -2f;\n" +
                   "            if (isGrounded && Input.GetKeyDown(KeyCode.Space))\n" +
                   "                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);\n" +
                   "            velocity.y += gravity * Time.deltaTime;\n" +
                   "            controller.Move(velocity * Time.deltaTime);\n" +
                   "        }\n" +
                   "    }\n" +
                   "}";
        }
    }
} 