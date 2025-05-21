using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using OojuInteractionPlugin;

namespace OojuInteractionPlugin
{
    public class AnimationUI
    {
        private AnimationSettings settings;
        private List<GameObject> pathPoints = new List<GameObject>();

        // ViewModel for animation parameters (for future testability)
        private class AnimationViewModel
        {
            public AnimationType SelectedAnimationType;
            public AnimationCategory SelectedCategory;
            public RelationalAnimationType SelectedRelationalType;
            public GameObject ReferenceObject;
            public List<GameObject> PathPoints = new List<GameObject>();
        }
        private AnimationViewModel viewModel = new AnimationViewModel();

        public AnimationUI()
        {
            settings = AnimationSettings.Instance;
        }

        public void DrawAnimationUI()
        {
            // Null check
            if (settings == null)
            {
                EditorGUILayout.HelpBox("AnimationSettings is not initialized.", MessageType.Error);
                return;
            }
            if (viewModel == null)
            {
                EditorGUILayout.HelpBox("Animation ViewModel is not initialized.", MessageType.Error);
                return;
            }
            if (viewModel.PathPoints == null)
            {
                viewModel.PathPoints = new List<GameObject>();
            }

            // Remove colored section box, use default background
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Space(10);
                // Unified blue-gray color for section titles
                // Color unifiedButtonColor = new Color(0.22f, 0.32f, 0.39f, 1f);
                GUIContent animIcon = EditorGUIUtility.IconContent("Animation Icon");
                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 15;
                headerStyle.normal.textColor = Color.white;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(animIcon, GUILayout.Width(24), GUILayout.Height(24));
                GUILayout.Space(6);
                EditorGUILayout.LabelField("Animation", headerStyle);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Add animations to selected objects", EditorStyles.miniLabel);
                GUILayout.Space(10);

                // Animation Type Category with colored toolbar
                Color tabColor = new Color(0.27f, 0.40f, 0.47f, 1f);
                GUIStyle toolbarStyle = new GUIStyle(EditorStyles.toolbarButton);
                toolbarStyle.fixedHeight = 28;
                toolbarStyle.fontSize = 12;
                GUI.backgroundColor = tabColor;
                viewModel.SelectedCategory = (AnimationCategory)GUILayout.Toolbar((int)viewModel.SelectedCategory, new string[] { "Independent", "Relational" }, toolbarStyle);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(10);

                if (viewModel.SelectedCategory == AnimationCategory.Independent)
                {
                    DrawIndependentAnimationUI();
                }
                else
                {
                    DrawRelationalAnimationUI();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in DrawAnimationUI: {ex.Message}");
                EditorGUILayout.HelpBox($"Error in DrawAnimationUI: {ex.Message}", MessageType.Error);
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // Helper: Create a single-color texture for background
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawIndependentAnimationUI()
        {
            // Info box with icon
            EditorGUILayout.HelpBox(new GUIContent("Independent animations are applied to each object individually (e.g., Hover, Wobble, Spin, Shake, Bounce).", EditorGUIUtility.IconContent("console.infoicon.sml").image), true);
            EditorGUILayout.BeginHorizontal();
            // Animation type label with tooltip
            GUILayout.Label(new GUIContent("Animation Type:", "Select the animation type to apply."), GUILayout.Width(120));
            // Enum popup with tooltip
            viewModel.SelectedAnimationType = (AnimationType)EditorGUILayout.EnumPopup(new GUIContent("", "Choose the animation type."), viewModel.SelectedAnimationType);
            EditorGUILayout.EndHorizontal();

            if (viewModel.SelectedAnimationType != AnimationType.None)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Parameters", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DrawIndependentAnimationParameters(viewModel.SelectedAnimationType);
                EditorGUI.indentLevel--;
                GUILayout.Space(10);
                DrawApplyAnimationButton();
            }
        }

        private void DrawIndependentAnimationParameters(AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Hover:
                    DrawHoverParameters();
                    break;
                case AnimationType.Wobble:
                    DrawWobbleParameters();
                    break;
                case AnimationType.Spin:
                    DrawSpinParameters();
                    break;
                case AnimationType.Shake:
                    DrawShakeParameters();
                    break;
                case AnimationType.Bounce:
                    DrawBounceParameters();
                    break;
                case AnimationType.Scale:
                    DrawScaleParameters();
                    break;
            }
        }
        private void DrawHoverParameters()
        {
            settings.hoverSpeed = EditorGUILayout.FloatField("Hover Speed", settings.hoverSpeed);
            settings.hoverDistance = EditorGUILayout.FloatField("Hover Distance", settings.hoverDistance);
        }
        private void DrawWobbleParameters()
        {
            settings.wobbleSpeed = EditorGUILayout.FloatField("Wobble Speed", settings.wobbleSpeed);
            settings.wobbleAngle = EditorGUILayout.FloatField("Wobble Angle", settings.wobbleAngle);
        }
        private void DrawSpinParameters()
        {
            settings.spinSpeed = EditorGUILayout.FloatField("Spin Speed", settings.spinSpeed);
        }
        private void DrawShakeParameters()
        {
            settings.shakeDuration = EditorGUILayout.FloatField("Shake Duration", settings.shakeDuration);
            settings.shakeMagnitude = EditorGUILayout.FloatField("Shake Magnitude", settings.shakeMagnitude);
        }
        private void DrawBounceParameters()
        {
            settings.bounceSpeed = EditorGUILayout.FloatField("Bounce Speed", settings.bounceSpeed);
            settings.bounceHeight = EditorGUILayout.FloatField("Bounce Height", settings.bounceHeight);
            settings.squashRatio = EditorGUILayout.FloatField("Squash Ratio", settings.squashRatio);
        }
        private void DrawScaleParameters()
        {
            settings.bounceSpeed = EditorGUILayout.FloatField("Scale Speed", settings.bounceSpeed);
            settings.bounceHeight = EditorGUILayout.FloatField("Scale Amount", settings.bounceHeight);
        }

