using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SecurityCenter.MainMenu
{
    public enum MenuState
    {
        BootScreen,
        MainMenu,
        GameModeSelect,
        Settings,
        Credits,
        ConfirmQuit,
        Transitioning
    }

    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene & Save Config")]
        [SerializeField] private string gameSceneName = "test";
        [SerializeField] private string saveKey = "SC_HasSave";
        [SerializeField] private bool inSceneOverlayMode = false;
        [SerializeField] private List<GameObject> overlayCanvasObjects = new List<GameObject>();

        [Header("Panels")]
        [SerializeField] private GameObject panelBoot;
        [SerializeField] private GameObject panelMain;
        [SerializeField] private GameObject panelGameMode;
        [SerializeField] private GameObject panelSettings;
        [SerializeField] private GameObject panelCredits;
        [SerializeField] private GameObject modalConfirm;
        [SerializeField] private GameObject titleBlockRoot;

        [Header("Boot Screen")]
        [SerializeField] private RectTransform bootPlayIconRect;
        [SerializeField] private CanvasGroup bootCanvasGroup;
        [SerializeField] private TMP_Text bootPressKeyText;

        [Header("Title & Stamp")]
        [SerializeField] private GlitchText titleGlitch;
        [SerializeField] private Extrude3DText titleExtrude;
        [SerializeField] private TypewriterText stampTypewriter;
        [SerializeField] private string stampFullText = "";

        [Header("Main Menu Items (3 Buttons: Find Job, Settings, Quit)")]
        [SerializeField] private List<MenuItemFX> mainItems = new List<MenuItemFX>();
        [SerializeField] private TypewriterText mainDescTypewriter;
        [SerializeField] private RectTransform mainDescRect;

        [Header("Game Mode Items")]
        [SerializeField] private List<MenuItemFX> gameModeItems = new List<MenuItemFX>();
        [SerializeField] private TypewriterText gameModeDescTypewriter;
        [SerializeField] private RectTransform gameModeDescRect;

        [Header("Confirm Modal Items")]
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

        private MenuState _state = MenuState.BootScreen;
        private int _mainSelectedIndex = 0;
        private int _modeSelectedIndex = 1;
        private int _confirmSelectedIndex = 0;
        private bool _confirmIsOverwrite = false;
        private bool _hasSave = false;

        private static readonly string[] ThreeButtonDescriptions =
        {
            "Clock in at 00:00 AM. Check emails, inspect websites, answer calls, and survive until 06:00 AM.",
            "Configure video brightness, CRT scanlines, audio levels, and camera rotation sensitivity.",
            "Power down the terminal and exit Job Haunter."
        };

        private static readonly string[] FiveButtonDescriptions =
        {
            "Resume your current night shift from the last recorded hour.",
            "Clock in at 00:00 AM. Inspect emails, answer the phone, and survive until 06:00 AM.",
            "Configure video, audio, camera rotation sensitivity, and accessibility.",
            "View the development team and system logs.",
            "Leave the desk and power down the terminal."
        };

        private static readonly string[] GameModeDescriptions =
        {
            "CASUAL SHIFT: Slower clock, 10-second shrine prayer window, clearer scam indicators.",
            "STANDARD SHIFT: Balanced 00:00-06:00 pacing. 7-second shrine countdown and standard phishing tricks.",
            "INSANITY SHIFT: Aggressive ghost calls, 4.5-second shrine window, subtle URL spoofing, heavier static."
        };

        private void Start()
        {
            Time.timeScale = inSceneOverlayMode ? 0f : 1f;
            _hasSave = PlayerPrefs.GetInt(saveKey, 0) == 1;

            for (int i = 0; i < mainItems.Count; i++)
            {
                if (mainItems[i] == null) continue;
                mainItems[i].ItemIndex = i;
                mainItems[i].OnHovered = OnMainItemHovered;
                mainItems[i].OnClicked = OnMainItemClicked;
            }

            if (mainItems.Count == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (mainItems[i] != null) mainItems[i].Interactable = true;
                }
                _mainSelectedIndex = 0;
            }
            else if (mainItems.Count >= 5)
            {
                if (mainItems[0] != null) mainItems[0].Interactable = _hasSave;
                _mainSelectedIndex = _hasSave ? 0 : 1;
            }

            for (int i = 0; i < gameModeItems.Count; i++)
            {
                if (gameModeItems[i] == null) continue;
                gameModeItems[i].ItemIndex = i;
                gameModeItems[i].OnHovered = OnModeItemHovered;
                gameModeItems[i].OnClicked = OnModeItemClicked;
            }

            for (int i = 0; i < confirmItems.Count; i++)
            {
                if (confirmItems[i] == null) continue;
                confirmItems[i].ItemIndex = i;
                confirmItems[i].OnHovered = OnConfirmItemHovered;
                confirmItems[i].OnClicked = OnConfirmItemClicked;
            }

            if (settingsManager != null)
            {
                settingsManager.OnBackRequested = ReturnToMainMenu;
            }

            EnterMainMenu(playBootBurst: false);
        }

        private void EnterBootScreen()
        {
            _state = MenuState.BootScreen;
            SetActivePanels(boot: true, main: false, mode: false, settings: false, credits: false, confirm: false, titleVisible: true);
            if (bottomHintText != null)
                bottomHintText.text = "[ PRESS ANY KEY OR CLICK TO ACCESS TERMINAL ]";
        }

        private void EnterMainMenu(bool playBootBurst)
        {
            _state = MenuState.MainMenu;
            SetActivePanels(boot: false, main: true, mode: false, settings: false, credits: false, confirm: false, titleVisible: true);

            if (easterEggs != null) easterEggs.enabled = true;
            if (bottomHintText != null)
                bottomHintText.text = "[W/S] NAVIGATE    [ENTER / CLICK] SELECT    [ESC] QUIT";

            if (playBootBurst)
            {
                if (MenuAudioManager.Instance != null)
                {
                    MenuAudioManager.Instance.Play("crt_on");
                    MenuAudioManager.Instance.ApplyVolumesFromPrefs();
                }
                if (staticController != null) staticController.PulseBurst(0.55f, 0.28f);
                if (titleGlitch != null) titleGlitch.TriggerBurst(0.24f, true);
                if (stampTypewriter != null) stampTypewriter.PlayText(stampFullText);
            }
            else
            {
                if (MenuAudioManager.Instance != null)
                    MenuAudioManager.Instance.ApplyVolumesFromPrefs();
            }

            SelectMainItem(_mainSelectedIndex, playSfx: false);
        }

        private void Update()
        {
            if (_state == MenuState.Transitioning) return;

            switch (_state)
            {
                case MenuState.BootScreen:
                    UpdateBootScreen();
                    break;
                case MenuState.MainMenu:
                    UpdateMainMenu();
                    break;
                case MenuState.GameModeSelect:
                    UpdateGameModeSelect();
                    break;
                case MenuState.Credits:
                    UpdateCredits();
                    break;
                case MenuState.ConfirmQuit:
                    UpdateConfirmModal();
                    break;
            }
        }

        private void UpdateBootScreen()
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.055f;
            if (bootPlayIconRect != null)
                bootPlayIconRect.localScale = new Vector3(pulse, pulse, 1f);

            if (bootPressKeyText != null)
            {
                Color c = bootPressKeyText.color;
                c.a = 0.45f + Mathf.PingPong(Time.unscaledTime * 1.1f, 0.55f);
                bootPressKeyText.color = c;
            }

            if (AnyKeyOrClickPressed())
            {
                EnterMainMenu(playBootBurst: true);
            }
        }

        private void UpdateMainMenu()
        {
            if (WasPressed(Key.W) || WasPressed(Key.UpArrow))
            {
                StepMainSelection(-1);
            }
            else if (WasPressed(Key.S) || WasPressed(Key.DownArrow))
            {
                StepMainSelection(1);
            }
            else if (WasPressed(Key.Enter) || WasPressed(Key.Space))
            {
                ExecuteMainSelection(_mainSelectedIndex);
            }
            else if (WasPressed(Key.Escape))
            {
                OpenConfirmQuit();
            }
        }

        private void StepMainSelection(int dir)
        {
            int count = mainItems.Count;
            if (count == 0) return;

            int next = _mainSelectedIndex;
            for (int attempt = 0; attempt < count; attempt++)
            {
                next = (next + dir + count) % count;
                if (mainItems[next] != null && mainItems[next].Interactable)
                    break;
            }
            SelectMainItem(next, playSfx: true);
        }

        private void SelectMainItem(int index, bool playSfx)
        {
            if (mainItems.Count == 0) return;
            _mainSelectedIndex = Mathf.Clamp(index, 0, mainItems.Count - 1);
            for (int i = 0; i < mainItems.Count; i++)
            {
                if (mainItems[i] != null)
                    mainItems[i].SetSelected(i == _mainSelectedIndex, playSfx && i == _mainSelectedIndex);
            }

            string[] descs = (mainItems.Count <= 3) ? ThreeButtonDescriptions : FiveButtonDescriptions;
            if (mainDescTypewriter != null && _mainSelectedIndex < descs.Length)
            {
                if (mainDescRect != null && mainItems[_mainSelectedIndex] != null)
                {
                    var itemRt = mainItems[_mainSelectedIndex].transform as RectTransform;
                    if (itemRt != null)
                    {
                        Vector2 p = mainDescRect.anchoredPosition;
                        p.y = itemRt.anchoredPosition.y;
                        mainDescRect.anchoredPosition = p;
                    }
                }
                mainDescTypewriter.PlayText(descs[_mainSelectedIndex]);
            }
        }

        private void OnMainItemHovered(int index)
        {
            if (_state != MenuState.MainMenu) return;
            if (index != _mainSelectedIndex)
                SelectMainItem(index, playSfx: true);
        }

        private void OnMainItemClicked(int index)
        {
            if (_state != MenuState.MainMenu) return;
            SelectMainItem(index, playSfx: false);
            ExecuteMainSelection(index);
        }

        private void ExecuteMainSelection(int index)
        {
            if (index < 0 || index >= mainItems.Count || mainItems[index] == null) return;
            if (!mainItems[index].Interactable)
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_deny");
                return;
            }

            mainItems[index].PlayPressFlash(null);
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select", 0.03f);

            // 3-Button Layout: 0 = Find Job, 1 = Settings, 2 = Quit
            if (mainItems.Count <= 3)
            {
                switch (index)
                {
                    case 0: // FIND JOB (Start Game)
                        PlayerPrefs.SetInt(saveKey, 1);
                        PlayerPrefs.Save();
                        StartGameTransition();
                        break;
                    case 1: // SETTINGS
                        OpenSettings();
                        break;
                    case 2: // QUIT
                        OpenConfirmQuit();
                        break;
                }
                return;
            }

            switch (index)
            {
                case 0:
                    StartGameTransition();
                    break;
                case 1:
                    PlayerPrefs.SetInt(saveKey, 1);
                    PlayerPrefs.Save();
                    StartGameTransition();
                    break;
                case 2:
                    OpenSettings();
                    break;
                case 3:
                    OpenCredits();
                    break;
                case 4:
                    OpenConfirmQuit();
                    break;
            }
        }

        private void OpenGameModeSelect()
        {
            _state = MenuState.GameModeSelect;
            if (easterEggs != null) easterEggs.enabled = false;
            if (staticController != null) staticController.PulseBurst(0.45f, 0.14f);
            SetActivePanels(boot: false, main: false, mode: true, settings: false, credits: false, confirm: false, titleVisible: true);
            if (bottomHintText != null)
                bottomHintText.text = "[W/S] SELECT DIFFICULTY    [ENTER] START SHIFT    [ESC / RIGHT CLICK] BACK";

            SelectModeItem(_modeSelectedIndex, playSfx: false);
        }

        private void UpdateGameModeSelect()
        {
            if (WasPressed(Key.Escape) || WasRightClicked())
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_back");
                ReturnToMainMenu();
                return;
            }

            if (WasPressed(Key.W) || WasPressed(Key.UpArrow))
                SelectModeItem((_modeSelectedIndex - 1 + gameModeItems.Count) % gameModeItems.Count, true);
            else if (WasPressed(Key.S) || WasPressed(Key.DownArrow))
                SelectModeItem((_modeSelectedIndex + 1) % gameModeItems.Count, true);
            else if (WasPressed(Key.Enter) || WasPressed(Key.Space))
                OnModeItemClicked(_modeSelectedIndex);
        }

        private void SelectModeItem(int index, bool playSfx)
        {
            if (gameModeItems.Count == 0) return;
            _modeSelectedIndex = Mathf.Clamp(index, 0, gameModeItems.Count - 1);
            for (int i = 0; i < gameModeItems.Count; i++)
            {
                if (gameModeItems[i] != null)
                    gameModeItems[i].SetSelected(i == _modeSelectedIndex, playSfx && i == _modeSelectedIndex);
            }

            if (gameModeDescTypewriter != null && _modeSelectedIndex < GameModeDescriptions.Length)
            {
                if (gameModeDescRect != null && gameModeItems[_modeSelectedIndex] != null)
                {
                    var itemRt = gameModeItems[_modeSelectedIndex].transform as RectTransform;
                    if (itemRt != null)
                    {
                        Vector2 p = gameModeDescRect.anchoredPosition;
                        p.y = itemRt.anchoredPosition.y;
                        gameModeDescRect.anchoredPosition = p;
                    }
                }
                gameModeDescTypewriter.PlayText(GameModeDescriptions[_modeSelectedIndex]);
            }
        }

        private void OnModeItemHovered(int index)
        {
            if (_state != MenuState.GameModeSelect) return;
            if (index != _modeSelectedIndex)
                SelectModeItem(index, true);
        }

        private void OnModeItemClicked(int index)
        {
            if (_state != MenuState.GameModeSelect) return;
            SelectModeItem(index, false);
            PlayerPrefs.SetInt("MM_Difficulty", index);
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();

            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select");
            StartGameTransition();
        }

        private void OpenSettings()
        {
            _state = MenuState.Settings;
            if (easterEggs != null) easterEggs.enabled = false;
            if (staticController != null) staticController.PulseBurst(0.45f, 0.14f);
            SetActivePanels(boot: false, main: false, mode: false, settings: true, credits: false, confirm: false, titleVisible: false);
            if (bottomHintText != null)
                bottomHintText.text = "[Q/E] CATEGORY   [W/S] ROW   [A/D or CLICK] ADJUST   [ESC / RIGHT CLICK] BACK";

            if (settingsManager != null)
                settingsManager.OpenSettings();
        }

        private void OpenCredits()
        {
            _state = MenuState.Credits;
            if (easterEggs != null) easterEggs.enabled = false;
            if (staticController != null) staticController.PulseBurst(0.45f, 0.14f);
            SetActivePanels(boot: false, main: false, mode: false, settings: false, credits: true, confirm: false, titleVisible: true);
            if (bottomHintText != null)
                bottomHintText.text = "[ESC / RIGHT CLICK / ENTER] RETURN TO MAIN MENU";

            if (creditsScrollRect != null)
                creditsScrollRect.anchoredPosition = new Vector2(0f, -260f);
        }

        private void UpdateCredits()
        {
            if (creditsScrollRect != null)
            {
                Vector2 pos = creditsScrollRect.anchoredPosition;
                pos.y += 38f * Time.unscaledDeltaTime;
                if (pos.y > 420f) pos.y = -260f;
                creditsScrollRect.anchoredPosition = pos;
            }

            if (WasPressed(Key.Escape) || WasPressed(Key.Enter) || WasPressed(Key.Space) || WasRightClicked())
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_back");
                ReturnToMainMenu();
            }
        }

        public void ReturnToMainMenu()
        {
            EnterMainMenu(playBootBurst: false);
            if (staticController != null) staticController.PulseBurst(0.4f, 0.12f);
        }

        private void OpenConfirmQuit()
        {
            _state = MenuState.ConfirmQuit;
            _confirmIsOverwrite = false;
            if (confirmPromptText != null)
                confirmPromptText.text = "LEAVE SHIFT BEFORE 06:00 AM? ARE YOU SURE YOU WANT TO QUIT?";
            if (modalConfirm != null) modalConfirm.SetActive(true);
            SelectConfirmItem(0, false);
        }

        private void UpdateConfirmModal()
        {
            if (WasPressed(Key.Escape) || WasRightClicked())
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_back");
                if (modalConfirm != null) modalConfirm.SetActive(false);
                _state = MenuState.MainMenu;
                return;
            }

            if (WasPressed(Key.A) || WasPressed(Key.LeftArrow) || WasPressed(Key.W) || WasPressed(Key.UpArrow))
                SelectConfirmItem(0, true);
            else if (WasPressed(Key.D) || WasPressed(Key.RightArrow) || WasPressed(Key.S) || WasPressed(Key.DownArrow))
                SelectConfirmItem(1, true);
            else if (WasPressed(Key.Enter) || WasPressed(Key.Space))
                OnConfirmItemClicked(_confirmSelectedIndex);
        }

        private void SelectConfirmItem(int idx, bool playSfx)
        {
            _confirmSelectedIndex = Mathf.Clamp(idx, 0, Mathf.Max(0, confirmItems.Count - 1));
            for (int i = 0; i < confirmItems.Count; i++)
            {
                if (confirmItems[i] != null)
                    confirmItems[i].SetSelected(i == _confirmSelectedIndex, playSfx && i == _confirmSelectedIndex);
            }
        }

        private void OnConfirmItemHovered(int idx)
        {
            if (_state != MenuState.ConfirmQuit) return;
            if (idx != _confirmSelectedIndex)
                SelectConfirmItem(idx, true);
        }

        private void OnConfirmItemClicked(int idx)
        {
            if (_state != MenuState.ConfirmQuit) return;
            SelectConfirmItem(idx, false);

            if (idx == 0) // CANCEL / STAY
            {
                if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_back");
                if (modalConfirm != null) modalConfirm.SetActive(false);
                _state = MenuState.MainMenu;
            }
            else // CONFIRM
            {
                if (modalConfirm != null) modalConfirm.SetActive(false);
                if (_confirmIsOverwrite)
                {
                    if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("ui_select");
                    StartGameTransition();
                }
                else
                {
                    QuitGameWithCRTCollapse();
                }
            }
        }

        private void StartGameTransition()
        {
            _state = MenuState.Transitioning;
            Time.timeScale = 1f;

            if (inSceneOverlayMode)
            {
                if (powerTransition != null)
                {
                    powerTransition.PlayPowerOff(DismissInSceneOverlay);
                }
                else
                {
                    DismissInSceneOverlay();
                }
                return;
            }

            string targetScene = string.IsNullOrEmpty(gameSceneName) ? "test" : gameSceneName;
            if (powerTransition != null)
            {
                powerTransition.PlayPowerOff(() =>
                {
                    SceneManager.LoadScene(targetScene);
                });
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }
        }

        private void DismissInSceneOverlay()
        {
            Time.timeScale = 1f;
            for (int i = 0; i < overlayCanvasObjects.Count; i++)
            {
                if (overlayCanvasObjects[i] != null)
                    overlayCanvasObjects[i].SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void QuitGameWithCRTCollapse()
        {
            _state = MenuState.Transitioning;

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

        private void SetActivePanels(bool boot, bool main, bool mode, bool settings, bool credits, bool confirm, bool titleVisible)
        {
            if (panelBoot != null) panelBoot.SetActive(boot);
            if (panelMain != null) panelMain.SetActive(main);
            if (panelGameMode != null) panelGameMode.SetActive(mode);
            if (panelSettings != null) panelSettings.SetActive(settings);
            if (panelCredits != null) panelCredits.SetActive(credits);
            if (modalConfirm != null) modalConfirm.SetActive(confirm);
            if (titleBlockRoot != null) titleBlockRoot.SetActive(titleVisible);
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

        private static bool AnyKeyOrClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mouse = Mouse.current != null &&
                         (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
            return key || mouse;
#else
            return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif
        }
    }
}
