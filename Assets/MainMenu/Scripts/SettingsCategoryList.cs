using System;
using System.Collections.Generic;
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

    public class SettingsCategoryList : MonoBehaviour
    {
        [SerializeField] private List<SettingsCategoryButton> categoryButtons = new List<SettingsCategoryButton>();

        public Action<int> OnCategorySelected;

        public void BindCategories(Action<int> callback)
        {
            OnCategorySelected = callback;
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                int idx = i;
                if (categoryButtons[i] != null)
                {
                    categoryButtons[i].categoryIndex = idx;
                    categoryButtons[i].OnCategoryClicked = SelectCategory;
                }
            }
        }

        public void SelectCategory(int index)
        {
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                if (categoryButtons[i] != null)
                    categoryButtons[i].SetSelected(i == index);
            }
            OnCategorySelected?.Invoke(index);
        }
    }
}
