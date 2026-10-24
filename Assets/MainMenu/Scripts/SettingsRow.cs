using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public enum SettingsRowType
    {
        Choice,
        Slider,
        ReadOnly
    }

    public class SettingsRow : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] private int rowIndex;
        [SerializeField] private Image highlightBar;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private ScrambleText scrambleLabel;

        public Action<int> OnRowHovered;
        public Action<int, int> OnRowStep; // (rowIndex, delta -1 or +1)
        public Action<int> OnRowChanged;

        private SettingsRowType _type;
        private string _prefKey;
        private string[] _options;
        private int _minVal;
        private int _maxVal;
        private int _stepVal;
        private int _currentIntValue;
        private int _defaultIntValue;
        private string _description;
        private string _readOnlyValue;
        private bool _isSelected;

        private static readonly StringBuilder SliderSb = new StringBuilder(32);

        public string Description => _description;
        public string PrefKey => _prefKey;

        public void ConfigureChoice(int index, string label, string prefKey, string[] options, int defaultIndex, string desc)
        {
            rowIndex = index;
            _type = SettingsRowType.Choice;
            _prefKey = prefKey;
            _options = options;
            _defaultIntValue = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, options.Length - 1));
            _currentIntValue = Mathf.Clamp(PlayerPrefs.GetInt(prefKey, _defaultIntValue), 0, Mathf.Max(0, options.Length - 1));
            _description = desc;

            SetLabelWithScramble(label);
            RefreshValueDisplay();
        }

        public void ConfigureSlider(int index, string label, string prefKey, int minVal, int maxVal, int stepVal, int defaultVal, string desc)
        {
            rowIndex = index;
            _type = SettingsRowType.Slider;
            _prefKey = prefKey;
            _minVal = minVal;
            _maxVal = maxVal;
            _stepVal = Mathf.Max(1, stepVal);
            _defaultIntValue = Mathf.Clamp(defaultVal, minVal, maxVal);
            _currentIntValue = Mathf.Clamp(PlayerPrefs.GetInt(prefKey, _defaultIntValue), minVal, maxVal);
            _description = desc;

            SetLabelWithScramble(label);
            RefreshValueDisplay();
        }

        public void ConfigureReadOnly(int index, string label, string displayValue, string desc)
        {
            rowIndex = index;
            _type = SettingsRowType.ReadOnly;
            _prefKey = "";
            _readOnlyValue = displayValue;
            _description = desc;

            SetLabelWithScramble(label);
            RefreshValueDisplay();
        }

        private void SetLabelWithScramble(string label)
        {
            if (scrambleLabel != null && gameObject.activeInHierarchy)
                scrambleLabel.DecodeTo(label);
            else if (labelText != null)
                labelText.text = label;
        }

        public void StepValue(int dir)
        {
            if (_type == SettingsRowType.ReadOnly) return;

            if (_type == SettingsRowType.Choice && _options != null && _options.Length > 0)
            {
                _currentIntValue = (_currentIntValue + dir + _options.Length) % _options.Length;
                PlayerPrefs.SetInt(_prefKey, _currentIntValue);
            }
            else if (_type == SettingsRowType.Slider)
            {
                _currentIntValue = Mathf.Clamp(_currentIntValue + dir * _stepVal, _minVal, _maxVal);
                PlayerPrefs.SetInt(_prefKey, _currentIntValue);
            }

            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_value_tick", 0.04f);

            RefreshValueDisplay();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (highlightBar != null)
                highlightBar.color = selected ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0f, 0f, 0f, 0f);
            if (labelText != null)
                labelText.color = selected ? new Color(0.04f, 0.04f, 0.04f, 1f) : new Color(0.82f, 0.85f, 0.86f, 1f);
            if (valueText != null)
                valueText.color = selected ? new Color(0.04f, 0.04f, 0.04f, 1f) : new Color(0.91f, 0.90f, 0.88f, 1f);
            RefreshValueDisplay();
        }

        private void RefreshValueDisplay()
        {
            if (valueText == null) return;

            if (_type == SettingsRowType.ReadOnly)
            {
                valueText.text = _readOnlyValue;
                return;
            }

            if (_type == SettingsRowType.Choice && _options != null && _options.Length > 0)
            {
                string opt = _options[Mathf.Clamp(_currentIntValue, 0, _options.Length - 1)];
                valueText.text = _isSelected ? $"◄  {opt}  ►" : opt;
            }
            else if (_type == SettingsRowType.Slider)
            {
                float ratio = _maxVal > _minVal ? (float)(_currentIntValue - _minVal) / (_maxVal - _minVal) : 0f;
                int filled = Mathf.RoundToInt(ratio * 10f);
                SliderSb.Clear();
                if (_isSelected) SliderSb.Append("◄ ");
                for (int i = 0; i < 10; i++)
                    SliderSb.Append(i < filled ? '█' : '░');
                SliderSb.Append(' ').Append(_currentIntValue);
                if (_isSelected) SliderSb.Append(" ►");
                valueText.text = SliderSb.ToString();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnRowHovered?.Invoke(rowIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRowStep?.Invoke(rowIndex, -1);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;

            var rt = transform as RectTransform;
            if (rt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 localPt))
            {
                float nx = (localPt.x - rt.rect.xMin) / Mathf.Max(1f, rt.rect.width);

                if (_type == SettingsRowType.Slider)
                {
                    // Left arrow region (< 0.72), slider bar region (0.72..0.90), right arrow region (> 0.90)
                    if (nx < 0.73f)
                    {
                        OnRowStep?.Invoke(rowIndex, -1);
                        return;
                    }
                    if (nx > 0.91f)
                    {
                        OnRowStep?.Invoke(rowIndex, 1);
                        return;
                    }
                    SetSliderFromNormalized(Mathf.InverseLerp(0.73f, 0.90f, nx));
                    return;
                }

                // For Choice rows: clicking left half of the value column steps -1, right half steps +1
                if (nx >= 0.55f && nx < 0.78f)
                {
                    OnRowStep?.Invoke(rowIndex, -1);
                    return;
                }
            }

            OnRowStep?.Invoke(rowIndex, 1);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_type != SettingsRowType.Slider) return;
            var rt = transform as RectTransform;
            if (rt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 localPt))
            {
                float nx = (localPt.x - rt.rect.xMin) / Mathf.Max(1f, rt.rect.width);
                SetSliderFromNormalized(Mathf.InverseLerp(0.73f, 0.90f, nx));
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Mathf.Abs(eventData.scrollDelta.y) > 0.01f)
            {
                OnRowStep?.Invoke(rowIndex, eventData.scrollDelta.y > 0f ? 1 : -1);
            }
        }

        private void SetSliderFromNormalized(float t)
        {
            if (_type != SettingsRowType.Slider) return;
            int raw = Mathf.RoundToInt(Mathf.Lerp(_minVal, _maxVal, Mathf.Clamp01(t)));
            int stepped = _minVal + Mathf.RoundToInt((float)(raw - _minVal) / _stepVal) * _stepVal;
            stepped = Mathf.Clamp(stepped, _minVal, _maxVal);
            if (stepped != _currentIntValue)
            {
                _currentIntValue = stepped;
                PlayerPrefs.SetInt(_prefKey, _currentIntValue);
                if (MenuAudioManager.Instance != null)
                    MenuAudioManager.Instance.Play("ui_value_tick", 0.04f);
                RefreshValueDisplay();
                OnRowChanged?.Invoke(rowIndex);
            }
        }
    }
}