        private void DrawApplyAnimationButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;
            GUI.backgroundColor = new Color32(0x67, 0x67, 0x67, 0xFF);
            GUI.contentColor = new Color32(0xDA, 0xDA, 0xDA, 0xFF);
            if (GUILayout.Button(new GUIContent("Apply Animation", "Apply the selected animation to the chosen objects."), GUILayout.Width(170), GUILayout.Height(34)))
            {
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    GUI.backgroundColor = prevBg;
                    GUI.contentColor = prevContent;
                    return;
                }
                ApplyAnimation(selectedObjects);
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRelationalAnimationUI()
        {
            EditorGUILayout.HelpBox("Relational animations involve a relationship with another object (e.g., orbiting around a target and returning).", MessageType.Info);
            EditorGUILayout.LabelField("Relational Animation Type", EditorStyles.boldLabel);
            viewModel.SelectedRelationalType = (RelationalAnimationType)EditorGUILayout.EnumPopup(viewModel.SelectedRelationalType);
            DrawRelationalAnimationParameters(viewModel.SelectedRelationalType);
            DrawApplyRelationalAnimationButton();
        }

        private void DrawRelationalAnimationParameters(RelationalAnimationType type)
        {
            switch (type)
            {
                case RelationalAnimationType.Orbit:
                    DrawOrbitParameters();
                    break;
                case RelationalAnimationType.LookAt:
                    DrawLookAtParameters();
                    break;
                case RelationalAnimationType.Follow:
                    DrawFollowParameters();
                    break;
                case RelationalAnimationType.MoveAlongPath:
                    DrawMoveAlongPathParameters();
                    break;
                case RelationalAnimationType.SnapToObject:
                    DrawSnapToObjectParameters();
                    break;
            }
        }
        private void DrawOrbitParameters()
        {
            viewModel.ReferenceObject = (GameObject)EditorGUILayout.ObjectField("Target", viewModel.ReferenceObject, typeof(GameObject), true);
            settings.orbitRadius = EditorGUILayout.FloatField("Orbit Radius", settings.orbitRadius);
            settings.orbitSpeed = EditorGUILayout.FloatField("Orbit Speed", settings.orbitSpeed);
        }
        private void DrawLookAtParameters()
        {
            viewModel.ReferenceObject = (GameObject)EditorGUILayout.ObjectField("Target", viewModel.ReferenceObject, typeof(GameObject), true);
            settings.lookAtSpeed = EditorGUILayout.FloatField("Look Speed", settings.lookAtSpeed);
        }
        private void DrawFollowParameters()
        {
            viewModel.ReferenceObject = (GameObject)EditorGUILayout.ObjectField("Target", viewModel.ReferenceObject, typeof(GameObject), true);
            settings.followSpeed = EditorGUILayout.FloatField("Follow Speed", settings.followSpeed);
            settings.followStopDistance = EditorGUILayout.FloatField("Stop Distance", settings.followStopDistance);
        }
        private void DrawMoveAlongPathParameters()
        {
            DrawPathPointsUI();
            settings.pathMoveSpeed = EditorGUILayout.FloatField("Move Speed", settings.pathMoveSpeed);
        }
        private void DrawSnapToObjectParameters()
        {
            viewModel.ReferenceObject = (GameObject)EditorGUILayout.ObjectField("Target", viewModel.ReferenceObject, typeof(GameObject), true);
            settings.snapRotation = EditorGUILayout.Toggle("Snap Rotation", settings.snapRotation);
        }

