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
        public AnimationType animationType = AnimationType.None;
        public RelationalType relationalType = RelationalType.None;
        public Transform relationalReferenceObject = null;
        public List<Transform> pathPoints = new List<Transform>();
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 originalScale;
        private Coroutine currentAnimationCoroutine = null;

        // Animation parameters (always referenced from AnimationSettings)
        public float hoverSpeed;
        public float baseHoverDistance;
        public float wobbleSpeed;
        public float baseWobbleAngle;
        public float spinSpeed;
        public float shakeDuration;
        public float baseShakeMagnitude;
        public float bounceSpeed;
        public float baseBounceHeight;
        public float squashStretchRatio;

        public float orbitRadius;
        public float orbitSpeed;
        public float lookAtSpeed;
        public float followSpeed;
        public float followStopDistance;
        public float pathMoveSpeed;
        public bool snapRotation;

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
            // Initialize animation parameters from settings
            hoverSpeed = settings.hoverSpeed;
            baseHoverDistance = settings.hoverDistance;
            wobbleSpeed = settings.wobbleSpeed;
            baseWobbleAngle = settings.wobbleAngle;
            spinSpeed = settings.spinSpeed;
            shakeDuration = settings.shakeDuration;
            baseShakeMagnitude = settings.shakeMagnitude;
            bounceSpeed = settings.bounceSpeed;
            baseBounceHeight = settings.bounceHeight;
            squashStretchRatio = settings.squashRatio;
        }

        private void OnEnable()
        {
            settings = AnimationSettings.Instance;
            // If animationType is set and in play mode, start animation automatically
            if (animationType != AnimationType.None && Application.isPlaying)
            {
                StartAnimation();
            }
            // If relationalType is set and in play mode, start relational animation automatically
            if (relationalType != RelationalType.None && Application.isPlaying)
            {
                switch (relationalType)
                {
                    case RelationalType.Orbit:
                        if (relationalReferenceObject != null && Application.isPlaying)
                            currentAnimationCoroutine = StartCoroutine(OrbitAnimation());
                        break;
                    case RelationalType.LookAt:
                        if (relationalReferenceObject != null && Application.isPlaying)
                            currentAnimationCoroutine = StartCoroutine(LookAtAnimation());
                        break;
                    case RelationalType.Follow:
                        if (relationalReferenceObject != null && Application.isPlaying)
                            currentAnimationCoroutine = StartCoroutine(FollowAnimation());
                        break;
                    case RelationalType.MoveAlongPath:
                        if (pathPoints != null && pathPoints.Count > 1 && Application.isPlaying)
                            currentAnimationCoroutine = StartCoroutine(MoveAlongPathAnimation());
                        break;
                    case RelationalType.SnapToObject:
                        if (relationalReferenceObject != null && Application.isPlaying)
                            currentAnimationCoroutine = StartCoroutine(SnapToObjectAnimation());
                        break;
                }
            }
        }

        public void SetAnimationType(AnimationType type)
        {
            animationType = type;
            StopCurrentAnimation();
            
            // Update animation parameters from settings
            hoverSpeed = settings.hoverSpeed;
            baseHoverDistance = settings.hoverDistance;
            wobbleSpeed = settings.wobbleSpeed;
            baseWobbleAngle = settings.wobbleAngle;
            spinSpeed = settings.spinSpeed;
            shakeDuration = settings.shakeDuration;
            baseShakeMagnitude = settings.shakeMagnitude;
            bounceSpeed = settings.bounceSpeed;
            baseBounceHeight = settings.bounceHeight;
            squashStretchRatio = settings.squashRatio;

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
                case AnimationType.Spin:
                    currentAnimationCoroutine = StartCoroutine(SpinAnimation());
                    break;
                case AnimationType.Shake:
                    currentAnimationCoroutine = StartCoroutine(ShakeAnimation());
                    break;
                case AnimationType.Bounce:
                    currentAnimationCoroutine = StartCoroutine(BounceAnimation());
                    break;
                case AnimationType.Scale:
                    currentAnimationCoroutine = StartCoroutine(ScaleAnimation());
                    break;
            }
        }

        public void StartOrbit(Transform target, float radius, float speed, float duration)
        {
            StopCurrentAnimation();
            relationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(OrbitAnimation());
        }

        public void StartLookAt(Transform target, float speed, float duration)
        {
            StopCurrentAnimation();
            relationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(LookAtAnimation());
        }

        public void StartFollow(Transform target, float speed, float stopDistance, float duration)
        {
            StopCurrentAnimation();
            relationalReferenceObject = target;
            currentAnimationCoroutine = StartCoroutine(FollowAnimation());
        }

        public void StartMoveAlongPath(List<Transform> points, float speed, float duration)
        {
            StopCurrentAnimation();
            pathPoints = points;
            currentAnimationCoroutine = StartCoroutine(MoveAlongPathAnimation());
        }

        public void SnapToObject(Transform target, bool rotate)
        {
            StopCurrentAnimation();
            relationalReferenceObject = target;
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
            Vector3 center = relationalReferenceObject.position;
            Vector3 startPosition = transform.position;
            Vector3 orbitAxis = Vector3.up;

            while (true)
            {
                time += Time.deltaTime;
                float angle = time * 360f * orbitSpeed;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.right * orbitRadius);
                transform.position = center + offset;
                yield return null;
            }
        }

        private IEnumerator LookAtAnimation()
        {
            while (true)
            {
                Vector3 targetDirection = (relationalReferenceObject.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator FollowAnimation()
        {
            while (true)
            {
                Vector3 targetPosition = relationalReferenceObject.position;
                Vector3 direction = (targetPosition - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, targetPosition);

                if (distance > followStopDistance)
                {
                    transform.position += direction * followSpeed * Time.deltaTime;
                }

                yield return null;
            }
        }

        private IEnumerator MoveAlongPathAnimation()
        {
            if (pathPoints.Count < 2) yield break;

            int currentPoint = 0;
            float time = 0f;

            while (true)
            {
                int nextPoint = (currentPoint + 1) % pathPoints.Count;
                Vector3 currentPos = pathPoints[currentPoint].position;
                Vector3 nextPos = pathPoints[nextPoint].position;

                time += Time.deltaTime * pathMoveSpeed;
                float t = Mathf.Clamp01(time);
                transform.position = Vector3.Lerp(currentPos, nextPos, t);

                if (t >= 1f)
                {
                    currentPoint = nextPoint;
                    time = 0f;
                }

                yield return null;
            }
        }

        private IEnumerator SnapToObjectAnimation()
        {
            float time = 0f;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 targetPosition = relationalReferenceObject.position;
            Quaternion targetRotation = settings.snapRotation ? relationalReferenceObject.rotation : startRotation;

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

        private IEnumerator SpinAnimation()
        {
            while (true)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
                yield return null;
            }
        }

        private IEnumerator ShakeAnimation()
        {
            float elapsed = 0f;
            Vector3 originalPos = originalPosition;
            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * baseShakeMagnitude;
                float y = Random.Range(-1f, 1f) * baseShakeMagnitude;
                float z = Random.Range(-1f, 1f) * baseShakeMagnitude;
                transform.position = originalPos + new Vector3(x, y, z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = originalPos;
        }

        private IEnumerator BounceAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * bounceSpeed;
                float yOffset = Mathf.Abs(Mathf.Sin(time)) * baseBounceHeight;
                float squash = 1f + Mathf.Sin(time) * squashStretchRatio;
                transform.position = originalPosition + new Vector3(0f, yOffset, 0f);
                transform.localScale = new Vector3(originalScale.x, originalScale.y * squash, originalScale.z);
                yield return null;
            }
        }

        public void SetOriginalTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            originalPosition = pos;
            originalRotation = rot;
            originalScale = scale;
        }

        // Store the current transform values as the original state
        private void StoreOriginalTransform()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
        }
    }
} 