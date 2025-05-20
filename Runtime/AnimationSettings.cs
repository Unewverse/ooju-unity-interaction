using UnityEngine;

namespace OojuInteractionPlugin
{
    [System.Serializable]
    public class AnimationSettings
    {
        // Independent Animation Parameters
        public float hoverSpeed = 1f;
        public float hoverDistance = 0.1f;
        public float wobbleSpeed = 2f;
        public float wobbleAngle = 5f;
        public float spinSpeed = 90f;
        public float shakeDuration = 0.5f;
        public float shakeMagnitude = 0.1f;
        public float bounceSpeed = 1f;
        public float bounceHeight = 0.5f;
        public float squashRatio = 0.1f;

        // Relational Animation Parameters
        public float orbitRadius = 2f;
        public float orbitSpeed = 1f;
        public float orbitDuration = 3f;
        public float lookAtSpeed = 5f;
        public float lookAtDuration = 2f;
        public float followSpeed = 2f;
        public float followStopDistance = 0.2f;
        public float followDuration = 3f;
        public float pathMoveSpeed = 2f;
        public float pathMoveDuration = 3f;
        public bool snapRotation = true;

        // Singleton instance
        private static AnimationSettings instance;
        public static AnimationSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AnimationSettings();
                }
                return instance;
            }
        }

        private AnimationSettings() { }
    }
} 