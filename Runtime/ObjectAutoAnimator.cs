using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using OojuInteractionPlugin;

namespace OojuInteractionPlugin
{
    // Core interfaces and classes
    // TODO: Remove or implement if needed. Currently not used.
    // public interface IObjectAnimation { ... }
    // public class ObjectAnimationFactory { ... }
    // public class CoroutineManager : MonoBehaviour { ... }

    [AddComponentMenu("OOJU/Object Auto Animator")]
    public class ObjectAutoAnimator : MonoBehaviour
    {
        private AnimationSettings settings;
        private AnimationType animationType = AnimationType.None;
        private RelationalType relationalType = RelationalType.None;
        private Transform relationalReferenceObject = null;
        private List<Transform> pathPoints = new List<Transform>();
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Coroutine currentAnimationCoroutine = null;

        // Animation parameters (always referenced from AnimationSettings)
        public float hoverSpeed { get; set; }
        public float baseHoverDistance { get; set; }
        public float wobbleSpeed { get; set; }
        public float baseWobbleAngle { get; set; }
        public float spinSpeed { get; set; }
        public float shakeDuration { get; set; }
        public float baseShakeMagnitude { get; set; }
        public float bounceSpeed { get; set; }
        public float baseBounceHeight { get; set; }
        public float squashStretchRatio { get; set; }

        public RelationalType RelationalType
        {
            get => relationalType;
            private set => relationalType = value;
        }
        public Transform RelationalReferenceObject
        {
            get => relationalReferenceObject;
            private set => relationalReferenceObject = value;
        }
        public List<Transform> PathPoints
        {
            get => pathPoints;
            private set => pathPoints = value;
        }

        private void Awake()
        {
            settings = AnimationSettings.Instance;
        }

        public void SetAnimationType(AnimationType type)
        {
            animationType = type;
            StopCurrentAnimation();
            if (Application.isPlaying)
            {
                StartAnimation();
            }
        }

        public void StartAnimation()
        {
            StopCurrentAnimation();
            StoreOriginalTransform();
            switch (animationType)
            {
                case AnimationType.Hover:
                    currentAnimationCoroutine = StartCoroutine(HoverAnimation());
                    break;
                case AnimationType.Wobble:
                    currentAnimationCoroutine = StartCoroutine(WobbleAnimation());
                    break;
                case AnimationType.Scale:
                    currentAnimationCoroutine = StartCoroutine(ScaleAnimation());
                    break;
            }
        }

        public void StartOrbit(Transform target, float radius, float speed, float duration)
        {
            StopCurrentAnimation();
            RelationalType = RelationalType.Orbit;
            RelationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(OrbitAnimation());
        }

        public void StartLookAt(Transform target, float speed, float duration)
        {
            StopCurrentAnimation();
            RelationalType = RelationalType.LookAt;
            RelationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(LookAtAnimation());
        }

        public void StartFollow(Transform target, float speed, float stopDistance, float duration)
        {
            StopCurrentAnimation();
            RelationalType = RelationalType.Follow;
            RelationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(FollowAnimation());
        }

        public void StartMoveAlongPath(List<Transform> points, float speed, float duration)
        {
            StopCurrentAnimation();
            RelationalType = RelationalType.MoveAlongPath;
            PathPoints = points;
            currentAnimationCoroutine = StartCoroutine(MoveAlongPathAnimation());
        }

        public void SnapToObject(Transform target, bool rotate)
        {
            StopCurrentAnimation();
            RelationalType = RelationalType.SnapToObject;
            RelationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(SnapToObjectAnimation());
        }

        private void StopCurrentAnimation()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }
            ResetTransform();
        }

        private void ResetTransform()
        {
            Debug.Log($"[ObjectAutoAnimator] ResetTransform - Resetting to originalPosition: {originalPosition}");
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
        }

        private IEnumerator HoverAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * hoverSpeed;
                float yOffset = Mathf.Sin(time) * baseHoverDistance;
                Vector3 newPos = originalPosition + new Vector3(0f, yOffset, 0f);
                Debug.Log($"[ObjectAutoAnimator] HoverAnimation - time: {time}, yOffset: {yOffset}, newPos: {newPos}");
                transform.position = newPos;
                yield return null;
            }
        }

        private IEnumerator WobbleAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * wobbleSpeed;
                float angle = Mathf.Sin(time) * baseWobbleAngle;
                transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
        }

        private IEnumerator ScaleAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                float scale = 1f + Mathf.Sin(time) * 0.1f;
                transform.localScale = originalScale * scale;
                yield return null;
            }
        }

        private IEnumerator OrbitAnimation()
        {
            float time = 0f;
            Vector3 center = RelationalReferenceObject.position;
            Vector3 startPosition = transform.position;
            Vector3 orbitAxis = Vector3.up;

            while (time < settings.orbitDuration)
            {
                time += Time.deltaTime;
                float angle = (time / settings.orbitDuration) * 360f * settings.orbitSpeed;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.right * settings.orbitRadius);
                transform.position = center + offset;
                yield return null;
            }

            ResetTransform();
        }

        private IEnumerator LookAtAnimation()
        {
            float time = 0f;
            Quaternion startRotation = transform.rotation;
            Vector3 targetDirection = (RelationalReferenceObject.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            while (time < settings.lookAtDuration)
            {
                time += Time.deltaTime;
                float t = time / settings.lookAtDuration;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            ResetTransform();
        }

        private IEnumerator FollowAnimation()
        {
            float time = 0f;
            Vector3 startPosition = transform.position;

            while (time < settings.followDuration)
            {
                time += Time.deltaTime;
                Vector3 targetPosition = RelationalReferenceObject.position;
                Vector3 direction = (targetPosition - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, targetPosition);

                if (distance > settings.followStopDistance)
                {
                    transform.position += direction * settings.followSpeed * Time.deltaTime;
                }

                yield return null;
            }

            ResetTransform();
        }

        private IEnumerator MoveAlongPathAnimation()
        {
            if (PathPoints.Count < 2) yield break;

            float time = 0f;
            int currentPoint = 0;
            Vector3 startPosition = transform.position;

            while (time < settings.pathMoveDuration)
            {
                time += Time.deltaTime;
                float t = time / settings.pathMoveDuration;

                int nextPoint = (currentPoint + 1) % PathPoints.Count;
                Vector3 currentPos = PathPoints[currentPoint].position;
                Vector3 nextPos = PathPoints[nextPoint].position;

                transform.position = Vector3.Lerp(currentPos, nextPos, t);

                if (t >= 1f)
                {
                    currentPoint = nextPoint;
                    time = 0f;
                }

                yield return null;
            }

            ResetTransform();
        }

        private IEnumerator SnapToObjectAnimation()
        {
            float time = 0f;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 targetPosition = RelationalReferenceObject.position;
            Quaternion targetRotation = settings.snapRotation ? RelationalReferenceObject.rotation : startRotation;

            while (time < 0.5f)
            {
                time += Time.deltaTime;
                float t = time / 0.5f;
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                if (settings.snapRotation)
                {
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }
                yield return null;
            }

            transform.position = targetPosition;
            if (settings.snapRotation)
            {
                transform.rotation = targetRotation;
            }
        }

        public void SetOriginalTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            originalPosition = pos;
            originalRotation = rot;
            originalScale = scale;
        }
    }
} 