using UnityEngine;
using UnityEngine.Events;
using Pete.Data;

namespace Pete.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class InteractableObject : MonoBehaviour
    {
        [Header("Target Identity")]
        [SerializeField] private InspectTargetType targetType = InspectTargetType.None;
        [Tooltip("จุดที่ต้องการให้กล้องเลื่อนเข้ามามองเวลาคลิก (หากว่างไว้ จะใช้ตำแหน่งวัตถุนี้)")]
        [SerializeField] private Transform inspectAnchor;

        [Header("Shrine Specific")]
        [Tooltip("ถ้าเปิดใช้งาน จะคลิกได้เฉพาะตอนที่ผีกำลังนับถอยหลังเท่านั้น")]
        [SerializeField] private bool requiresActiveGhostCountdown = false;

        [Header("Events")]
        public UnityEvent onInteracted;

        public void TriggerInteraction()
        {
            if (CameraController.Instance != null && CameraController.Instance.IsInspecting)
                return;

            if (requiresActiveGhostCountdown)
            {
                var ghostMgr = Systems.GhostManager.Instance;
                if (ghostMgr == null || !ghostMgr.IsCountdownActive) return;

                // เปิด UI บทสวดมนต์ทันทีที่คลิกศาลเจ้า
                ghostMgr.OpenChantSelection();
                onInteracted?.Invoke();
                return;
            }

            // สั่งซูมกล้องไปยังจุดตรวจสอบ
            if (inspectAnchor != null && CameraController.Instance != null)
            {
                CameraController.Instance.Inspect(inspectAnchor, targetType);
            }

            onInteracted?.Invoke();
        }

        private void OnMouseDown()
        {
            TriggerInteraction();
        }

        public InspectTargetType TargetType => targetType;
        public Transform InspectAnchor => inspectAnchor;

        public void SetTarget(InspectTargetType type, Transform anchor = null)
        {
            targetType = type;
            inspectAnchor = anchor;
        }

        public void SetRequiresGhostCountdown(bool require)
        {
            requiresActiveGhostCountdown = require;
        }
    }
}
