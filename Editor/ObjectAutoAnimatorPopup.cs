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

    // Animation parameters (default values)
    private float hoverSpeed = 1f;
    private float baseHoverDistance = 0.1f;
    private float wobbleSpeed = 2f;
    private float baseWobbleAngle = 5f;
    private float spinSpeed = 90f;
    private float shakeDuration = 0.5f;
    private float baseShakeMagnitude = 0.1f;
    private float bounceSpeed = 1f;
    private float baseBounceHeight = 0.5f;
    private float squashStretchRatio = 0.1f;
    private float scaleAmount = 1f;

    private void OnEnable()
    {
        // Load defaults from preset or AnimationSettings
        if (preset != null)
        {
            hoverSpeed = preset.hoverSpeed;
            baseHoverDistance = preset.baseHoverDistance;
            wobbleSpeed = preset.wobbleSpeed;
            baseWobbleAngle = preset.baseWobbleAngle;
            // scaleAmount can be added to preset if needed
        }
        else
        {
            hoverSpeed = AnimationSettings.Instance.hoverSpeed;
            baseHoverDistance = AnimationSettings.Instance.hoverDistance;
            wobbleSpeed = AnimationSettings.Instance.wobbleSpeed;
            baseWobbleAngle = AnimationSettings.Instance.wobbleAngle;
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
                baseHoverDistance = EditorGUILayout.FloatField("Base Hover Distance", baseHoverDistance);
                break;
            case AnimationType.Wobble:
                wobbleSpeed = EditorGUILayout.FloatField("Wobble Speed", wobbleSpeed);
                baseWobbleAngle = EditorGUILayout.FloatField("Base Wobble Angle", baseWobbleAngle);
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

        // Backup current transform values
        Vector3 pos = targetObject.transform.position;
        Quaternion rot = targetObject.transform.rotation;
        Vector3 scale = targetObject.transform.localScale;

        var animator = targetObject.GetComponent<ObjectAutoAnimator>();
        if (animator == null)
        {
            animator = targetObject.AddComponent<ObjectAutoAnimator>();
            // Restore transform values in case Unity resets them
            targetObject.transform.position = pos;
            targetObject.transform.rotation = rot;
            targetObject.transform.localScale = scale;
        }

        // Always set original transform with the correct values
        animator.SetOriginalTransform(pos, rot, scale);

        animator.SetAnimationType(animationType);
        // Set parameters for each type
        switch (animationType)
        {
            case AnimationType.Hover:
                AnimationSettings.Instance.hoverSpeed = hoverSpeed;
                AnimationSettings.Instance.hoverDistance = baseHoverDistance;
                break;
            case AnimationType.Wobble:
                AnimationSettings.Instance.wobbleSpeed = wobbleSpeed;
                AnimationSettings.Instance.wobbleAngle = baseWobbleAngle;
                break;
            case AnimationType.Scale:
                // If you add scaleAmount to AnimationSettings, set it here
                break;
        }
        animator.hoverSpeed = hoverSpeed;
        animator.baseHoverDistance = baseHoverDistance;
        animator.wobbleSpeed = wobbleSpeed;
        animator.baseWobbleAngle = baseWobbleAngle;
        animator.spinSpeed = spinSpeed;
        animator.shakeDuration = shakeDuration;
        animator.baseShakeMagnitude = baseShakeMagnitude;
        animator.bounceSpeed = bounceSpeed;
        animator.baseBounceHeight = baseBounceHeight;
        animator.squashStretchRatio = squashStretchRatio;
        EditorUtility.SetDirty(animator);
    }
}
} 