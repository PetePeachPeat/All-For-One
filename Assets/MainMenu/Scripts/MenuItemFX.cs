using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class MenuItemFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private int itemIndex;
        [SerializeField] private RectTransform barRect;
        [SerializeField] private Image barImage;
        [SerializeField] private RectTransform labelRect;
        [SerializeField] private TMP_Text labelText;
        [TextArea(2, 3)]
        [SerializeField] private string descriptionText;
        [SerializeField] private bool isDanger;
        [SerializeField] private bool interactable = true;

        public Action<int> OnHovered;
        public Action<int> OnClicked;

        public int ItemIndex
        {
            get => itemIndex;
            set => itemIndex = value;
        }

        public string DescriptionText
        {
            get => descriptionText;
            set => descriptionText = value;
        }
        public bool IsDanger => isDanger;
        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                ApplyVisualImmediate(_isSelected);
            }
        }

        private bool _isSelected;
        private Coroutine _animRoutine;

        private static readonly Color ColorTextNormal = new Color(0.52f, 0.52f, 0.52f, 1f);
        private static readonly Color ColorTextDisabled = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color ColorTextSelectedDark = new Color(0.04f, 0.04f, 0.04f, 1f);
        private static readonly Color ColorTextSelectedWhite = new Color(0.96f, 0.96f, 0.94f, 1f);
        private static readonly Color ColorBarSelect = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColorBarDanger = new Color(0.90f, 0.10f, 0.10f, 1f);
        private static readonly Color ColorBarDisabled = new Color(0.23f, 0.23f, 0.23f, 0.85f);

        public void Setup(int idx, string title, string desc, bool danger = false, bool canInteract = true)
        {
            itemIndex = idx;
            if (labelText != null) labelText.text = title;
            descriptionText = desc;
            isDanger = danger;
            interactable = canInteract;
            ApplyVisualImmediate(false);
        }

        public void SetSelected(bool selected, bool animate = true)
        {
            _isSelected = selected;
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            if (!animate || !gameObject.activeInHierarchy)
            {
                ApplyVisualImmediate(selected);
            }
            else
            {
                _animRoutine = StartCoroutine(AnimateSelectRoutine(selected));
            }
        }

        public void PlayPressFlash(Action onDone)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(PressFlashRoutine(onDone));
        }

        private void ApplyVisualImmediate(bool selected)
        {
            if (barRect != null)
            {
                barRect.pivot = new Vector2(0f, 0.5f);
                barRect.localScale = new Vector3(selected ? 1f : 0f, 1f, 1f);
            }
            if (barImage != null)
            {
                if (!interactable) barImage.color = ColorBarDisabled;
                else if (isDanger) barImage.color = ColorBarDanger;
                else barImage.color = ColorBarSelect;
            }
            if (labelRect != null)
            {
                labelRect.anchoredPosition = new Vector2(selected ? 22f : 8f, 0f);
            }
            if (labelText != null)
            {
                if (!interactable)
                    labelText.color = selected ? new Color(0.68f, 0.68f, 0.68f, 1f) : ColorTextDisabled;
                else if (selected)
                    labelText.color = isDanger ? ColorTextSelectedWhite : ColorTextSelectedDark;
                else
                    labelText.color = ColorTextNormal;
            }
        }

        private IEnumerator AnimateSelectRoutine(bool selected)
        {
            float dur = selected ? 0.12f : 0.08f;
            float startScale = barRect != null ? barRect.localScale.x : 0f;
            float targetScale = selected ? 1f : 0f;
            float startX = labelRect != null ? labelRect.anchoredPosition.x : 8f;
            float targetX = selected ? 22f : 8f;

            ApplyVisualImmediate(selected);
            if (barRect != null) barRect.localScale = new Vector3(startScale, 1f, 1f);
            if (labelRect != null) labelRect.anchoredPosition = new Vector2(startX, 0f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                if (barRect != null)
                    barRect.localScale = new Vector3(Mathf.Lerp(startScale, targetScale, ease), 1f, 1f);
                if (labelRect != null)
                    labelRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, ease), 0f);
                yield return null;
            }
            ApplyVisualImmediate(selected);
        }

        private IEnumerator PressFlashRoutine(Action onDone)
        {
            for (int i = 0; i < 2; i++)
            {
                if (barImage != null) barImage.enabled = false;
                if (labelText != null) labelText.color = ColorTextSelectedWhite;
                yield return new WaitForSecondsRealtime(0.045f);
                if (barImage != null) barImage.enabled = true;
                ApplyVisualImmediate(true);
                yield return new WaitForSecondsRealtime(0.045f);
            }
            onDone?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(itemIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnClicked?.Invoke(itemIndex);
        }
    }
}
