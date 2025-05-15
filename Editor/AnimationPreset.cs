using UnityEngine;

namespace OojuInteractionPlugin
{
    [CreateAssetMenu(menuName = "OOJU/Animation Preset")]
    public class AnimationPreset : ScriptableObject
    {
        // Hover
        public float hoverSpeed = 1f;
        public float baseHoverDistance = 0.1f;
        // Wobble
        public float wobbleSpeed = 2f;
        public float baseWobbleAngle = 5f;
        // Spin
        public float spinSpeed = 90f;
        // Shake
        public float shakeDuration = 0.5f;
        public float baseShakeMagnitude = 0.1f;
        // Bounce
        public float bounceSpeed = 1f;
        public float baseBounceHeight = 0.5f;
        public float squashStretchRatio = 0.1f;
    }
} 