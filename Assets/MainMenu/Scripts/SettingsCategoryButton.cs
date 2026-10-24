using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SecurityCenter.MainMenu
{
    public class SettingsCategoryButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public int categoryIndex;
        public RectTransform rectTransform;
        public TMP_Text labelText;
        public Action<int> OnCategoryClicked;

        public void SetSelected(bool selected)
        {
            if (labelText != null)
            {
                labelText.color = selected
                    ? new Color(0.96f, 0.96f, 0.94f, 1f)
                    : new Color(0.45f, 0.48f, 0.50f, 0.55f);
            }
            if (rectTransform != null)
            {
                Vector2 pos = rectTransform.anchoredPosition;
                pos.x = selected ? -16f : 0f;
                rectTransform.anchoredPosition = pos;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_hover_tick", 0.04f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnCategoryClicked?.Invoke(categoryIndex);
        }
    }
}
