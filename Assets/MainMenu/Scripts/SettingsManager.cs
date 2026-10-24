using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class SettingsManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text breadcrumbText;
        [SerializeField] private Extrude3DText header3DText;
        [SerializeField] private TypewriterText descriptionTypewriter;
        [SerializeField] private SettingsCategoryList categoryList;
        [SerializeField] private List<SettingsRow> rows = new List<SettingsRow>();
        [SerializeField] private CRTStaticController staticController;
        [SerializeField] private Image brightnessOverlayImage;
        [SerializeField] private GameObject shrineCalibrationBox;
        [SerializeField] private Image shrineSilhouetteImage;

        private int _currentCategoryIndex;
        private int _currentRowIndex;
        private int _activeRowCount;

        private static readonly string[] CategoryNames =
        {
            "VIDEO",
            "AUDIO",
            "CONTROLS",
            "GAMEPLAY",
            "ACCESSIBILITY"
        };

        private void Awake()
        {
            EnsureDefaultsExist();
            if (categoryList != null)
                categoryList.BindCategories(OnCategoryChosen);

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].OnRowHovered = SelectRow;
                    rows[i].OnRowStep = (rIdx, dir) =>
                    {
                        SelectRow(rIdx);
                        StepCurrentRow(dir);
                    };
                    rows[i].OnRowChanged = rIdx =>
                    {
                        SelectRow(rIdx);
                        ApplyAllSettingsToRuntime();
                    };
                }
            }
        }

        private void Start()
        {
            ApplyAllSettingsToRuntime();
        }

        public void OpenSettings()
        {
            SelectCategory(0);
        }

        public void SelectCategory(int catIndex)
        {
            _currentCategoryIndex = (catIndex + CategoryNames.Length) % CategoryNames.Length;
            if (categoryList != null)
                categoryList.SelectCategory(_currentCategoryIndex);
            else
                OnCategoryChosen(_currentCategoryIndex);
        }

        public void StepCategory(int delta)
        {
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_hover_tick", 0.03f);
            SelectCategory(_currentCategoryIndex + delta);
        }

        private void OnCategoryChosen(int catIndex)
        {
            _currentCategoryIndex = catIndex;
            if (breadcrumbText != null)
                breadcrumbText.text = $"MAIN MENU  /  SETTINGS  /  {CategoryNames[catIndex]}";

            PopulateRowsForCategory(catIndex);
            SelectRow(0);
        }

        public void StepRowSelection(int delta)
        {
            if (_activeRowCount <= 0) return;
            int next = (_currentRowIndex + delta + _activeRowCount) % _activeRowCount;
            SelectRow(next);
        }

        public void SelectRow(int rowIdx)
        {
            if (_activeRowCount <= 0) return;
            int clamped = Mathf.Clamp(rowIdx, 0, _activeRowCount - 1);
            if (clamped != _currentRowIndex && MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_hover_tick", 0.04f);

            _currentRowIndex = clamped;
            for (int i = 0; i < _activeRowCount; i++)
            {
                if (rows[i] != null)
                    rows[i].SetSelected(i == _currentRowIndex);
            }

            if (rows[_currentRowIndex] != null && descriptionTypewriter != null)
            {
                descriptionTypewriter.PlayText(rows[_currentRowIndex].Description, 0.014f);
            }

            bool showCalib = (_currentCategoryIndex == 0 && _currentRowIndex == 0);
            if (shrineCalibrationBox != null)
                shrineCalibrationBox.SetActive(showCalib);
        }

        public void StepCurrentRow(int dir)
        {
            if (_currentRowIndex >= 0 && _currentRowIndex < _activeRowCount && rows[_currentRowIndex] != null)
            {
                rows[_currentRowIndex].StepValue(dir);
                ApplyAllSettingsToRuntime();
            }
        }

        public void ResetAllToDefaults()
        {
            PlayerPrefs.SetInt("MM_Brightness", 50);
            PlayerPrefs.SetInt("MM_CRTFx", 2);
            PlayerPrefs.SetInt("MM_Grain", 1);
            PlayerPrefs.SetInt("MM_FullscreenMode", 0);
            PlayerPrefs.SetInt("MM_ResIndex", 0);
            PlayerPrefs.SetInt("MM_VSync", 1);
            PlayerPrefs.SetInt("MM_FPS", 1);

            PlayerPrefs.SetInt("MM_VolMaster", 80);
            PlayerPrefs.SetInt("MM_VolMusic", 70);
            PlayerPrefs.SetInt("MM_VolSFX", 80);
            PlayerPrefs.SetInt("MM_VolGhost", 90);

            PlayerPrefs.SetInt("MM_TurnSens", 10);
            PlayerPrefs.SetInt("MM_TurnSmooth", 1);
            PlayerPrefs.SetInt("MM_RightClickBack", 1);

            PlayerPrefs.SetInt("MM_Lang", 0);
            PlayerPrefs.SetInt("MM_Subs", 1);
            PlayerPrefs.SetInt("MM_SubSize", 1);
            PlayerPrefs.SetInt("MM_Shake", 100);

            PlayerPrefs.SetInt("MM_PhotoSafe", 0);
            PlayerPrefs.SetInt("MM_JumpCap", 0);
            PlayerPrefs.SetInt("MM_ReduceDebuffFx", 0);
            PlayerPrefs.Save();

            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select", 0.02f);
            PopulateRowsForCategory(_currentCategoryIndex);
            SelectRow(_currentRowIndex);
            ApplyAllSettingsToRuntime();
        }

        private void EnsureDefaultsExist()
        {
            if (!PlayerPrefs.HasKey("MM_Brightness"))
            {
                ResetAllToDefaults();
            }
        }

        private void PopulateRowsForCategory(int catIndex)
        {
            foreach (var r in rows)
            {
                if (r != null) r.gameObject.SetActive(false);
            }

            if (catIndex == 0) // VIDEO
            {
                _activeRowCount = 7;
                EnableRow(0).ConfigureSlider(0, "BRIGHTNESS", "MM_Brightness", 0, 100, 5, 50, "");
                EnableRow(1).ConfigureChoice(1, "CRT / SCANLINE FX", "MM_CRTFx", new[] { "OFF", "LOW", "HIGH" }, 2, "");
                EnableRow(2).ConfigureChoice(2, "FILM GRAIN", "MM_Grain", new[] { "OFF", "ON" }, 1, "");
                EnableRow(3).ConfigureChoice(3, "DISPLAY MODE", "MM_FullscreenMode", new[] { "FULLSCREEN", "WINDOWED", "BORDERLESS" }, 0, "");
                EnableRow(4).ConfigureChoice(4, "RESOLUTION", "MM_ResIndex", new[] { "1920×1080", "1600×900", "1280×720" }, 0, "");
                EnableRow(5).ConfigureChoice(5, "V-SYNC", "MM_VSync", new[] { "OFF", "ON" }, 1, "");
                EnableRow(6).ConfigureChoice(6, "FPS LIMIT", "MM_FPS", new[] { "30 FPS", "60 FPS", "120 FPS", "UNLIMITED" }, 1, "");
            }
            else if (catIndex == 1) // AUDIO
            {
                _activeRowCount = 4;
                EnableRow(0).ConfigureSlider(0, "MASTER VOLUME", "MM_VolMaster", 0, 100, 10, 80, "");
                EnableRow(1).ConfigureSlider(1, "AMBIENCE VOLUME", "MM_VolMusic", 0, 100, 10, 70, "");
                EnableRow(2).ConfigureSlider(2, "SFX VOLUME", "MM_VolSFX", 0, 100, 10, 80, "");
                EnableRow(3).ConfigureSlider(3, "GHOST & PHONE VOLUME", "MM_VolGhost", 0, 100, 10, 90, "");
            }
            else if (catIndex == 2) // CONTROLS
            {
                _activeRowCount = 4;
                EnableRow(0).ConfigureSlider(0, "CAMERA SENSITIVITY (A/D)", "MM_TurnSens", 5, 20, 1, 10, "");
                EnableRow(1).ConfigureChoice(1, "CAMERA SMOOTHING", "MM_TurnSmooth", new[] { "LOW", "MEDIUM", "HIGH" }, 1, "");
                EnableRow(2).ConfigureChoice(2, "RIGHT-CLICK TO BACK OUT", "MM_RightClickBack", new[] { "OFF", "ON" }, 1, "");
                EnableRow(3).ConfigureReadOnly(3, "KEYBINDINGS", "[A/D] TURN   [LMB] INTERACT   [ESC/RMB] BACK", "");
            }
            else if (catIndex == 3) // GAMEPLAY
            {
                _activeRowCount = 4;
                EnableRow(0).ConfigureChoice(0, "LANGUAGE", "MM_Lang", new[] { "ENGLISH", "THAI" }, 0, "");
                EnableRow(1).ConfigureChoice(1, "PHONE SUBTITLES", "MM_Subs", new[] { "OFF", "ON" }, 1, "");
                EnableRow(2).ConfigureChoice(2, "SUBTITLE SIZE", "MM_SubSize", new[] { "SMALL", "MEDIUM", "LARGE" }, 1, "");
                EnableRow(3).ConfigureSlider(3, "SCREEN SHAKE", "MM_Shake", 0, 100, 10, 100, "");
            }
            else // ACCESSIBILITY
            {
                _activeRowCount = 3;
                EnableRow(0).ConfigureChoice(0, "PHOTOSENSITIVE SAFE MODE", "MM_PhotoSafe", new[] { "OFF", "ON" }, 0, "");
                EnableRow(1).ConfigureChoice(1, "JUMPSCARE VOLUME CAP", "MM_JumpCap", new[] { "OFF", "ON" }, 0, "");
                EnableRow(2).ConfigureChoice(2, "REDUCE GHOST DEBUFF FX", "MM_ReduceDebuffFx", new[] { "OFF", "ON" }, 0, "");
            }
        }

        private SettingsRow EnableRow(int idx)
        {
            var r = rows[idx];
            r.gameObject.SetActive(true);
            return r;
        }

        public void ApplyAllSettingsToRuntime()
        {
            // Brightness overlay (-35% dark to +25% bright)
            int bright = PlayerPrefs.GetInt("MM_Brightness", 50);
            if (brightnessOverlayImage != null)
            {
                if (bright < 50)
                {
                    float a = Mathf.Lerp(0.55f, 0f, bright / 50f);
                    brightnessOverlayImage.color = new Color(0f, 0f, 0f, a);
                }
                else
                {
                    float a = Mathf.Lerp(0f, 0.22f, (bright - 50f) / 50f);
                    brightnessOverlayImage.color = new Color(0.95f, 0.96f, 0.94f, a);
                }
            }
            if (shrineSilhouetteImage != null)
            {
                float vis = Mathf.Clamp01(bright / 65f) * 0.55f;
                shrineSilhouetteImage.color = new Color(0.88f, 0.88f, 0.85f, vis);
            }

            if (staticController != null)
                staticController.ApplyPrefsQuality();
            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.ApplyVolumesFromPrefs();

            int vsync = PlayerPrefs.GetInt("MM_VSync", 1);
            QualitySettings.vSyncCount = vsync;

            int fpsChoice = PlayerPrefs.GetInt("MM_FPS", 1);
            Application.targetFrameRate = fpsChoice == 0 ? 30 : (fpsChoice == 1 ? 60 : (fpsChoice == 2 ? 120 : -1));
        }
    }
}
