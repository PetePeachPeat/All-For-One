using UnityEngine;
using UnityEngine.UI;
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

        private void Awake()
        {
            EnsureNonBlocking(gameObject);
            EnsureNonBlocking(blindnessOverlay);
            EnsureNonBlocking(blurOverlay);
        }

        private void EnsureNonBlocking(GameObject go)
        {
            if (go == null) return;

            // Make sure all UI Graphics ignore raycasts so vision obstruction doesn't block player clicks
            var graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = false;
            }

            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

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
            bool reduceFx = PlayerPrefs.GetInt("MM_ReduceDebuffFx", 0) == 1;
            float scale = reduceFx ? 0.5f : 1f;

            if (ghostType == GhostType.Threat)
            {
                if (blindnessOverlay != null)
                {
                    EnsureNonBlocking(blindnessOverlay);
                    blindnessOverlay.SetActive(true);
                    var img = blindnessOverlay.GetComponent<Image>();
                    if (img != null)
                    {
                        img.raycastTarget = false;
                        img.color = new Color(0f, 0f, 0f, 0.96f * scale);
                    }
                }
                if (blurOverlay != null) blurOverlay.SetActive(false);
            }
            else if (ghostType == GhostType.Scream)
            {
                if (blindnessOverlay != null) blindnessOverlay.SetActive(false);
                if (blurOverlay != null)
                {
                    EnsureNonBlocking(blurOverlay);
                    blurOverlay.SetActive(true);
                    var img = blurOverlay.GetComponent<Image>();
                    if (img != null)
                    {
                        img.raycastTarget = false;
                        img.color = new Color(0.75f, 0.80f, 0.85f, 0.62f * scale);
                    }
                }
            }
        }

        private void ClearDebuffVisual()
        {
            if (blindnessOverlay != null)
            {
                EnsureNonBlocking(blindnessOverlay);
                blindnessOverlay.SetActive(false);
            }
            if (blurOverlay != null)
            {
                EnsureNonBlocking(blurOverlay);
                blurOverlay.SetActive(false);
            }
        }
    }
}
