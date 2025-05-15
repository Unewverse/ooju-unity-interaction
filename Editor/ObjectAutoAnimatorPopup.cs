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

    private float hoverSpeed = 1f;
    private float hoverDistance = 0.1f;
    private float wobbleSpeed = 2f;
    private float wobbleAngle = 5f;
    private float scaleAmount = 1f;

    private void OnEnable()
    {
        // Load defaults from preset or AnimationSettings
        if (preset != null)
        {
            hoverSpeed = preset.hoverSpeed;
            hoverDistance = preset.baseHoverDistance;
            wobbleSpeed = preset.wobbleSpeed;
            wobbleAngle = preset.baseWobbleAngle;
            // scaleAmount can be added to preset if needed
        }
        else
        {
            hoverSpeed = AnimationSettings.Instance.hoverSpeed;
            hoverDistance = AnimationSettings.Instance.hoverDistance;
            wobbleSpeed = AnimationSettings.Instance.wobbleSpeed;
            wobbleAngle = AnimationSettings.Instance.wobbleAngle;
            // scaleAmount = 1f;
        }
    }

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

        // Parameter input for each type
        switch (animationType)
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
                scaleAmount = EditorGUILayout.FloatField("Scale Amount", scaleAmount);
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
        switch (animationType)
        {
            case AnimationType.Hover:
                AnimationSettings.Instance.hoverSpeed = hoverSpeed;
                AnimationSettings.Instance.hoverDistance = hoverDistance;
                break;
            case AnimationType.Wobble:
                AnimationSettings.Instance.wobbleSpeed = wobbleSpeed;
                AnimationSettings.Instance.wobbleAngle = wobbleAngle;
                break;
            case AnimationType.Scale:
                // If you add scaleAmount to AnimationSettings, set it here
                break;
        }
        EditorUtility.SetDirty(animator);
    }
}
} 