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

        // Animation state variables
        private AnimationType selectedAnimationType = AnimationType.None;
        private float hoverSpeed = 1f;
        private float hoverDistance = 0.1f;
        private float wobbleSpeed = 2f;
        private float wobbleAngle = 5f;
        private float spinSpeed = 90f;
        private float shakeDuration = 0.5f;
        private float shakeMagnitude = 0.1f;
        private float bounceSpeed = 1f;
        private float bounceHeight = 0.5f;
        private float squashRatio = 0.1f;

        private enum AnimationCategory { Independent, Relational }
        private AnimationCategory selectedCategory = AnimationCategory.Independent;
        private GameObject referenceObject = null;

        private enum RelationalAnimationType { Orbit, LookAt, Follow, MoveAlongPath, SnapToObject }
        private RelationalAnimationType selectedRelationalType = RelationalAnimationType.Orbit;
        private float orbitRadius = 2f;
        private float orbitSpeed = 1f;
        private float orbitDuration = 3f;
        private float lookAtSpeed = 5f;
        private float lookAtDuration = 2f;
        private float followSpeed = 2f;
        private float followStopDistance = 0.2f;
        private float followDuration = 3f;
        private List<GameObject> pathPoints = new List<GameObject>();
        private float pathMoveSpeed = 2f;
        private bool snapRotation = true;

        private enum InteractionTab { Tools, Settings }
        private InteractionTab currentInteractionTab = InteractionTab.Tools;

        private UIStyles styles;

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

        // Interaction Tools tab UI (minimal skeleton)
        private void DrawInteractionToolsTab(float contentWidth, float buttonWidth)
        {
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            // Description & Analysis section
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
                GenerateDescriptionInternal();
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

            GUILayout.Space(20);

            // Sentence-to-Interaction section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Sentence-to-Interaction", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Describe the interaction you want to create as a single sentence", EditorStyles.miniLabel);
            GUILayout.Space(10);
            // Text input area
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            userInteractionInput = EditorGUILayout.TextArea(userInteractionInput, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Interaction", GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                GenerateSentenceToInteraction();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(sentenceToInteractionResult))
            {
                GUILayout.Space(10);
                // Show script summary in a scrollable TextArea
                if (!string.IsNullOrEmpty(lastScriptSummary))
                {
                    EditorGUILayout.LabelField("Script Summary / How to Apply:", EditorStyles.boldLabel);
                    lastScriptSummaryScroll = EditorGUILayout.BeginScrollView(lastScriptSummaryScroll, GUILayout.Height(80), GUILayout.ExpandWidth(true));
                    EditorGUILayout.TextArea(lastScriptSummary, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndScrollView();
                }
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

            GUILayout.Space(20);

            // Interaction Suggestions section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Interaction Suggestions", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Get suggestions for selected objects", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(isGeneratingDescription);
            if (GUILayout.Button(new GUIContent("Generate Suggestions", "Get interaction suggestions for selected objects"), GUILayout.Width(buttonWidth), GUILayout.Height(30)))
            {
                GenerateSuggestionsInternal();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (interactionSuggestions != null && interactionSuggestions.Count > 0)
            {
                GUILayout.Space(10);
                foreach (var kvp in interactionSuggestions)
                {
                    EditorGUILayout.LabelField($"Object: {kvp.Key}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    if (kvp.Value != null)
                    {
                        foreach (var suggestion in kvp.Value)
                        {
                            EditorGUILayout.LabelField($"• {suggestion}", EditorStyles.wordWrappedLabel);
                        }
                    }
                    EditorGUI.indentLevel--;
                    GUILayout.Space(5);
                }
            }
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            // Animation section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Add animations to selected objects", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Type Category", EditorStyles.boldLabel);
            selectedCategory = (AnimationCategory)GUILayout.Toolbar((int)selectedCategory, new string[] { "Independent", "Relational" });
            GUILayout.Space(10);
            if (selectedCategory == AnimationCategory.Independent)
            {
                EditorGUILayout.HelpBox("Independent animations are applied to each object individually (e.g., Hover, Wobble, Spin, Shake, Bounce).", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Animation Type:", GUILayout.Width(100));
                selectedAnimationType = (AnimationType)EditorGUILayout.EnumPopup(selectedAnimationType);
                EditorGUILayout.EndHorizontal();
                if (selectedAnimationType != AnimationType.None)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Animation Parameters", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    switch (selectedAnimationType)
                    {
                        case AnimationType.Hover:
                            hoverSpeed = EditorGUILayout.FloatField("Hover Speed", hoverSpeed);
                            hoverDistance = EditorGUILayout.FloatField("Hover Distance", hoverDistance);
                            break;
                        case AnimationType.Wobble:
                            wobbleSpeed = EditorGUILayout.FloatField("Wobble Speed", wobbleSpeed);
                            wobbleAngle = EditorGUILayout.FloatField("Wobble Angle", wobbleAngle);
                            break;
                        case AnimationType.Scale:
                            // Scale parameters
                            break;
                    }
                    EditorGUI.indentLevel--;
                    GUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Apply Animation", GUILayout.Width(150), GUILayout.Height(30)))
                    {
                        var selectedObjects = Selection.gameObjects;
                        if (selectedObjects.Length == 0)
                        {
                            EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                        }
                        else
                        {
                            foreach (var obj in selectedObjects)
                            {
                                // Collider auto add
                                var collider = obj.GetComponent<Collider>();
                                if (collider == null)
                                {
                                    var meshFilter = obj.GetComponent<MeshFilter>();
                                    if (meshFilter != null && meshFilter.sharedMesh != null)
                                    {
                                        var meshCollider = obj.AddComponent<MeshCollider>();
                                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                                        meshCollider.convex = false;
                                    }
                                    else
                                    {
                                        obj.AddComponent<BoxCollider>();
                                    }
                                }
                                // Check for existing ObjectAutoAnimator
                                var existingAnimator = obj.GetComponent<ObjectAutoAnimator>();
                                if (existingAnimator != null)
                                {
                                    // Remove existing animator
                                    Undo.DestroyObjectImmediate(existingAnimator);
                                }

                                // Add new animator
                                var animator = Undo.AddComponent<ObjectAutoAnimator>(obj);
                                Undo.RecordObject(animator, "Set Animation");

                                switch (selectedAnimationType)
                                {
                                    case AnimationType.Hover:
                                        animator.SetAnimationType(selectedAnimationType);
                                        animator.hoverSpeed = hoverSpeed;
                                        animator.baseHoverDistance = hoverDistance;
                                        break;
                                    case AnimationType.Wobble:
                                        animator.SetAnimationType(selectedAnimationType);
                                        animator.wobbleSpeed = wobbleSpeed;
                                        animator.baseWobbleAngle = wobbleAngle;
                                        break;
                                    case AnimationType.Scale:
                                        animator.SetAnimationType(selectedAnimationType);
                                        break;
                                }

                                EditorUtility.SetDirty(animator);
                                if (Application.isPlaying)
                                {
                                    animator.StartAnimation();
                                }
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else if (selectedCategory == AnimationCategory.Relational)
            {
                EditorGUILayout.HelpBox("Relational animations involve a relationship with another object (e.g., orbiting around a reference object and returning).", MessageType.Info);
                EditorGUILayout.LabelField("Relational Animation Type", EditorStyles.boldLabel);
                selectedRelationalType = (RelationalAnimationType)EditorGUILayout.EnumPopup(selectedRelationalType);
                switch (selectedRelationalType)
                {
                    case RelationalAnimationType.Orbit:
                        referenceObject = (GameObject)EditorGUILayout.ObjectField("Reference Object", referenceObject, typeof(GameObject), true);
                        orbitRadius = EditorGUILayout.FloatField("Orbit Radius", orbitRadius);
                        orbitSpeed = EditorGUILayout.FloatField("Orbit Speed", orbitSpeed);
                        orbitDuration = EditorGUILayout.FloatField("Duration", orbitDuration);
                        break;
                    case RelationalAnimationType.LookAt:
                        referenceObject = (GameObject)EditorGUILayout.ObjectField("Target Object", referenceObject, typeof(GameObject), true);
                        lookAtSpeed = EditorGUILayout.FloatField("Look Speed", lookAtSpeed);
                        lookAtDuration = EditorGUILayout.FloatField("Duration", lookAtDuration);
                        break;
                    case RelationalAnimationType.Follow:
                        referenceObject = (GameObject)EditorGUILayout.ObjectField("Target Object", referenceObject, typeof(GameObject), true);
                        followSpeed = EditorGUILayout.FloatField("Follow Speed", followSpeed);
                        followStopDistance = EditorGUILayout.FloatField("Stop Distance", followStopDistance);
                        followDuration = EditorGUILayout.FloatField("Duration", followDuration);
                        break;
                    case RelationalAnimationType.MoveAlongPath:
                        EditorGUILayout.LabelField("Path Points (Add GameObjects):");
                        int removeIdx = -1;
                        for (int i = 0; i < pathPoints.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            pathPoints[i] = (GameObject)EditorGUILayout.ObjectField(pathPoints[i], typeof(GameObject), true);
                            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                                removeIdx = i;
                            EditorGUILayout.EndHorizontal();
                        }
                        if (removeIdx >= 0) pathPoints.RemoveAt(removeIdx);
                        if (GUILayout.Button("Add Path Point")) pathPoints.Add(null);
                        pathMoveSpeed = EditorGUILayout.FloatField("Move Speed", pathMoveSpeed);
                        break;
                    case RelationalAnimationType.SnapToObject:
                        referenceObject = (GameObject)EditorGUILayout.ObjectField("Reference Object", referenceObject, typeof(GameObject), true);
                        snapRotation = EditorGUILayout.Toggle("Snap Rotation", snapRotation);
                        break;
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("This animation makes the selected object move around the reference object and return to its original position.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply Relational Animation", GUILayout.Width(180), GUILayout.Height(30)))
                {
                    var selectedObjects = Selection.gameObjects;
                    if (selectedObjects.Length == 0)
                    {
                        EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    }
                    else
                    {
                        foreach (var obj in selectedObjects)
                        {
                            // Check for existing ObjectAutoAnimator
                            var existingAnimator = obj.GetComponent<ObjectAutoAnimator>();
                            if (existingAnimator != null)
                            {
                                // Remove existing animator
                                Undo.DestroyObjectImmediate(existingAnimator);
                            }

                            // Add new animator
                            var animator = Undo.AddComponent<ObjectAutoAnimator>(obj);
                            Undo.RecordObject(animator, "Set Relational Animation");

                            switch (selectedRelationalType)
                            {
                                case RelationalAnimationType.Orbit:
                                    if (referenceObject != null)
                                    {
                                        animator.relationalType = RelationalType.Orbit;
                                        animator.relationalReferenceObject = referenceObject.transform;
                                        animator.orbitRadius = orbitRadius;
                                        animator.orbitSpeed = orbitSpeed;
                                        animator.orbitDuration = orbitDuration;
                                        EditorUtility.SetDirty(animator);
                                        if (Application.isPlaying)
                                            animator.StartOrbit(referenceObject.transform, orbitRadius, orbitSpeed, orbitDuration);
                                    }
                                    break;
                                case RelationalAnimationType.LookAt:
                                    if (referenceObject != null)
                                    {
                                        animator.relationalType = RelationalType.LookAt;
                                        animator.relationalReferenceObject = referenceObject.transform;
                                        animator.lookAtSpeed = lookAtSpeed;
                                        animator.lookAtDuration = lookAtDuration;
                                        EditorUtility.SetDirty(animator);
                                        if (Application.isPlaying)
                                            animator.StartLookAt(referenceObject.transform, lookAtSpeed, lookAtDuration);
                                    }
                                    break;
                                case RelationalAnimationType.Follow:
                                    if (referenceObject != null)
                                    {
                                        animator.relationalType = RelationalType.Follow;
                                        animator.relationalReferenceObject = referenceObject.transform;
                                        animator.followSpeed = followSpeed;
                                        animator.followStopDistance = followStopDistance;
                                        animator.followDuration = followDuration;
                                        EditorUtility.SetDirty(animator);
                                        if (Application.isPlaying)
                                            animator.StartFollow(referenceObject.transform, followSpeed, followStopDistance, followDuration);
                                    }
                                    break;
                                case RelationalAnimationType.MoveAlongPath:
                                    var path = pathPoints.FindAll(p => p != null).ConvertAll(p => p.transform);
                                    if (path.Count > 0)
                                    {
                                        animator.relationalType = RelationalType.MoveAlongPath;
                                        animator.pathPoints = path;
                                        animator.pathMoveSpeed = pathMoveSpeed;
                                        animator.pathMoveDuration = orbitDuration;
                                        EditorUtility.SetDirty(animator);
                                        if (Application.isPlaying)
                                            animator.StartMoveAlongPath(path, pathMoveSpeed, orbitDuration);
                                    }
                                    break;
                                case RelationalAnimationType.SnapToObject:
                                    if (referenceObject != null)
                                    {
                                        animator.relationalType = RelationalType.SnapToObject;
                                        animator.relationalReferenceObject = referenceObject.transform;
                                        animator.snapRotation = snapRotation;
                                        EditorUtility.SetDirty(animator);
                                        if (Application.isPlaying)
                                            animator.SnapToObject(referenceObject.transform, snapRotation);
                                    }
                                    break;
                            }
                        }
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Animation", GUILayout.Width(150), GUILayout.Height(30)))
            {
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                }
                else
                {
                    int removedCount = 0;
                    foreach (var obj in selectedObjects)
                    {
                        var animator = obj.GetComponent<ObjectAutoAnimator>();
                        if (animator != null)
                        {
                            Undo.DestroyObjectImmediate(animator);
                            removedCount++;
                        }
                    }
                    if (removedCount > 0)
                    {
                        EditorUtility.DisplayDialog("Success", $"Removed animation from {removedCount} object(s).", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Info", "No ObjectAutoAnimator components found on selected objects.", "OK");
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (isGeneratingDescription)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Generating... Please wait.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
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

        private async void GenerateDescriptionInternal()
        {
            isGeneratingDescription = true;
            EditorUtility.DisplayProgressBar("Generating Scene Description", "Please wait while the scene is being analyzed...", 0.5f);
            Repaint();
            try
            {
                if (string.IsNullOrEmpty(OISettings.Instance.ApiKey))
                {
                    EditorUtility.DisplayDialog("Error", "OpenAI API Key is not set. Please set it in the Settings tab.", "OK");
                    return;
                }

                sceneDescription = await OIDescriptor.GenerateSceneDescription();
                interactionSuggestions = null;
                EditorUtility.DisplayDialog("Scene Description", "Scene description generated successfully.", "OK");
            }
            catch (System.Exception ex)
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

        private async void GenerateSuggestionsInternal()
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
            Repaint();
            try
            {
                interactionSuggestions = await OIDescriptor.GenerateInteractionSuggestions(sceneDescription, selectedObjects);
                EditorUtility.DisplayDialog("Interaction Suggestions", "Suggestions generated successfully.", "OK");
            }
            catch (System.Exception ex)
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

        // Generates the interaction using OpenAI based on the user's sentence and scene description
        private async void GenerateSentenceToInteraction()
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
            Repaint();

            try
            {
                string prompt = $"Scene Description:\n{sceneDescription}\n\nUser Request (Sentence):\n{userInteractionInput}\n\n" +
                                "1. Explain how this can be implemented in Unity.\n" +
                                "2. Identify the most suitable object(s) in the current scene for this interaction.\n" +
                                "3. Provide the relevant Unity C# script code for this interaction.";

                sentenceToInteractionResult = await OIDescriptor.RequestLLMInteraction(prompt);

                // Extract code and save script
                string code = ExtractCodeBlock(sentenceToInteractionResult);
                if (!string.IsNullOrEmpty(code))
                {
                    lastGeneratedScriptPath = SaveGeneratedScript(code);
                }
                else
                {
                    lastGeneratedScriptPath = "No code block found.";
                }

                // Extract summary
                lastScriptSummary = ExtractScriptSummary(sentenceToInteractionResult);

                // Extract suggested object names from the result
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
                lastScriptSummary = "";
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

        // Extracts a brief summary of the generated script from the LLM result
        private string ExtractScriptSummary(string result)
        {
            // Look for a line starting with 'Summary:' or 'Description:'
            using (StringReader reader = new StringReader(result))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = line.IndexOf(":");
                        if (idx != -1 && idx + 1 < line.Length)
                        {
                            return line.Substring(idx + 1).Trim();
                        }
                    }
                }
            }
            // Fallback: use the first non-empty line
            using (StringReader reader = new StringReader(result))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("```"))
                    {
                        return line.Trim();
                    }
                }
            }
            return "No summary available.";
        }
    }
} 