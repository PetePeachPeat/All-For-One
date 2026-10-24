using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Pete.UI
{
    public class FolderHoverZoomFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform iconContainer;
        public Image cardBorder;
        public Image cardBackground;
        public TextMeshProUGUI folderLabel;

        private bool isHovered;
        private Vector3 baseScale = Vector3.one;
        private Vector2 baseAnchoredPos;
        private bool initialized;

        private void Awake()
        {
            InitIfNeeded();
        }

        private void InitIfNeeded()
        {
            if (initialized) return;
            initialized = true;
            if (iconContainer != null)
            {
                baseScale = iconContainer.localScale;
                baseAnchoredPos = iconContainer.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            InitIfNeeded();
            isHovered = false;
            if (iconContainer != null)
            {
                iconContainer.localScale = baseScale;
                iconContainer.anchoredPosition = baseAnchoredPos;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (cardBorder != null) cardBorder.color = new Color(0.53f, 0.94f, 0.67f, 1f);
            if (cardBackground != null) cardBackground.color = new Color(0.03f, 0.18f, 0.09f, 0.92f);
            if (folderLabel != null) folderLabel.color = Color.white;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (cardBorder != null) cardBorder.color = new Color(0.13f, 0.77f, 0.37f, 0.85f);
            if (cardBackground != null) cardBackground.color = new Color(0.015f, 0.10f, 0.05f, 0.82f);
            if (folderLabel != null) folderLabel.color = new Color(0.29f, 0.87f, 0.50f, 1f);
        }

        private void Update()
        {
            if (iconContainer == null) return;
            Vector3 targetScale = isHovered ? baseScale * 1.34f : baseScale;
            Vector2 targetPos = isHovered ? baseAnchoredPos + new Vector2(0f, 8f) : baseAnchoredPos;
            iconContainer.localScale = Vector3.Lerp(iconContainer.localScale, targetScale, Time.unscaledDeltaTime * 14f);
            iconContainer.anchoredPosition = Vector2.Lerp(iconContainer.anchoredPosition, targetPos, Time.unscaledDeltaTime * 14f);
        }
    }
}
