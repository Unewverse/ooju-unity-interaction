using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using OojuInteractionPlugin;

namespace OojuInteractionPlugin
{
public class ObjectAutoAnimatorPopup : EditorWindow
{
    private GameObject targetObject;
    private AnimationType animationType = AnimationType.Hover;

    // AnimationPreset ScriptableObject reference
    public AnimationPreset preset;

    public static void ShowWindow(GameObject obj)
    {
        var window = ScriptableObject.CreateInstance<ObjectAutoAnimatorPopup>();
        window.titleContent = new GUIContent("Add Object Animation");
        window.targetObject = obj;
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 350);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        if (targetObject == null)
        {
            EditorGUILayout.HelpBox("No target object selected.", MessageType.Error);
            if (GUILayout.Button("Close")) this.Close();
            return;
        }

        EditorGUILayout.LabelField("Select Animation Type", EditorStyles.boldLabel);
        animationType = (AnimationType)EditorGUILayout.EnumPopup("Animation Type", animationType);
        EditorGUILayout.Space();

        // Input parameters for each type
        switch (animationType)
        {
            case AnimationType.Hover:
                // Add hover parameters if needed
                break;
            case AnimationType.Wobble:
                // Add wobble parameters if needed
                break;
            case AnimationType.Scale:
                // Add scale parameters if needed
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply", GUILayout.Height(30)))
        {
            ApplyAnimation();
            this.Close();
        }
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            this.Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyAnimation()
    {
        if (targetObject == null) return;
        var animator = targetObject.GetComponent<ObjectAutoAnimator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<ObjectAutoAnimator>(targetObject);
        }
        Undo.RecordObject(animator, "Set ObjectAutoAnimator Properties");
        animator.SetAnimationType(animationType);
        // Set parameters for each type
        EditorUtility.SetDirty(animator);
    }
}
} 