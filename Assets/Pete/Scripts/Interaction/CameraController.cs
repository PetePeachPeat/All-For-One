using System;
using UnityEngine;
using Pete.Data;

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
        [SerializeField] private float rotationSpeed = 90f;
        [Tooltip("เวลาในการชะลอการหมุนให้นุ่มนวล (Smooth Damping)")]
        [SerializeField] private float smoothTime = 0.12f;

        [Header("Seated View Reference")]
        [SerializeField] private Vector3 seatedPosition = new Vector3(0.03f, 1.31f, -9.257f);
        [SerializeField] private float seatedPitch = 0f; // มุมก้ม/เงยคงที่เวลานั่ง

        [Header("Inspect / Zoom Settings")]
        [SerializeField] private float inspectTransitionSpeed = 8f;

        // Runtime states
        private float currentYaw;
        private float targetYaw;
        private float yawVelocity;

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
            else if (Instance != this) Destroy(gameObject);

            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            // เริ่มต้นจากมุมมองปัจจุบันของกล้อง
            seatedPosition = transform.position;
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            targetYaw = currentYaw;
            seatedPitch = angles.x;
        }

        private void Update()
        {
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
                if (mouse.rightButton.wasPressedThisFrame) exitRequested = true;
                if (mouse.leftButton.wasPressedThisFrame) leftClick = true;
                mouseScreenPos = mouse.position.ReadValue();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) exitRequested = true;
            if (Input.GetKey(KeyCode.A)) rotationInput -= 1f;
            if (Input.GetKey(KeyCode.D)) rotationInput += 1f;
            if (Input.GetMouseButtonDown(0)) leftClick = true;
            mouseScreenPos = Input.mousePosition;
#endif

            // จัดการการซูมและการออกจากซูม
            if (IsInspecting)
            {
                if (exitRequested)
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
                // เลื่อนกล้องกลับสู่ตำแหน่งนั่งปกติ
                transform.position = Vector3.Lerp(transform.position, seatedPosition, Time.deltaTime * inspectTransitionSpeed);

                // หมุนกล้องตามปุ่ม A / D
                targetYaw += rotationInput * rotationSpeed * Time.deltaTime;
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime);

                transform.rotation = Quaternion.Euler(seatedPitch, currentYaw, 0f);

                // ตรวจสอบการคลิกเมาส์ซ้ายเพื่อโต้ตอบวัตถุ 3D (New Input System Raycast)
                if (leftClick)
                {
                    Check3DInteractionRaycast(mouseScreenPos);
                }
            }
        }

        private void Check3DInteractionRaycast(Vector2 screenPos)
        {
            // ถ้าคลิกโดน UI จะไม่ยิง Raycast ทะลุไปโดนวัตถุ 3D
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

        /// <summary>
        /// ซูมกล้องไปยังจุดตรวจสอบ
        /// </summary>
        public void Inspect(Transform anchor, InspectTargetType targetType)
        {
            if (anchor == null) return;

            currentInspectAnchor = anchor;
            inspectTargetPos = anchor.position;
            inspectTargetRot = anchor.rotation;

            IsInspecting = true;
            CurrentInspectType = targetType;

            OnInspectStarted?.Invoke(targetType);
        }

        /// <summary>
        /// ออกจากโหมดซูม กลับสู่ท่านั่งหมุนกล้องปกติ
        /// </summary>
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
    }
}
