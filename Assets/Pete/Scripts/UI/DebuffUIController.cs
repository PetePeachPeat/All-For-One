using UnityEngine;
using Pete.Data;
using Pete.Systems;

namespace Pete.UI
{
    public class DebuffUIController : MonoBehaviour
    {
        [Header("Debuff Overlays")]
        [Tooltip("หน้ากากมืดครึ่งจอ (ตาบอดข้างเดียว) สำหรับผีขู่")]
        [SerializeField] private GameObject blindnessOverlay;

        [Tooltip("หน้ากากจอเบลอ สำหรับผีกรี๊ด")]
        [SerializeField] private GameObject blurOverlay;

        private void Start()
        {
            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostDebuffApplied += ApplyDebuffVisual;
                GhostManager.Instance.OnGhostDebuffEnded += ClearDebuffVisual;
            }

            ClearDebuffVisual();
        }

        private void OnDestroy()
        {
            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostDebuffApplied -= ApplyDebuffVisual;
                GhostManager.Instance.OnGhostDebuffEnded -= ClearDebuffVisual;
            }
        }

        private void ApplyDebuffVisual(GhostType ghostType, float duration)
        {
            if (ghostType == GhostType.Threat)
            {
                if (blindnessOverlay != null) blindnessOverlay.SetActive(true);
                if (blurOverlay != null) blurOverlay.SetActive(false);
            }
            else if (ghostType == GhostType.Scream)
            {
                if (blindnessOverlay != null) blindnessOverlay.SetActive(false);
                if (blurOverlay != null) blurOverlay.SetActive(true);
            }
        }

        private void ClearDebuffVisual()
        {
            if (blindnessOverlay != null) blindnessOverlay.SetActive(false);
            if (blurOverlay != null) blurOverlay.SetActive(false);
        }
    }
}
