using System;
using UnityEngine;
using Pete.Data;
using Pete.Systems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pete.Interaction
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Horizontal Rotation (A / D)")]
        [Tooltip("ความเร็วในการหมุนกล้อง (องศาต่อวินาที)")]
        [SerializeField] private float rotationSpeed = 100f;
        [Tooltip("เวลาในการชะลอการหมุนให้นุ่มนวล (Smooth Damping)")]
        [SerializeField] private float smoothTime = 0.12f;

        [Header("Seated View Reference")]
        [SerializeField] private Vector3 seatedPosition = new Vector3(0.03f, 1.31f, -9.257f);
        [SerializeField] private float seatedPitch = 0f;

        [Header("Inspect / Zoom Settings")]
        [SerializeField] private float inspectTransitionSpeed = 8f;

        // Runtime states
        private float currentYaw;
        private float targetYaw;
        private float yawVelocity;
        private bool allowRightClickBack = true;

        public bool IsInspecting { get; private set; }
        public InspectTargetType CurrentInspectType { get; private set; } = InspectTargetType.None;

        private Transform currentInspectAnchor;
        private Vector3 inspectTargetPos;
        private Quaternion inspectTargetRot;
        private Camera targetCamera;

        public event Action<InspectTargetType> OnInspectStarted;
        public event Action OnInspectEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            Time.timeScale = 1f;

            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            seatedPosition = transform.position;
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            targetYaw = currentYaw;
            seatedPitch = angles.x;

            ApplySavedControlSettings();
        }

        public void ApplySavedControlSettings()
        {
            // MM_TurnSens: 1..20 (default 10 -> 100 deg/s)
            int sens = PlayerPrefs.GetInt("MM_TurnSens", 10);
            rotationSpeed = Mathf.Clamp(sens * 10f, 10f, 250f);

            // MM_TurnSmooth: 0 = OFF, 1 = LOW, 2 = HIGH
            int smoothMode = PlayerPrefs.GetInt("MM_TurnSmooth", 1);
            if (smoothMode == 0) smoothTime = 0.01f;
            else if (smoothMode == 1) smoothTime = 0.12f;
            else smoothTime = 0.28f;

            // MM_RightClickBack: 0 = OFF, 1 = ON
            allowRightClickBack = PlayerPrefs.GetInt("MM_RightClickBack", 1) == 1;
        }

        private void Update()
        {
            // ขณะกด Spacebar หลับตา จะไม่สามารถคลิกโต้ตอบวัตถุได้
            bool eyesClosed = SanityManager.Instance != null && SanityManager.Instance.IsClosingEyes;

            bool exitRequested = false;
            float rotationInput = 0f;
            bool leftClick = false;
            Vector2 mouseScreenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) exitRequested = true;
                if (kb.aKey.isPressed) rotationInput -= 1f;
                if (kb.dKey.isPressed) rotationInput += 1f;
            }

            if (mouse != null)
            {
                if (allowRightClickBack && mouse.rightButton.wasPressedThisFrame) exitRequested = true;
                if (mouse.leftButton.wasPressedThisFrame) leftClick = true;
                mouseScreenPos = mouse.position.ReadValue();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape) || (allowRightClickBack && Input.GetMouseButtonDown(1))) exitRequested = true;
            if (Input.GetKey(KeyCode.A)) rotationInput -= 1f;
            if (Input.GetKey(KeyCode.D)) rotationInput += 1f;
            if (Input.GetMouseButtonDown(0)) leftClick = true;
            mouseScreenPos = Input.mousePosition;
#endif

            if (IsInspecting)
            {
                if (exitRequested && !eyesClosed)
                {
                    ExitInspect();
                    return;
                }

                if (currentInspectAnchor != null)
                {
                    inspectTargetPos = currentInspectAnchor.position;
                    inspectTargetRot = currentInspectAnchor.rotation;
                }

                transform.position = Vector3.Lerp(transform.position, inspectTargetPos, Time.deltaTime * inspectTransitionSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, inspectTargetRot, Time.deltaTime * inspectTransitionSpeed);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, seatedPosition, Time.deltaTime * inspectTransitionSpeed);

                if (!eyesClosed)
                {
                    targetYaw += rotationInput * rotationSpeed * Time.deltaTime;
                }
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime);

                transform.rotation = Quaternion.Euler(seatedPitch, currentYaw, 0f);

                bool isTransitioningToSeat = !IsInspecting && Vector3.Distance(transform.position, seatedPosition) > 0.15f;
                if (leftClick && !eyesClosed && !isTransitioningToSeat)
                {
                    Check3DInteractionRaycast(mouseScreenPos);
                }
            }
        }

        private void Check3DInteractionRaycast(Vector2 screenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var interactable = hit.collider.GetComponentInParent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.TriggerInteraction();
                }
            }
        }

        public void Inspect(Transform anchor, InspectTargetType targetType)
        {
            if (anchor == null) return;
            if (SanityManager.Instance != null && SanityManager.Instance.IsClosingEyes) return;

            currentInspectAnchor = anchor;
            inspectTargetPos = anchor.position;
            inspectTargetRot = anchor.rotation;

            IsInspecting = true;
            CurrentInspectType = targetType;

            OnInspectStarted?.Invoke(targetType);
        }

        public void ExitInspect()
        {
            if (!IsInspecting) return;

            IsInspecting = false;
            CurrentInspectType = InspectTargetType.None;
            currentInspectAnchor = null;

            OnInspectEnded?.Invoke();
        }

        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = Mathf.Max(10f, speed);
        }

        public void SetSmoothTime(float smooth)
        {
            smoothTime = Mathf.Max(0.005f, smooth);
        }
    }
}
