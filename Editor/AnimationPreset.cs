using UnityEngine;

namespace OojuInteractionPlugin
{
    [CreateAssetMenu(fileName = "New Animation Preset", menuName = "OOJU/Animation Preset")]
    public class AnimationPreset : ScriptableObject
    {
        // Animation parameters
        public float hoverSpeed = 1f;
        public float baseHoverDistance = 0.1f;
        public float wobbleSpeed = 2f;
        public float baseWobbleAngle = 5f;
        public float spinSpeed = 90f;
        public float shakeDuration = 0.5f;
        public float baseShakeMagnitude = 0.1f;
        public float bounceSpeed = 1f;
        public float baseBounceHeight = 0.5f;
        public float squashStretchRatio = 0.1f;
        public float scaleAmount = 1f;
    }
} 