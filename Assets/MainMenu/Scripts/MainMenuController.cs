using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public enum MenuScreenState
    {
        Boot,
        Main,
        GameMode,
        Settings,
        Credits,
        ConfirmModal,
        Transitioning
    }

    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene & Save Config")]
        [SerializeField] private string gameSceneName = "test";
        [SerializeField] private string saveKey = "SC_HasSave";
        [SerializeField] private bool inSceneOverlayMode = false;
        [SerializeField] private List<GameObject> overlayCanvasObjects = new List<GameObject>();

        [Header("Screen Panels")]
        [SerializeField] private GameObject panelBoot;
        [SerializeField] private GameObject panelMain;
        [SerializeField] private GameObject panelGameMode;
        [SerializeField] private GameObject panelSettings;
        [SerializeField] private GameObject panelCredits;
        [SerializeField] private GameObject modalConfirm;
        [SerializeField] private GameObject titleBlockRoot;

        [Header("Boot Elements")]
        [SerializeField] private RectTransform bootPlayIconRect;
        [SerializeField] private CanvasGroup bootCanvasGroup;
        [SerializeField] private TMP_Text bootPressKeyText;

        [Header("Title & Stamp")]
        [SerializeField] private GlitchText titleGlitch;
        [SerializeField] private Extrude3DText titleExtrude;
        [SerializeField] private TypewriterText stampTypewriter;
        [SerializeField] private string stampFullText = "NIGHT SHIFT";

        [Header("Main Menu List")]
        [SerializeField] private List<MenuItemFX> mainItems = new List<MenuItemFX>();
        [SerializeField] private TypewriterText mainDescTypewriter;
        [SerializeField] private RectTransform mainDescRect;

        [Header("Game Mode List")]
        [SerializeField] private List<MenuItemFX> gameModeItems = new List<MenuItemFX>();
        [SerializeField] private TypewriterText gameModeDescTypewriter;
        [SerializeField] private RectTransform gameModeDescRect;

        [Header("Confirm Modal")]
        [SerializeField] private TMP_Text confirmPromptText;
        [SerializeField] private List<MenuItemFX> confirmItems = new List<MenuItemFX>();

        [Header("Credits")]
        [SerializeField] private RectTransform creditsScrollRect;

        [Header("Sub-Controllers")]
        [SerializeField] private CRTStaticController staticController;
        [SerializeField] private CRTPowerTransition powerTransition;
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private AmbientEasterEggs easterEggs;
        [SerializeField] private TMP_Text bottomHintText;

        private MenuScreenState _state = MenuScreenState.Boot;
        private int _mainIndex = 1;
        private int _modeIndex = 1;
        private int _confirmIndex = 0;
        private Action _confirmYesAction;
        private float _bootBlinkTimer;
        private bool _hasSave;
        private static bool _hasSeenBootThisSession;

        private void Start()
        {
            if (inSceneOverlayMode) Time.timeScale = 0f;
            _hasSave = PlayerPrefs.GetInt(saveKey, 0) == 1;
            if (titleExtrude != null) titleExtrude.RebuildExtrudeLayers();

            SetupMainItems();
            SetupGameModeItems();
            SetupConfirmItems();

            if (_hasSeenBootThisSession)
            {
                EnterMainScreenDirect();
            }
            else
            {
                EnterBootScreen();
            }
        }

        private void OnDisable()
        {
            if (inSceneOverlayMode) Time.timeScale = 1f;
        }

        private void SetupMainItems()
        {
            if (mainItems.Count < 5) return;

            mainItems[0].Setup(0, "Continue", "", false, _hasSave);
            mainItems[1].Setup(1, "New Shift", "", false, true);
            mainItems[2].Setup(2, "Settings", "", false, true);
            mainItems[3].Setup(3, "Credits", "", false, true);
            mainItems[4].Setup(4, "Quit", "", true, true);

            for (int i = 0; i < mainItems.Count; i++)
            {
                mainItems[i].OnHovered = OnMainItemHovered;
                mainItems[i].OnClicked = OnMainItemClicked;
            }
        }

        private void SetupGameModeItems()
        {
            if (gameModeItems.Count < 3) return;

            gameModeItems[0].Setup(0, "Casual", "", false, true);
            gameModeItems[1].Setup(1, "Standard", "", false, true);
            gameModeItems[2].Setup(2, "Insanity", "", true, true);

            for (int i = 0; i < gameModeItems.Count; i++)
            {
                gameModeItems[i].OnHovered = OnGameModeHovered;
                gameModeItems[i].OnClicked = OnGameModeClicked;
            }
        }

        private void SetupConfirmItems()
        {
            if (confirmItems.Count < 2) return;
            confirmItems[0].Setup(0, "Stay", "", false, true);
            confirmItems[1].Setup(1, "Quit", "", true, true);

            for (int i = 0; i < confirmItems.Count; i++)
            {
                confirmItems[i].OnHovered = idx => SelectConfirmIndex(idx, true);
                confirmItems[i].OnClicked = idx =>
                {
                    SelectConfirmIndex(idx, false);
                    ExecuteConfirmChoice();
                };
            }
        }

        // =====================================================================
        // BOOT & INTRO SEQUENCE
        // =====================================================================
        private void EnterBootScreen()
        {
            _state = MenuScreenState.Boot;
            ShowOnlyPanel(panelBoot, false);

            if (staticController != null)
                staticController.SetIntensityImmediate(0.96f);

            if (bottomHintText != null)
                bottomHintText.text = "[ANY KEY / CLICK] START SYSTEM";

            if (powerTransition != null)
                powerTransition.PlayPowerOn();
        }

        private void EnterMainScreenDirect()
        {
            _state = MenuScreenState.Main;
            ShowOnlyPanel(panelMain, true);
            if (staticController != null)
                staticController.TweenIntensity(0.08f, 0.35f);
            if (stampTypewriter != null)
                stampTypewriter.PlayText(stampFullText, 0.028f);

            _mainIndex = _hasSave ? 0 : 1;
            SelectMainIndex(_mainIndex, false);
            UpdateBottomHintForMain();
        }

        private IEnumerator TransitionBootToMainRoutine()
        {
            _state = MenuScreenState.Transitioning;
            _hasSeenBootThisSession = true;

            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_select", 0.02f);

            // Zoom ▶ icon out + fade boot screen while static drops 1.0 -> 0.08
            if (staticController != null)
                staticController.TweenIntensity(0.08f, 0.55f);

            float dur = 0.38f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                if (bootPlayIconRect != null)
                    bootPlayIconRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.45f, p);
                if (bootCanvasGroup != null)
                    bootCanvasGroup.alpha = 1f - p;
                yield return null;
            }

            ShowOnlyPanel(panelMain, true);
            if (titleGlitch != null)
                titleGlitch.TriggerIntroGlitchIn();
            if (stampTypewriter != null)
                stampTypewriter.PlayText(stampFullText, 0.032f);

            _mainIndex = _hasSave ? 0 : 1;
            SelectMainIndex(_mainIndex, false);
            UpdateBottomHintForMain();
            _state = MenuScreenState.Main;
        }

        // =====================================================================
        // UPDATE & INPUT HANDLING (New Input System)
        // =====================================================================
        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null && kb.anyKey.wasPressedThisFrame && easterEggs != null)
                easterEggs.NotifyUserInput();

            if (_state == MenuScreenState.Boot)
            {
                _bootBlinkTimer += Time.unscaledDeltaTime;
                if (bootPressKeyText != null)
                {
                    float a = Mathf.Lerp(0.28f, 1f, 0.5f + 0.5f * Mathf.Sin(_bootBlinkTimer * 3.2f));
                    Color c = bootPressKeyText.color;
                    c.a = a;
                    bootPressKeyText.color = c;
                }

                bool anyPressed = (kb != null && kb.anyKey.wasPressedThisFrame) ||
                                  (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));
                if (anyPressed)
                {
                    StartCoroutine(TransitionBootToMainRoutine());
                }
                return;
            }

            if (_state == MenuScreenState.Transitioning) return;

            bool up = kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame);
            bool down = kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame);
            bool left = kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
            bool right = kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
            bool submit = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
            bool back = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) ||
                        (mouse != null && mouse.rightButton.wasPressedThisFrame && PlayerPrefs.GetInt("MM_RightClickBack", 1) == 1);
            bool resetKey = kb != null && kb.rKey.wasPressedThisFrame;

            switch (_state)
            {
                case MenuScreenState.Main:
                    if (up) SelectMainIndex((_mainIndex - 1 + mainItems.Count) % mainItems.Count, true);
                    else if (down) SelectMainIndex((_mainIndex + 1) % mainItems.Count, true);
                    else if (submit) OnMainItemClicked(_mainIndex);
                    break;

                case MenuScreenState.GameMode:
                    if (up) SelectGameModeIndex((_modeIndex - 1 + gameModeItems.Count) % gameModeItems.Count, true);
                    else if (down) SelectGameModeIndex((_modeIndex + 1) % gameModeItems.Count, true);
                    else if (submit) OnGameModeClicked(_modeIndex);
                    else if (back) ReturnToMainMenu();
                    break;

                case MenuScreenState.Settings:
                    if (settingsManager != null)
                    {
                        if (up) settingsManager.StepRowSelection(-1);
                        else if (down) settingsManager.StepRowSelection(1);
                        else if (left) settingsManager.StepCurrentRow(-1);
                        else if (right) settingsManager.StepCurrentRow(1);
                        else if (kb != null && kb.qKey.wasPressedThisFrame) settingsManager.StepCategory(-1);
                        else if (kb != null && kb.eKey.wasPressedThisFrame) settingsManager.StepCategory(1);
                        else if (resetKey) settingsManager.ResetAllToDefaults();
                    }
                    if (back)
                    {
                        PlayerPrefs.Save();
                        ReturnToMainMenu();
                    }
                    break;

                case MenuScreenState.Credits:
                    if (creditsScrollRect != null)
                    {
                        Vector2 pos = creditsScrollRect.anchoredPosition;
                        pos.y += Time.unscaledDeltaTime * 42f;
                        if (pos.y > 680f)
                        {
                            ReturnToMainMenu();
                            return;
                        }
                        creditsScrollRect.anchoredPosition = pos;
                    }
                    if (back || submit) ReturnToMainMenu();
                    break;

                case MenuScreenState.ConfirmModal:
                    if (left || up) SelectConfirmIndex(0, true);
                    else if (right || down) SelectConfirmIndex(1, true);
                    else if (submit) ExecuteConfirmChoice();
                    else if (back) CloseConfirmModal();
                    break;
            }
        }

        // =====================================================================
        // MAIN MENU ACTIONS
        // =====================================================================
        private void OnMainItemHovered(int index)
        {
            if (_state != MenuScreenState.Main) return;
            if (index != _mainIndex)
                SelectMainIndex(index, true);
        }

        private void SelectMainIndex(int index, bool playTick)
        {
            _mainIndex = Mathf.Clamp(index, 0, mainItems.Count - 1);
            if (playTick && MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_hover_tick", 0.04f);

            for (int i = 0; i < mainItems.Count; i++)
                mainItems[i].SetSelected(i == _mainIndex);

            var item = mainItems[_mainIndex];
            if (titleGlitch != null)
                titleGlitch.SetDangerMode(item.IsDanger);

            if (mainDescTypewriter != null)
                mainDescTypewriter.PlayText(item.DescriptionText, 0.015f);
        }

        private void OnMainItemClicked(int index)
        {
            if (_state != MenuScreenState.Main) return;
            SelectMainIndex(index, false);

            var item = mainItems[index];
            if (!item.Interactable)
            {
                if (MenuAudioManager.Instance != null)
                    MenuAudioManager.Instance.Play("ui_deny", 0.02f);
                if (staticController != null)
                    staticController.PulseBurst(0.22f, 0.14f);
                return;
            }

            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_select", 0.02f);
            if (staticController != null)
                staticController.PulseBurst(0.24f, 0.16f);
            if (titleGlitch != null)
                titleGlitch.TriggerBurst(0.18f, false);

            item.PlayPressFlash(() =>
            {
                switch (index)
                {
                    case 0: // ทำงานต่อ (Continue)
                        StartGameTransition();
                        break;
                    case 1: // เริ่มกะใหม่ (New Shift -> Game Mode Select)
                        OpenGameModeScreen();
                        break;
                    case 2: // ตั้งค่า (Settings)
                        OpenSettingsScreen();
                        break;
                    case 3: // เครดิต (Credits)
                        OpenCreditsScreen();
                        break;
                    case 4: // Quit Confirm
                        OpenConfirmModal("LEAVE SHIFT BEFORE 06:00 AM? ARE YOU SURE YOU WANT TO QUIT?", QuitApplication);
                        break;
                }
            });
        }

        // =====================================================================
        // GAME MODE SCREEN (Ref 5)
        // =====================================================================
        private void OpenGameModeScreen()
        {
            _state = MenuScreenState.GameMode;
            ShowOnlyPanel(panelGameMode, true);
            _modeIndex = PlayerPrefs.GetInt("MM_GameMode", 1);
            SelectGameModeIndex(_modeIndex, false);
            if (bottomHintText != null)
                bottomHintText.text = "[↑/↓] Scroll    [ENTER / CLICK] Confirm    [ESC / RMB] Back";
        }

        private void OnGameModeHovered(int idx)
        {
            if (_state != MenuScreenState.GameMode) return;
            if (idx != _modeIndex) SelectGameModeIndex(idx, true);
        }

        private void SelectGameModeIndex(int idx, bool playTick)
        {
            _modeIndex = Mathf.Clamp(idx, 0, gameModeItems.Count - 1);
            if (playTick && MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_hover_tick", 0.04f);

            for (int i = 0; i < gameModeItems.Count; i++)
                gameModeItems[i].SetSelected(i == _modeIndex);

            var item = gameModeItems[_modeIndex];
            if (titleGlitch != null)
                titleGlitch.SetDangerMode(item.IsDanger);
            if (gameModeDescTypewriter != null)
                gameModeDescTypewriter.PlayText(item.DescriptionText, 0.015f);
        }

        private void OnGameModeClicked(int idx)
        {
            if (_state != MenuScreenState.GameMode) return;
            SelectGameModeIndex(idx, false);
            PlayerPrefs.SetInt("MM_GameMode", idx);
            PlayerPrefs.Save();

            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_select", 0.02f);

            gameModeItems[idx].PlayPressFlash(StartGameTransition);
        }

        // =====================================================================
        // SETTINGS, CREDITS, CONFIRM MODAL
        // =====================================================================
        private void OpenSettingsScreen()
        {
            _state = MenuScreenState.Settings;
            ShowOnlyPanel(panelSettings, false);
            if (settingsManager != null)
                settingsManager.OpenSettings();
            if (bottomHintText != null)
                bottomHintText.text = "[↑/↓] Scroll   [←/→] Adjust   [Q/E] Category   [R] Default   [ESC] Save & Back";
        }

        private void OpenCreditsScreen()
        {
            _state = MenuScreenState.Credits;
            ShowOnlyPanel(panelCredits, true);
            if (creditsScrollRect != null)
                creditsScrollRect.anchoredPosition = new Vector2(0f, -260f);
            if (bottomHintText != null)
                bottomHintText.text = "[ESC / ENTER / RMB] Return to Main Menu";
        }

        private void OpenConfirmModal(string prompt, Action onYes)
        {
            _state = MenuScreenState.ConfirmModal;
            _confirmYesAction = onYes;
            if (confirmPromptText != null) confirmPromptText.text = prompt;
            if (modalConfirm != null) modalConfirm.SetActive(true);
            SelectConfirmIndex(0, false);
            if (bottomHintText != null)
                bottomHintText.text = "[←/→] Select   [ENTER] Confirm   [ESC] Cancel";
        }

        private void SelectConfirmIndex(int idx, bool playTick)
        {
            _confirmIndex = Mathf.Clamp(idx, 0, 1);
            if (playTick && MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_hover_tick", 0.04f);
            for (int i = 0; i < confirmItems.Count; i++)
                confirmItems[i].SetSelected(i == _confirmIndex);
        }

        private void ExecuteConfirmChoice()
        {
            if (_confirmIndex == 0)
            {
                CloseConfirmModal();
            }
            else
            {
                if (MenuAudioManager.Instance != null)
                    MenuAudioManager.Instance.Play("ui_select", 0.02f);
                _confirmYesAction?.Invoke();
            }
        }

        private void CloseConfirmModal()
        {
            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_back", 0.02f);
            if (modalConfirm != null) modalConfirm.SetActive(false);
            _state = MenuScreenState.Main;
            UpdateBottomHintForMain();
        }

        public void ReturnToMainMenu()
        {
            if (MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("ui_back", 0.02f);
            if (staticController != null)
                staticController.PulseBurst(0.20f, 0.14f);

            _state = MenuScreenState.Main;
            ShowOnlyPanel(panelMain, true);
            SelectMainIndex(_mainIndex, false);
            UpdateBottomHintForMain();
        }

        private void ShowOnlyPanel(GameObject activePanel, bool showTitleBlock)
        {
            if (panelBoot != null) panelBoot.SetActive(activePanel == panelBoot);
            if (panelMain != null) panelMain.SetActive(activePanel == panelMain);
            if (panelGameMode != null) panelGameMode.SetActive(activePanel == panelGameMode);
            if (panelSettings != null) panelSettings.SetActive(activePanel == panelSettings);
            if (panelCredits != null) panelCredits.SetActive(activePanel == panelCredits);
            if (modalConfirm != null) modalConfirm.SetActive(false);
            if (titleBlockRoot != null) titleBlockRoot.SetActive(showTitleBlock);
        }

        private void UpdateBottomHintForMain()
        {
            if (bottomHintText != null)
                bottomHintText.text = "[W/S or ↑/↓] Scroll     [ENTER / CLICK] Select     [ESC / RMB] Back";
        }

        // =====================================================================
        // CRT POWER-OFF TRANSITION INTO GAME SCENE
        // =====================================================================
        private void StartGameTransition()
        {
            _state = MenuScreenState.Transitioning;
            StartCoroutine(LoadGameSceneRoutine());
        }

        private IEnumerator LoadGameSceneRoutine()
        {
            if (staticController != null)
                staticController.TweenIntensity(1.0f, 0.35f);

            yield return new WaitForSecondsRealtime(0.25f);

            bool powerOffFinished = false;
            if (powerTransition != null)
            {
                powerTransition.PlayPowerOff(() => powerOffFinished = true);
                while (!powerOffFinished) yield return null;
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.4f);
            }

            if (inSceneOverlayMode)
            {
                foreach (var go in overlayCanvasObjects)
                {
                    if (go != null) go.SetActive(false);
                }
                if (MenuAudioManager.Instance != null)
                {
                    foreach (var src in MenuAudioManager.Instance.GetComponents<AudioSource>())
                        if (src.loop) src.Stop();
                }
                Time.timeScale = 1f;
                if (powerTransition != null) powerTransition.PlayPowerOn();
                gameObject.SetActive(false);
                yield break;
            }

            if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
                while (op != null && !op.isDone)
                    yield return null;
            }
            else
            {
                Debug.Log($"[SecurityCenter.MainMenu] CRT Power-Off complete. Ready to load scene '{gameSceneName}' (add '{gameSceneName}' to Build Settings if needed).");
                // Recover gracefully in Editor preview if scene is not in Build Settings yet
                yield return new WaitForSecondsRealtime(0.6f);
                if (powerTransition != null) powerTransition.PlayPowerOn();
                EnterMainScreenDirect();
            }
        }

        private void QuitApplication()
        {
            _state = MenuScreenState.Transitioning;
            if (powerTransition != null)
            {
                powerTransition.PlayPowerOff(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
