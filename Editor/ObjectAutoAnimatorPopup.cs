using UnityEditor;
using UnityEngine;
using OojuInteractionPlugin;

namespace OojuInteractionPlugin
{
public class ObjectAutoAnimatorPopup : EditorWindow
{
    private GameObject targetObject;
    private AnimationType animationType = AnimationType.Hover;

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
                hoverSpeed = EditorGUILayout.FloatField("Hover Speed", hoverSpeed);
                baseHoverDistance = EditorGUILayout.FloatField("Base Hover Distance", baseHoverDistance);
                break;
            case AnimationType.Wobble:
                wobbleSpeed = EditorGUILayout.FloatField("Wobble Speed", wobbleSpeed);
                baseWobbleAngle = EditorGUILayout.FloatField("Base Wobble Angle", baseWobbleAngle);
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