        private void DrawPathPointsUI()
        {
            EditorGUILayout.LabelField("Path Points (Add GameObjects):");
            int removeIdx = -1;
            for (int i = 0; i < viewModel.PathPoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                viewModel.PathPoints[i] = (GameObject)EditorGUILayout.ObjectField(viewModel.PathPoints[i], typeof(GameObject), true);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    removeIdx = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIdx >= 0) viewModel.PathPoints.RemoveAt(removeIdx);
            if (GUILayout.Button("Add Path Point")) viewModel.PathPoints.Add(null);
        }

        private void DrawApplyRelationalAnimationButton()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("This animation makes the selected object move around the target and return to its original position.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;
            GUI.backgroundColor = new Color32(0x67, 0x67, 0x67, 0xFF);
            GUI.contentColor = new Color32(0xDA, 0xDA, 0xDA, 0xFF);
            if (GUILayout.Button(new GUIContent("Apply Relational Animation", "Apply the selected relational animation to the chosen objects."), GUILayout.Width(180), GUILayout.Height(30)))
            {
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Please select at least one object.", "OK");
                    GUI.backgroundColor = prevBg;
                    GUI.contentColor = prevContent;
                    return;
                }
                if (viewModel.ReferenceObject == null && viewModel.SelectedRelationalType != RelationalAnimationType.MoveAlongPath)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a target.", "OK");
                    GUI.backgroundColor = prevBg;
                    GUI.contentColor = prevContent;
                    return;
                }
                if (viewModel.SelectedRelationalType == RelationalAnimationType.MoveAlongPath && (viewModel.PathPoints == null || viewModel.PathPoints.Count < 2))
                {
                    EditorUtility.DisplayDialog("Error", "Please add at least two path points.", "OK");
                    GUI.backgroundColor = prevBg;
                    GUI.contentColor = prevContent;
                    return;
                }
                ApplyRelationalAnimation(selectedObjects);
            }
            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyAnimation(GameObject[] selectedObjects)
        {
            foreach (var obj in selectedObjects)
            {
                AddColliderIfNeeded(obj);
                ApplyAnimationToObject(obj);
            }
        }

        private void AddColliderIfNeeded(GameObject obj)
        {
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
        }

        private void ApplyAnimationToObject(GameObject obj)
        {
            var existingAnimator = obj.GetComponent<ObjectAutoAnimator>();
            if (existingAnimator != null)
            {
                Undo.DestroyObjectImmediate(existingAnimator);
            }

            var animator = Undo.AddComponent<ObjectAutoAnimator>(obj);
            Undo.RecordObject(animator, "Set Animation");

            animator.SetOriginalTransform(obj.transform.position, obj.transform.rotation, obj.transform.localScale);
            animator.animationType = viewModel.SelectedAnimationType;
            animator.hoverSpeed = settings.hoverSpeed;
            animator.baseHoverDistance = settings.hoverDistance;
            animator.wobbleSpeed = settings.wobbleSpeed;
            animator.baseWobbleAngle = settings.wobbleAngle;
            animator.spinSpeed = settings.spinSpeed;
            animator.shakeDuration = settings.shakeDuration;
            animator.baseShakeMagnitude = settings.shakeMagnitude;
            animator.bounceSpeed = settings.bounceSpeed;
            animator.baseBounceHeight = settings.bounceHeight;
            animator.squashStretchRatio = settings.squashRatio;

            EditorUtility.SetDirty(animator);
            if (Application.isPlaying)
            {
                animator.StartAnimation();
            }
        }

