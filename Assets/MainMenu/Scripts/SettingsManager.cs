using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SecurityCenter.MainMenu
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text breadcrumbText;
        [SerializeField] private Extrude3DText header3DText;
        [SerializeField] private TypewriterText descriptionTypewriter;
        [SerializeField] private SettingsCategoryList categoryList;
        [SerializeField] private List<SettingsRow> rows = new List<SettingsRow>();
        [SerializeField] private Button backButton;

        [Header("Visual Feedback Links")]
        [SerializeField] private CRTStaticController staticController;
        [SerializeField] private Image brightnessOverlayImage;
        [SerializeField] private GameObject shrineCalibrationBox;
        [SerializeField] private Image shrineSilhouetteImage;

        public Action OnBackRequested;

        private int _currentCategory;
        private int _selectedRow;
        private int _activeRowCount;
        private bool _isOpen;

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
            if (categoryList != null)
                categoryList.BindCategories(SelectCategory);

            if (backButton != null)
                backButton.onClick.AddListener(CloseSettings);

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].OnRowHovered = OnRowHover;
                    rows[i].OnRowStep = OnRowStep;
                    rows[i].OnRowChanged = OnRowDirectChange;
                }
            }

            ApplyAllSettingsToRuntime();
        }

        public void OpenSettings()
        {
            _isOpen = true;
            SelectCategory(0);
        }

        public void CloseSettings()
        {
            if (!_isOpen) return;
            _isOpen = false;
            PlayerPrefs.Save();
            OnBackRequested?.Invoke();
        }

        private void Update()
        {
            if (!_isOpen) return;

            if (WasPressed(Key.Escape) || WasRightClicked())
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_back");
                CloseSettings();
                return;
            }

            // Category switching: Q / E
            if (WasPressed(Key.Q))
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select", 0.03f);
                SelectCategory((_currentCategory - 1 + CategoryNames.Length) % CategoryNames.Length);
            }
            else if (WasPressed(Key.E))
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select", 0.03f);
                SelectCategory((_currentCategory + 1) % CategoryNames.Length);
            }

            // Row navigation: W / S or Up / Down
            if (_activeRowCount > 0)
            {
                if (WasPressed(Key.W) || WasPressed(Key.UpArrow))
                {
                    if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_hover_tick", 0.03f);
                    SelectRow((_selectedRow - 1 + _activeRowCount) % _activeRowCount);
                }
                else if (WasPressed(Key.S) || WasPressed(Key.DownArrow))
                {
                    if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_hover_tick", 0.03f);
                    SelectRow((_selectedRow + 1) % _activeRowCount);
                }

                // Value adjustment: A / D or Left / Right
                if (WasPressed(Key.A) || WasPressed(Key.LeftArrow))
                {
                    OnRowStep(_selectedRow, -1);
                }
                else if (WasPressed(Key.D) || WasPressed(Key.RightArrow))
                {
                    OnRowStep(_selectedRow, 1);
                }
            }
        }

        public void SelectCategory(int catIndex)
        {
            _currentCategory = Mathf.Clamp(catIndex, 0, CategoryNames.Length - 1);
            if (categoryList != null)
            {
                // Update visual state of category buttons without re-triggering callback loop
                var cb = categoryList.OnCategorySelected;
                categoryList.OnCategorySelected = null;
                categoryList.SelectCategory(_currentCategory);
                categoryList.OnCategorySelected = cb;
            }

            if (breadcrumbText != null)
                breadcrumbText.text = $"OPTIONS  /  {CategoryNames[_currentCategory]}  (Q / E)";

            if (shrineCalibrationBox != null)
                shrineCalibrationBox.SetActive(_currentCategory == 0);

            PopulateCategoryRows(_currentCategory);
            SelectRow(0);
        }

        private void PopulateCategoryRows(int cat)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null) rows[i].gameObject.SetActive(false);

            switch (cat)
            {
                case 0: // VIDEO
                    _activeRowCount = 7;
                    SetupRowSlider(0, "BRIGHTNESS", "MM_Brightness", 0, 100, 5, 50,
                        "Adjust overall room & monitor brightness so the shrine silhouette is barely visible.");
                    SetupRowChoice(1, "CRT / SCANLINE EFFECT", "MM_CRTFx", new[] { "OFF", "LOW", "HIGH" }, 2,
                        "Controls horizontal CRT scanline intensity across the screen and PC monitor.");
                    SetupRowChoice(2, "FILM GRAIN", "MM_Grain", new[] { "OFF", "ON" }, 1,
                        "Enables analog security camera static grain across the dark office.");
                    SetupRowChoice(3, "DISPLAY MODE", "MM_FullscreenMode", new[] { "FULLSCREEN", "WINDOWED", "BORDERLESS" }, 0,
                        "Switch between exclusive fullscreen, windowed, or borderless window.");
                    SetupRowChoice(4, "RESOLUTION", "MM_ResIndex", new[] { "1920x1080", "1600x900", "1280x720" }, 0,
                        "Target render resolution for the security monitor and room view.");
                    SetupRowChoice(5, "V-SYNC", "MM_VSync", new[] { "OFF", "ON" }, 1,
                        "Synchronize frame rate with monitor refresh rate to prevent tearing.");
                    SetupRowChoice(6, "FPS LIMIT", "MM_FpsLimit", new[] { "30 FPS", "60 FPS", "120 FPS", "UNLIMITED" }, 1,
                        "Cap maximum frame rate for consistent late-night performance.");
                    break;

                case 1: // AUDIO
                    _activeRowCount = 4;
                    SetupRowSlider(0, "MASTER VOLUME", "MM_VolMaster", 0, 100, 5, 80,
                        "Global master audio output level for the entire game.");
                    SetupRowSlider(1, "AMBIENCE & HUM VOLUME", "MM_VolMusic", 0, 100, 5, 70,
                        "Volume of fluorescent hum, midnight room tone, and background ambience.");
                    SetupRowSlider(2, "UI & PC TERMINAL SFX", "MM_VolSFX", 0, 100, 5, 85,
                        "Volume of keyboard clicks, email alerts, and breathing sounds.");
                    SetupRowSlider(3, "GHOST & PHONE RING VOLUME", "MM_VolGhost", 0, 100, 5, 90,
                        "Volume of incoming phone calls, ghost whispers, screams, and heartbeat.");
                    break;

                case 2: // CONTROLS
                    _activeRowCount = 7;
                    SetupRowSlider(0, "CAMERA SENSITIVITY (A/D)", "MM_TurnSens", 1, 20, 1, 10,
                        "Horizontal rotation speed when pressing A or D to turn toward the shrine.");
                    SetupRowChoice(1, "CAMERA SMOOTHING", "MM_TurnSmooth", new[] { "OFF", "LOW", "HIGH" }, 1,
                        "Damping smoothness when turning the camera between desk and shrine.");
                    SetupRowChoice(2, "RIGHT-CLICK TO BACK OUT", "MM_RightClickBack", new[] { "OFF", "ON" }, 1,
                        "Allow right mouse button to quickly exit zoomed-in PC or Phone view.");
                    SetupRowReadOnly(3, "TURN CAMERA LEFT / RIGHT", "[ A / D ]",
                        "Hold A or D to rotate your chair between the desk and the shrine behind you.");
                    SetupRowReadOnly(4, "CLOSE EYES (RESTORE SANITY)", "[ HOLD SPACE ]",
                        "Hold SPACEBAR to close your eyes and slowly recover Sanity.");
                    SetupRowReadOnly(5, "INTERACT (PC / PHONE / SHRINE)", "[ LEFT CLICK ]",
                        "Click on the monitor, telephone, sticky note, or shrine to interact.");
                    SetupRowReadOnly(6, "EXIT ZOOM / BACK", "[ ESC / RIGHT CLICK ]",
                        "Step back from the PC monitor or hang up view.");
                    break;

                case 3: // GAMEPLAY
                    _activeRowCount = 2;
                    SetupRowChoice(0, "TEXT SPEED", "MM_TextSpeed", new[] { "NORMAL", "FAST", "INSTANT" }, 1,
                        "Speed at which incoming phone dialogue and terminal prompts appear.");
                    SetupRowChoice(1, "JUMPSCARE VOLUME CAP", "MM_JumpCap", new[] { "OFF", "ON" }, 0,
                        "Caps peak volume during sudden ghost scream events to 50% for comfort.");
                    break;

                case 4: // ACCESSIBILITY
                    _activeRowCount = 3;
                    SetupRowChoice(0, "FONT SIZE BOOST", "MM_FontBoost", new[] { "100%", "115%", "130%" }, 0,
                        "Increases readability of small email body text and sticky note rules.");
                    SetupRowChoice(1, "HIGH-CONTRAST URL HIGHLIGHT", "MM_UrlHighlight", new[] { "OFF", "ON" }, 0,
                        "Highlights domain names inside website links to help spot phishing URLs.");
                    SetupRowChoice(2, "REDUCE GHOST DEBUFF FX", "MM_ReduceDebuffFx", new[] { "OFF", "ON" }, 0,
                        "Softens the intensity of half-blindness and screen blur debuffs by 50%.");
                    break;
            }
        }

        private void SetupRowChoice(int idx, string label, string key, string[] opts, int defIdx, string desc)
        {
            if (idx >= rows.Count || rows[idx] == null) return;
            rows[idx].gameObject.SetActive(true);
            rows[idx].ConfigureChoice(idx, label, key, opts, defIdx, desc);
        }

        private void SetupRowSlider(int idx, string label, string key, int min, int max, int step, int defVal, string desc)
        {
            if (idx >= rows.Count || rows[idx] == null) return;
            rows[idx].gameObject.SetActive(true);
            rows[idx].ConfigureSlider(idx, label, key, min, max, step, defVal, desc);
        }

        private void SetupRowReadOnly(int idx, string label, string val, string desc)
        {
            if (idx >= rows.Count || rows[idx] == null) return;
            rows[idx].gameObject.SetActive(true);
            rows[idx].ConfigureReadOnly(idx, label, val, desc);
        }

        private void SelectRow(int rowIndex)
        {
            _selectedRow = Mathf.Clamp(rowIndex, 0, Mathf.Max(0, _activeRowCount - 1));
            for (int i = 0; i < _activeRowCount; i++)
            {
                if (i < rows.Count && rows[i] != null)
                    rows[i].SetSelected(i == _selectedRow);
            }

            if (_selectedRow < rows.Count && rows[_selectedRow] != null && descriptionTypewriter != null)
            {
                descriptionTypewriter.PlayText(rows[_selectedRow].Description);
            }
        }

        private void OnRowHover(int idx)
        {
            if (idx != _selectedRow && idx < _activeRowCount)
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_hover_tick", 0.03f);
                SelectRow(idx);
            }
        }

        private void OnRowStep(int idx, int dir)
        {
            if (idx < 0 || idx >= _activeRowCount || idx >= rows.Count || rows[idx] == null) return;
            SelectRow(idx);
            rows[idx].StepValue(dir);
            ApplyAllSettingsToRuntime();
        }

        private void OnRowDirectChange(int idx)
        {
            if (idx < 0 || idx >= _activeRowCount || idx >= rows.Count || rows[idx] == null) return;
            SelectRow(idx);
            ApplyAllSettingsToRuntime();
        }

        public void ApplyAllSettingsToRuntime()
        {
            // Keep alias keys in sync for gameplay scene consumers
            PlayerPrefs.SetInt("MM_CrtEffect", PlayerPrefs.GetInt("MM_CRTFx", 2));
            PlayerPrefs.SetInt("MM_VolSfx", PlayerPrefs.GetInt("MM_VolSFX", 85));

            // 1. Apply system-wide Display, Resolution, VSync, FPS, and AudioListener settings
            ApplySystemWideSettings();

            // 2. Brightness (0..100, default 50)
            int bright = PlayerPrefs.GetInt("MM_Brightness", 50);
            ApplyBrightnessToImage(brightnessOverlayImage, bright);

            if (shrineSilhouetteImage != null)
            {
                float sAlpha = Mathf.Lerp(0.06f, 0.90f, bright / 100f);
                shrineSilhouetteImage.color = new Color(0.88f, 0.88f, 0.85f, sAlpha);
            }

            // 3. CRT & Film Grain in Main Menu
            if (staticController != null)
            {
                staticController.ApplyPrefsQuality();
            }

            // 4. Audio volumes in Main Menu
            if (MenuAudioManager.Instance != null)
            {
                MenuAudioManager.Instance.ApplyVolumesFromPrefs();
            }
        }

        public static void ApplySystemWideSettings()
        {
            // Display Mode & Resolution
            int fsModeIdx = PlayerPrefs.GetInt("MM_FullscreenMode", 0);
            FullScreenMode fsMode = FullScreenMode.FullScreenWindow;
            if (fsModeIdx == 0) fsMode = FullScreenMode.ExclusiveFullScreen;
            else if (fsModeIdx == 1) fsMode = FullScreenMode.Windowed;
            else fsMode = FullScreenMode.FullScreenWindow;

            int resIdx = PlayerPrefs.GetInt("MM_ResIndex", 0);
            int targetW = 1920;
            int targetH = 1080;
            if (resIdx == 1) { targetW = 1600; targetH = 900; }
            else if (resIdx == 2) { targetW = 1280; targetH = 720; }

            Screen.fullScreenMode = fsMode;
            Screen.SetResolution(targetW, targetH, fsMode);

            // V-Sync
            int vsync = PlayerPrefs.GetInt("MM_VSync", 1);
            QualitySettings.vSyncCount = (vsync == 1) ? 1 : 0;

            // FPS Limit
            int fpsIdx = PlayerPrefs.GetInt("MM_FpsLimit", 1);
            int targetFps = 60;
            if (fpsIdx == 0) targetFps = 30;
            else if (fpsIdx == 1) targetFps = 60;
            else if (fpsIdx == 2) targetFps = 120;
            else targetFps = -1;
            Application.targetFrameRate = targetFps;

            // Master Volume on AudioListener
            float masterVol = Mathf.Clamp01(PlayerPrefs.GetInt("MM_VolMaster", 80) / 100f);
            AudioListener.volume = masterVol;
        }

        public static void ApplyBrightnessToImage(Image overlay, int bright)
        {
            if (overlay == null) return;
            if (bright <= 50)
            {
                // 0 -> 0.72 black darkness, 50 -> 0.0 transparent
                float darkAlpha = Mathf.Lerp(0.72f, 0f, bright / 50f);
                overlay.color = new Color(0f, 0f, 0f, darkAlpha);
            }
            else
            {
                // 50 -> 0.0 transparent, 100 -> 0.22 soft gamma lift
                float liftAlpha = Mathf.Lerp(0f, 0.22f, (bright - 50f) / 50f);
                overlay.color = new Color(0.95f, 0.97f, 1f, liftAlpha);
            }
        }

        private static bool WasPressed(Key k)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
#else
            return false;
#endif
        }

        private static bool WasRightClicked()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }
    }
}
