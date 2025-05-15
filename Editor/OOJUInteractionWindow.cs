using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using OojuInteractionPlugin;

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
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(20);

            // Description & Analysis section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
                descriptionScrollPosition = EditorGUILayout.BeginScrollView(descriptionScrollPosition, GUILayout.Height(100));
                EditorGUILayout.TextArea(sceneDescription, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
            }
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            // Interaction Suggestions section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
                                // Collider 자동 추가
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
                Repaint();
            }
        }
    }
} 