        private void ApplyRelationalAnimation(GameObject[] selectedObjects)
        {
            foreach (var obj in selectedObjects)
            {
                var existingAnimator = obj.GetComponent<ObjectAutoAnimator>();
                if (existingAnimator != null)
                {
                    Undo.DestroyObjectImmediate(existingAnimator);
                }

                var animator = Undo.AddComponent<ObjectAutoAnimator>(obj);
                Undo.RecordObject(animator, "Set Relational Animation");

                // Store original transform before starting relational animation
                animator.SetOriginalTransform(obj.transform.position, obj.transform.rotation, obj.transform.localScale);

                // Assign all animation parameters from settings
                animator.hoverSpeed = settings.hoverSpeed;
                animator.baseHoverDistance = settings.hoverDistance;
                animator.wobbleSpeed = settings.wobbleSpeed;
                animator.baseWobbleAngle = settings.wobbleAngle;
                animator.spinSpeed = settings.spinSpeed;
                animator.shakeDuration = settings.shakeDuration;
                animator.baseShakeMagnitude = settings.shakeMagnitude;
                animator.bounceSpeed = settings.bounceSpeed;
                animator.baseBounceHeight = settings.bounceHeight;
                animator.squashStretchRatio = settings.squashRatio;
                animator.orbitRadius = settings.orbitRadius;
                animator.orbitSpeed = settings.orbitSpeed;
                animator.lookAtSpeed = settings.lookAtSpeed;
                animator.followSpeed = settings.followSpeed;
                animator.followStopDistance = settings.followStopDistance;
                animator.pathMoveSpeed = settings.pathMoveSpeed;
                animator.snapRotation = settings.snapRotation;

                // Assign relational type and target
                animator.relationalType = MapRelationalAnimationTypeToRelationalType(viewModel.SelectedRelationalType);
                animator.relationalReferenceObject = viewModel.ReferenceObject != null ? viewModel.ReferenceObject.transform : null;
                animator.pathPoints = viewModel.PathPoints != null ? viewModel.PathPoints.FindAll(p => p != null).ConvertAll(p => p.transform) : new List<Transform>();

                ApplyRelationalAnimationToObject(animator);
                EditorUtility.SetDirty(animator);
            }
        }

        private void ApplyRelationalAnimationToObject(ObjectAutoAnimator animator)
        {
            if (!Application.isPlaying)
            {
                // Do not start relational animation in edit mode
                return;
            }
            switch (viewModel.SelectedRelationalType)
            {
                case RelationalAnimationType.Orbit:
                    if (viewModel.ReferenceObject != null)
                    {
                        animator.StartOrbit(viewModel.ReferenceObject.transform, settings.orbitRadius, settings.orbitSpeed, settings.orbitDuration);
                    }
                    break;
                case RelationalAnimationType.LookAt:
                    if (viewModel.ReferenceObject != null)
                    {
                        animator.StartLookAt(viewModel.ReferenceObject.transform, settings.lookAtSpeed, settings.lookAtDuration);
                    }
                    break;
                case RelationalAnimationType.Follow:
                    if (viewModel.ReferenceObject != null)
                    {
                        animator.StartFollow(viewModel.ReferenceObject.transform, settings.followSpeed, settings.followStopDistance, settings.followDuration);
                    }
                    break;
                case RelationalAnimationType.MoveAlongPath:
                    var path = viewModel.PathPoints.FindAll(p => p != null).ConvertAll(p => p.transform);
                    if (path.Count > 0)
                    {
                        animator.StartMoveAlongPath(path, settings.pathMoveSpeed, settings.orbitDuration);
                    }
                    break;
                case RelationalAnimationType.SnapToObject:
                    if (viewModel.ReferenceObject != null)
                    {
                        animator.SnapToObject(viewModel.ReferenceObject.transform, settings.snapRotation);
                    }
                    break;
            }
        }

        // Map RelationalAnimationType to RelationalType
        private RelationalType MapRelationalAnimationTypeToRelationalType(RelationalAnimationType type)
        {
            switch (type)
            {
                case RelationalAnimationType.Orbit: return RelationalType.Orbit;
                case RelationalAnimationType.LookAt: return RelationalType.LookAt;
                case RelationalAnimationType.Follow: return RelationalType.Follow;
                case RelationalAnimationType.MoveAlongPath: return RelationalType.MoveAlongPath;
                case RelationalAnimationType.SnapToObject: return RelationalType.SnapToObject;
                default: return RelationalType.None;
            }
        }
    }
} 