using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    public class SettingsCategoryList : MonoBehaviour
    {
        [SerializeField] private List<SettingsCategoryButton> categoryButtons = new List<SettingsCategoryButton>();

        public Action<int> OnCategorySelected;

        private void Awake()
        {
            EnsureCategoryButtonsPopulated();
        }

        private void EnsureCategoryButtonsPopulated()
        {
            bool needsRebuild = categoryButtons == null || categoryButtons.Count == 0;
            if (!needsRebuild)
            {
                for (int i = 0; i < categoryButtons.Count; i++)
                {
                    if (categoryButtons[i] == null)
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }

            if (needsRebuild)
            {
                categoryButtons = new List<SettingsCategoryButton>(GetComponentsInChildren<SettingsCategoryButton>(true));
            }
        }

        public void BindCategories(Action<int> callback)
        {
            EnsureCategoryButtonsPopulated();
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
            EnsureCategoryButtonsPopulated();
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                if (categoryButtons[i] != null)
                    categoryButtons[i].SetSelected(i == index);
            }
            OnCategorySelected?.Invoke(index);
        }
    }
}
