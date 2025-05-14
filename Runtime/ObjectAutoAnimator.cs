using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace OojuInteractionPlugin
{
    // Core interfaces and classes
    public interface IObjectAnimation
    {
        bool IsPlaying { get; }
        void StartAnimation();
        void StopAnimation();
        void ResetToInitialState();
    }

    public class DefaultSettingsProvider { }

    public class ObjectAnimationFactory
    {
        private DefaultSettingsProvider settingsProvider;

        public ObjectAnimationFactory(DefaultSettingsProvider provider)
        {
            settingsProvider = provider;
        }

        public IObjectAnimation CreateAnimation(GameObject obj, AnimationType type)
        {
            // For now, return null as we're not implementing actual animations
            // This can be expanded later with actual animation implementations
            return null;
        }
    }

    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager _instance;
        public static CoroutineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CoroutineManager");
                    _instance = go.AddComponent<CoroutineManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void StopManagedCoroutines(string id)
        {
            // Implementation can be added later if needed
        }
    }

    [AddComponentMenu("OOJU/Object Auto Animator")]
    public class ObjectAutoAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public AnimationType animationType = AnimationType.None;
        private IObjectAnimation currentAnimation;

        [Header("Hover Settings")]
        public float hoverSpeed = 1f;
        public float baseHoverDistance = 0.1f;

        [Header("Wobble Settings")]
        public float wobbleSpeed = 2f;
        public float baseWobbleAngle = 5f;

        [Header("Spin Settings")]
        public float spinSpeed = 90f;

        [Header("Shake Settings")]
        public float shakeDuration = 0.5f;
        public float baseShakeMagnitude = 0.1f;

        [Header("Bounce Settings")]
        public float bounceSpeed = 1f;
        public float baseBounceHeight = 0.5f;
        public float squashStretchRatio = 0.1f;

        [Header("Relational Animation Settings")]
        public RelationalType relationalType = RelationalType.None;
        public Transform relationalReferenceObject;
        public float orbitRadius = 2f;
        public float orbitSpeed = 1f;
        public float orbitDuration = 3f;
        public float lookAtSpeed = 5f;
        public float lookAtDuration = 2f;
        public float followSpeed = 2f;
        public float followStopDistance = 0.2f;
        public float followDuration = 3f;
        public List<Transform> pathPoints = new List<Transform>();
        public float pathMoveSpeed = 2f;
        public float pathMoveDuration = 3f;
        public bool snapRotation = true;

        // Private variables for transform state
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private float objectSize;
        private bool isAnimating = false;

        // Unique identifier for coroutine management
        private string CoroutineIdentifier => $"ObjectAutoAnimator_{GetInstanceID()}";

        private void Awake()
        {
            // Create animation based on type
            var factory = new ObjectAnimationFactory(new DefaultSettingsProvider());
            currentAnimation = factory.CreateAnimation(gameObject, animationType);
        }

        private void OnEnable()
        {
            if (currentAnimation != null)
            {
                currentAnimation.StartAnimation();
            }
            // Store initial transform state
            StoreInitialState();
        }

        private void StoreInitialState()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialScale = transform.localScale;

            // Calculate object size based on renderer bounds
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                objectSize = renderer.bounds.extents.magnitude;
            }
            else
            {
                objectSize = 1f;
                Debug.LogWarning($"No renderer found in {gameObject.name}. Using default size.");
            }
        }

        private void Start()
        {
            StoreInitialState();
            StartAnimation();
            // Automatically start Relational Animation
            switch (relationalType)
            {
                case RelationalType.Orbit:
                    if (relationalReferenceObject != null)
                        StartOrbit(relationalReferenceObject, orbitRadius, orbitSpeed, orbitDuration);
                    break;
                case RelationalType.LookAt:
                    if (relationalReferenceObject != null)
                        StartLookAt(relationalReferenceObject, lookAtSpeed, lookAtDuration);
                    break;
                case RelationalType.Follow:
                    if (relationalReferenceObject != null)
                        StartFollow(relationalReferenceObject, followSpeed, followStopDistance, followDuration);
                    break;
                case RelationalType.MoveAlongPath:
                    if (pathPoints != null && pathPoints.Count > 0)
                        StartMoveAlongPath(pathPoints, pathMoveSpeed, pathMoveDuration);
                    break;
                case RelationalType.SnapToObject:
                    if (relationalReferenceObject != null)
                        SnapToObject(relationalReferenceObject, snapRotation);
                    break;
            }
            Debug.Log($"[AutoAnimator] relationalType={relationalType}, reference={relationalReferenceObject}");
        }

        private void OnDisable()
        {
            if (currentAnimation != null)
            {
                currentAnimation.StopAnimation();
            }
            CoroutineManager.Instance.StopManagedCoroutines(CoroutineIdentifier);
            // Reset to initial state when disabled
            if (gameObject.activeInHierarchy)
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
                transform.localScale = initialScale;
            }
            isAnimating = false;
        }

        private void OnDestroy()
        {
            if (currentAnimation != null)
            {
                currentAnimation.StopAnimation();
                currentAnimation.ResetToInitialState();
            }
            CoroutineManager.Instance.StopManagedCoroutines(CoroutineIdentifier);
        }

        private IEnumerator HoverAnimation()
        {
            float time = 0f;
            // Scale hover distance based on object size
            float scaledHoverDistance = baseHoverDistance * objectSize;
            var rb = GetComponent<Rigidbody>();
            while (isAnimating)
            {
                time += Time.deltaTime * hoverSpeed;
                float yOffset = Mathf.Sin(time) * scaledHoverDistance;
                Vector3 targetPos = initialPosition + new Vector3(0f, yOffset, 0f);
                if (rb != null && rb.isKinematic)
                    rb.MovePosition(targetPos);
                else
                    transform.position = targetPos;
                yield return null;
            }
        }

        private IEnumerator WobbleAnimation()
        {
            float time = 0f;
            // Scale wobble angle based on object size
            float scaledWobbleAngle = baseWobbleAngle;

            while (isAnimating)
            {
                time += Time.deltaTime * wobbleSpeed;
                float rotationX = Mathf.Sin(time) * scaledWobbleAngle;
                float rotationZ = Mathf.Cos(time) * scaledWobbleAngle;
                transform.rotation = initialRotation * Quaternion.Euler(rotationX, 0f, rotationZ);
                yield return null;
            }
        }

        private IEnumerator SpinAnimation()
        {
            float time = 0f;
            while (isAnimating)
            {
                time += Time.deltaTime * spinSpeed;
                transform.rotation = initialRotation * Quaternion.Euler(0f, time, 0f);
                yield return null;
            }
        }

        private IEnumerator ShakeAnimation()
        {
            float elapsed = 0f;
            // Scale shake magnitude based on object size
            float scaledShakeMagnitude = baseShakeMagnitude * objectSize;
            var rb = GetComponent<Rigidbody>();
            while (elapsed < shakeDuration && isAnimating)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / shakeDuration;
                float currentMagnitude = scaledShakeMagnitude * (1f - progress);

                Vector3 randomOffset = Random.insideUnitSphere * currentMagnitude;
                Vector3 targetPos = initialPosition + randomOffset;
                if (rb != null && rb.isKinematic)
                    rb.MovePosition(targetPos);
                else
                    transform.position = targetPos;
                transform.rotation = initialRotation * Quaternion.Euler(
                    Random.Range(-currentMagnitude, currentMagnitude),
                    Random.Range(-currentMagnitude, currentMagnitude),
                    Random.Range(-currentMagnitude, currentMagnitude)
                );

                yield return null;
            }

            // Reset to initial state when shake ends
            if (rb != null && rb.isKinematic)
                rb.MovePosition(initialPosition);
            else
                transform.position = initialPosition;
            transform.rotation = initialRotation;
            isAnimating = false;
        }

        private IEnumerator BounceAnimation()
        {
            float time = 0f;
            // Scale bounce height based on object size
            float scaledBounceHeight = baseBounceHeight * objectSize;
            var rb = GetComponent<Rigidbody>();
            while (isAnimating)
            {
                time += Time.deltaTime * bounceSpeed;
                float yOffset = Mathf.Abs(Mathf.Sin(time)) * scaledBounceHeight;
                Vector3 targetPos = initialPosition + new Vector3(0f, yOffset, 0f);
                if (rb != null && rb.isKinematic)
                    rb.MovePosition(targetPos);
                else
                    transform.position = targetPos;

                // Apply squash and stretch effect relative to initial scale
                float squashFactor = 1f - (Mathf.Abs(Mathf.Sin(time)) * squashStretchRatio);
                float stretchFactor = 1f + (Mathf.Abs(Mathf.Sin(time)) * squashStretchRatio * 0.5f);
                transform.localScale = new Vector3(
                    initialScale.x * stretchFactor,
                    initialScale.y * squashFactor,
                    initialScale.z * stretchFactor
                );

                yield return null;
            }
        }

        public void StartAnimation()
        {
            if (currentAnimation != null && !currentAnimation.IsPlaying)
            {
                currentAnimation.StartAnimation();
            }
        }

        public void StopAnimation()
        {
            if (currentAnimation != null && currentAnimation.IsPlaying)
            {
                currentAnimation.StopAnimation();
            }
        }

        public void ResetToInitialState()
        {
            if (currentAnimation != null)
            {
                currentAnimation.ResetToInitialState();
            }
        }

        public void SetAnimationType(AnimationType newType)
        {
            if (animationType == newType)
                return;

            // Stop and clean up current animation
            if (currentAnimation != null)
            {
                currentAnimation.StopAnimation();
                currentAnimation.ResetToInitialState();
                Destroy(currentAnimation as MonoBehaviour);
            }

            // Create new animation
            animationType = newType;
            var factory = new ObjectAnimationFactory(new DefaultSettingsProvider());
            currentAnimation = factory.CreateAnimation(gameObject, animationType);

            // Start new animation if component is enabled
            if (enabled)
            {
                currentAnimation.StartAnimation();
            }
        }

        // Relational Animation Methods
        public void StartOrbit(Transform reference, float radius, float speed, float duration)
        {
            if (Application.isPlaying)
            {
                StoreInitialState();
                StopAllCoroutines();
                StartCoroutine(OrbitAroundObject(reference, radius, speed, duration));
            }
            else
            {
                Undo.RecordObject(this, "Set Relational Animation Params");
                EditorUtility.SetDirty(this);
            }
        }

        private IEnumerator OrbitAroundObject(Transform reference, float radius, float speed, float duration)
        {
            Vector3 center = reference.position;
            Vector3 startOffset = transform.position - center;
            float startAngle = Mathf.Atan2(startOffset.z, startOffset.x);
            float time = 0f;
            var rb = GetComponent<Rigidbody>();
            while (duration <= 0f || time < duration)
            {
                time += Time.deltaTime;
                float angle = startAngle + time * speed;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 targetPos = center + offset;
                if (rb != null && rb.isKinematic)
                    rb.MovePosition(targetPos);
                else
                    transform.position = targetPos;
                yield return null;
            }
            // Return to initial position
            if (rb != null && rb.isKinematic)
                rb.MovePosition(initialPosition);
            else
                transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        public void StartLookAt(Transform target, float lookSpeed, float duration)
        {
            if (Application.isPlaying)
            {
                StoreInitialState();
                StopAllCoroutines();
                StartCoroutine(LookAtTarget(target, lookSpeed, duration));
            }
            else
            {
                Undo.RecordObject(this, "Set Relational Animation Params");
                EditorUtility.SetDirty(this);
            }
        }

        private IEnumerator LookAtTarget(Transform target, float lookSpeed, float duration)
        {
            float time = 0f;
            while (duration <= 0f || time < duration)
            {
                time += Time.deltaTime;
                Vector3 direction = (target.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
                yield return null;
            }
            // Return to initial position
            transform.rotation = initialRotation;
            transform.position = initialPosition;
        }

        public void StartFollow(Transform target, float followSpeed, float stopDistance, float duration)
        {
            if (Application.isPlaying)
            {
                StoreInitialState();
                StopAllCoroutines();
                StartCoroutine(FollowTarget(target, followSpeed, stopDistance, duration));
            }
            else
            {
                Undo.RecordObject(this, "Set Relational Animation Params");
                EditorUtility.SetDirty(this);
            }
        }

        private IEnumerator FollowTarget(Transform target, float followSpeed, float stopDistance, float duration)
        {
            float time = 0f;
            var rb = GetComponent<Rigidbody>();
            while (duration <= 0f || time < duration)
            {
                time += Time.deltaTime;
                Vector3 direction = (target.position - transform.position);
                if (direction.magnitude > stopDistance)
                {
                    Vector3 move = direction.normalized * followSpeed * Time.deltaTime;
                    Vector3 targetPos = transform.position + move;
                    if (rb != null && rb.isKinematic)
                        rb.MovePosition(targetPos);
                    else
                        transform.position = targetPos;
                }
                yield return null;
            }
            // Return to initial position
            if (rb != null && rb.isKinematic)
                rb.MovePosition(initialPosition);
            else
                transform.position = initialPosition;
        }

        public void StartMoveAlongPath(List<Transform> waypoints, float moveSpeed, float duration = 0f)
        {
            if (Application.isPlaying)
            {
                StoreInitialState();
                StopAllCoroutines();
                StartCoroutine(MoveAlongPath(waypoints, moveSpeed, duration));
            }
            else
            {
                Undo.RecordObject(this, "Set Relational Animation Params");
                EditorUtility.SetDirty(this);
            }
        }

        private IEnumerator MoveAlongPath(List<Transform> waypoints, float moveSpeed, float duration = 0f)
        {
            int currentWaypoint = 0;
            float time = 0f;
            var rb = GetComponent<Rigidbody>();
            while ((duration <= 0f || time < duration) && waypoints.Count > 0)
            {
                time += Time.deltaTime;
                Transform currentTarget = waypoints[currentWaypoint];
                if ((transform.position - currentTarget.position).magnitude <= 0.05f)
                {
                    currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
                }
                else
                {
                    Vector3 targetPos = Vector3.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);
                    if (rb != null && rb.isKinematic)
                        rb.MovePosition(targetPos);
                    else
                        transform.position = targetPos;
                }
                yield return null;
            }
            // Return to initial position
            if (rb != null && rb.isKinematic)
                rb.MovePosition(initialPosition);
            else
                transform.position = initialPosition;
        }

        public void SnapToObject(Transform reference, bool snapRotation)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
                rb.MovePosition(reference.position);
            else
                transform.position = reference.position;
            if (snapRotation)
                transform.rotation = reference.rotation;
        }
    }
} 