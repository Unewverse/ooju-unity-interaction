using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using OojuInteractionPlugin;
using System.IO;
using System.Linq;

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
        private Dictionary<string, string[]> interactionSuggestions = null;
        private CAIGAnalyzer.AnalysisData analysisData = null;
        private string caigApiKey = null;
        private string caigApiKeyTemp = null;
        private bool caigApiKeyShow = false;

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
        private string lastScriptSummary = "";
        private Vector2 lastScriptSummaryScroll = Vector2.zero;

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
                    DrawCAIGSettingsInnerTab();
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Description & Analysis", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Generate descriptions and interaction suggestions", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(isGeneratingDescription);
            if (GUILayout.Button(new GUIContent("Generate Scene Description", "Analyze and describe the current scene"), GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                try
                {
                    GenerateDescriptionInternal();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in GenerateDescriptionInternal: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", $"Error in GenerateDescriptionInternal: {ex.Message}", "OK");
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(sceneDescription))
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Current Scene Description:", EditorStyles.boldLabel);
                descriptionScrollPosition = EditorGUILayout.BeginScrollView(descriptionScrollPosition, GUILayout.Height(100), GUILayout.ExpandWidth(true));
                EditorGUILayout.TextArea(sceneDescription, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndScrollView();
            }
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // Draws the sentence-to-interaction section
        private void DrawSentenceToInteractionSection(float buttonWidth)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Sentence-to-Interaction", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Describe the interaction you want to create as a single sentence", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            userInteractionInput = EditorGUILayout.TextArea(userInteractionInput, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Interaction", GUILayout.Width(buttonWidth), GUILayout.Height(30)))
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
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(sentenceToInteractionResult))
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("How to Apply:", EditorStyles.boldLabel);
                lastScriptSummaryScroll = EditorGUILayout.BeginScrollView(lastScriptSummaryScroll, GUILayout.Height(100), GUILayout.ExpandWidth(true));
                EditorGUILayout.TextArea(
                    "1. The generated script is saved in: Assets/OOJU/Interaction/Generated/\n" +
                    "2. In the Unity Editor, select the GameObject you want to apply the script to.\n" +
                    "3. In the Inspector window, click 'Add Component' and search for the script by name, or drag and drop the script from the Project window onto the GameObject.\n" +
                    "4. If the script requires a target object (e.g., another GameObject), assign it by dragging the desired object from the Hierarchy to the corresponding field in the Inspector.",
                    EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndScrollView();
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
            EditorGUILayout.EndVertical();
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

        // Optimized async method for generating scene description
        private async void GenerateDescriptionInternal()
        {
            try
            {
                isGeneratingDescription = true;
                EditorUtility.DisplayProgressBar("Generating Scene Description", "Please wait while the scene is being analyzed...", 0.5f);
                if (string.IsNullOrEmpty(OISettings.Instance.ApiKey))
                {
                    EditorUtility.ClearProgressBar();
                    isGeneratingDescription = false;
                    EditorUtility.DisplayDialog("Error", "OpenAI API Key is not set. Please set it in the Settings tab.", "OK");
                    return;
                }
                sceneDescription = await OIDescriptor.GenerateSceneDescription();
                interactionSuggestions = null;
                EditorUtility.DisplayDialog("Scene Description", "Scene description generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating scene description: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error generating scene description: {ex.Message}", "OK");
                sceneDescription = $"Error: {ex.Message}";
            }
            finally
            {
                isGeneratingDescription = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        // Optimized async method for generating suggestions
        private async void GenerateSuggestionsInternal()
        {
            try
            {
                if (string.IsNullOrEmpty(sceneDescription))
                {
                    EditorUtility.DisplayDialog("Error", "Please generate a scene description first.", "OK");
                    return;
                }
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    return;
                }
                isGeneratingDescription = true;
                EditorUtility.DisplayProgressBar("Generating Suggestions", "Please wait while suggestions are being generated...", 0.5f);
                interactionSuggestions = await OIDescriptor.GenerateInteractionSuggestions(sceneDescription, selectedObjects);
                EditorUtility.DisplayDialog("Interaction Suggestions", "Suggestions generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating interaction suggestions: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error generating interaction suggestions: {ex.Message}", "OK");
                interactionSuggestions = new Dictionary<string, string[]> { { "Error", new string[] { ex.Message } } };
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
            try
            {
                if (string.IsNullOrEmpty(OISettings.Instance.ApiKey))
                {
                    EditorUtility.DisplayDialog("Error", "OpenAI API Key is not set. Please set it in the Settings tab.", "OK");
                    return;
                }
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
                string prompt = $"Scene Description:\n{sceneDescription}\n\nUser Request (Sentence):\n{userInteractionInput}\n\n" +
                                "1. Generate a Unity C# script for this interaction.\n" +
                                "2. The script must define only one class, and the class name must be unique (for example, append a timestamp or a random string).\n" +
                                "3. Do not define the same class or method more than once.\n" +
                                "4. If you need to implement Update, Start, or other Unity methods, each should appear only once in the class.\n" +
                                "5. All comments in the script must be written in English.\n" +
                                "6. Output only the code block.";
                sentenceToInteractionResult = await OIDescriptor.RequestLLMInteraction(prompt);
                string code = ExtractCodeBlock(sentenceToInteractionResult);
                if (!string.IsNullOrEmpty(code))
                {
                    lastGeneratedScriptPath = SaveGeneratedScript(code);
                }
                else
                {
                    lastGeneratedScriptPath = "No code block found.";
                }
                lastSuggestedObjectNames = ExtractSuggestedObjectNames(sentenceToInteractionResult);
                foundSuggestedObjects = FindObjectsInSceneByNames(lastSuggestedObjectNames);
                EditorUtility.DisplayDialog("Sentence-to-Interaction", "Interaction generated successfully.\nScript saved to: " + lastGeneratedScriptPath, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating Sentence-to-Interaction: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error generating interaction: {ex.Message}", "OK");
                sentenceToInteractionResult = $"Error: {ex.Message}";
                lastGeneratedScriptPath = "";
                lastSuggestedObjectNames = "";
                foundSuggestedObjects.Clear();
            }
            finally
            {
                isGeneratingDescription = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
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

        // CAIG Settings internal tab UI (minimal skeleton)
        private void DrawCAIGSettingsInnerTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OpenAI API Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (caigApiKeyShow)
            {
                caigApiKeyTemp = EditorGUILayout.TextField("API Key", caigApiKeyTemp);
            }
            else
            {
                caigApiKeyTemp = EditorGUILayout.PasswordField("API Key", caigApiKeyTemp);
            }
            if (GUILayout.Button(caigApiKeyShow ? "Hide" : "Show", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                caigApiKeyShow = !caigApiKeyShow;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Save API Key"))
            {
                caigApiKey = caigApiKeyTemp;
                OISettings.Instance.ApiKey = caigApiKey;
                EditorUtility.SetDirty(OISettings.Instance);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Saved", "API Key has been saved.", "OK");
            }
        }
    }
} 