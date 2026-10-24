#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu.Editor
{
    public static class MainMenuSceneBuilder
    {
        private const string TargetScenePath = "Assets/Cee/Main menu.unity";
        private const string BaseDir = "Assets/MainMenu";

        [MenuItem("Tools/Security Center/Build Main Menu Scene")]
        public static string BuildMainMenuScene(bool startInMainPreview = true)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != TargetScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            MainMenuAssetGenerator.GenerateAllAssets();

            // Clear previously generated menu roots in Main menu scene only
            string[] managedRoots =
            {
                "EventSystem",
                "_MenuSystems",
                "Canvas_Background",
                "Canvas_UI",
                "Canvas_FX"
            };
            foreach (var rName in managedRoots)
            {
                var old = GameObject.Find(rName);
                if (old != null) Object.DestroyImmediate(old);
            }

            // Configure Main Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam = camGo.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.02f, 1f);
            cam.fieldOfView = 35f;

            // Load generated assets
            var fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_TitleFont_SDF.asset");
            var fontType = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_Typewriter_SDF.asset");
            var fontThai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_ThaiUI_SDF.asset");
            if (fontTitle == null) fontTitle = TMP_Settings.defaultFontAsset;
            if (fontType == null) fontType = fontTitle;
            if (fontThai == null) fontThai = fontTitle;

            var sprDesk = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/BG_Desk.png");
            var sprStamp = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/StampBrushBox.png");
            var sprScanline = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/ScanlineMask.png");
            var sprPlay = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/PlayTriangle.png");
            var sprVignette = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/Vignette.png");
            var sprEye = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/GhostEye.png");
            var sprShrine = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/ShrineSilhouette.png");
            var texStaticNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseDir + "/Textures/StaticNoiseTex.png");
            var matStatic = AssetDatabase.LoadAssetAtPath<Material>(BaseDir + "/Materials/MM_CRTStatic.mat");

            // 1. EventSystem (New Input System)
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 2. _MenuSystems Root
            var sysGo = new GameObject("_MenuSystems");
            var mainCtrl = sysGo.AddComponent<MainMenuController>();
            var audioMgr = sysGo.AddComponent<MenuAudioManager>();
            var settingsMgr = sysGo.AddComponent<SettingsManager>();
            var easterEggs = sysGo.AddComponent<AmbientEasterEggs>();

            WireAudioSystem(sysGo, audioMgr);

            // 3. Canvas_Background (SortOrder 0)
            var canvasBg = CreateScreenCanvas("Canvas_Background", 0);
            var bgDeskGo = CreateUIObject("BG_Desk", canvasBg.transform);
            StretchFull(bgDeskGo.GetComponent<RectTransform>(), -28f); // extra margin for parallax
            var bgDeskImg = bgDeskGo.AddComponent<Image>();
            bgDeskImg.sprite = sprDesk;
            bgDeskImg.color = new Color(0.88f, 0.89f, 0.90f, 1f);
            bgDeskImg.raycastTarget = false;
            var parallax = bgDeskGo.AddComponent<ParallaxBackground>();
            SetSerializedField(parallax, "targetRect", bgDeskGo.GetComponent<RectTransform>());

            var bgStaticGo = CreateUIObject("BG_Static", canvasBg.transform);
            StretchFull(bgStaticGo.GetComponent<RectTransform>(), 0f);
            var bgStaticRaw = bgStaticGo.AddComponent<RawImage>();
            bgStaticRaw.texture = texStaticNoise;
            bgStaticRaw.material = matStatic;
            bgStaticRaw.color = Color.white;
            bgStaticRaw.raycastTarget = false;
            var staticCtrl = bgStaticGo.AddComponent<CRTStaticController>();
            SetSerializedField(staticCtrl, "staticRawImage", bgStaticRaw);
            SetSerializedField(staticCtrl, "staticMaterial", matStatic);
            if (matStatic != null) matStatic.SetFloat("_Intensity", startInMainPreview ? 0.08f : 0.96f);

            // Easter Egg Layer inside Background
            var bgEasterGo = CreateUIObject("BG_EasterEggLayer", canvasBg.transform);
            StretchFull(bgEasterGo.GetComponent<RectTransform>(), 0f);
            var eyeGo = CreateUIObject("GhostEyeFlash", bgEasterGo.transform);
            eyeGo.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 210f);
            var eyeImg = eyeGo.AddComponent<Image>();
            eyeImg.sprite = sprEye;
            eyeImg.color = new Color(0.95f, 0.15f, 0.15f, 0.18f);
            eyeImg.raycastTarget = false;
            eyeGo.SetActive(false);

            var wordGo = CreateUIObject("GhostWordFlash", bgEasterGo.transform);
            wordGo.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 140f);
            var wordTmp = wordGo.AddComponent<TextMeshProUGUI>();
            wordTmp.font = fontTitle;
            wordTmp.fontSize = 96f;
            wordTmp.alignment = TextAlignmentOptions.Center;
            wordTmp.text = "HELP";
            wordTmp.color = new Color(0.92f, 0.12f, 0.12f, 0.22f);
            wordTmp.raycastTarget = false;
            wordGo.SetActive(false);

            SetSerializedField(easterEggs, "eyeFlashRect", eyeGo.GetComponent<RectTransform>());
            SetSerializedField(easterEggs, "eyeFlashImage", eyeImg);
            SetSerializedField(easterEggs, "wordFlashRect", wordGo.GetComponent<RectTransform>());
            SetSerializedField(easterEggs, "wordFlashText", wordTmp);

            var bgVigGo = CreateUIObject("BG_Vignette", canvasBg.transform);
            StretchFull(bgVigGo.GetComponent<RectTransform>(), 0f);
            var bgVigImg = bgVigGo.AddComponent<Image>();
            bgVigImg.sprite = sprVignette;
            bgVigImg.color = Color.white;
            bgVigImg.raycastTarget = false;

            // 4. Canvas_UI (SortOrder 10)
            var canvasUi = CreateScreenCanvas("Canvas_UI", 10);

            // FilmStripLeft
            BuildFilmStripLeft(canvasUi.transform);

            // HUD_Rec (Top-left ● REC)
            var hudRecGo = CreateUIObject("HUD_Rec", canvasUi.transform);
            SetTopLeft(hudRecGo.GetComponent<RectTransform>(), 76f, -44f, 200f, 36f);
            var recDotGo = CreateUIObject("RecDot", hudRecGo.transform);
            SetTopLeft(recDotGo.GetComponent<RectTransform>(), 0f, -8f, 18f, 18f);
            var recDotImg = recDotGo.AddComponent<Image>();
            recDotImg.color = new Color(0.90f, 0.10f, 0.10f, 1f);
            recDotImg.raycastTarget = false;

            var recTxtGo = CreateUIObject("RecLabel", hudRecGo.transform);
            SetTopLeft(recTxtGo.GetComponent<RectTransform>(), 28f, -2f, 140f, 32f);
            var recTmp = CreateTMP(recTxtGo, fontType, "REC", 24f, new Color(0.91f, 0.90f, 0.88f, 0.9f), TextAlignmentOptions.MidlineLeft);
            recTmp.characterSpacing = 12f;

            // HUD_Clock (Top-right 00:00 AM)
            var hudClockGo = CreateUIObject("HUD_Clock", canvasUi.transform);
            var clockRt = hudClockGo.GetComponent<RectTransform>();
            clockRt.anchorMin = new Vector2(1f, 1f);
            clockRt.anchorMax = new Vector2(1f, 1f);
            clockRt.pivot = new Vector2(1f, 1f);
            clockRt.anchoredPosition = new Vector2(-64f, -44f);
            clockRt.sizeDelta = new Vector2(260f, 38f);
            var clockTmp = CreateTMP(hudClockGo, fontType, "00:00 AM", 26f, new Color(0.91f, 0.90f, 0.88f, 0.88f), TextAlignmentOptions.MidlineRight);
            clockTmp.characterSpacing = 10f;

            // Bottom Hint Bar & Bottom-Right Tag
            var hintGo = CreateUIObject("HintBar", canvasUi.transform);
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(0f, 0f);
            hintRt.pivot = new Vector2(0f, 0f);
            hintRt.anchoredPosition = new Vector2(118f, 38f);
            hintRt.sizeDelta = new Vector2(1280f, 34f);
            var hintTmp = CreateTMP(hintGo, fontType, "[W/S] NAVIGATE     [ENTER / CLICK] SELECT     [ESC / RMB] BACK", 20f, new Color(0.85f, 0.85f, 0.82f, 0.68f), TextAlignmentOptions.MidlineLeft);

            var secTagGo = CreateUIObject("Tag_SecOS", canvasUi.transform);
            var secRt = secTagGo.GetComponent<RectTransform>();
            secRt.anchorMin = new Vector2(1f, 0f);
            secRt.anchorMax = new Vector2(1f, 0f);
            secRt.pivot = new Vector2(1f, 0f);
            secRt.anchoredPosition = new Vector2(-64f, 38f);
            secRt.sizeDelta = new Vector2(320f, 34f);
            var secTmp = CreateTMP(secTagGo, fontType, "JOB HAUNTER v1.0", 21f, new Color(0.29f, 0.87f, 0.50f, 0.85f), TextAlignmentOptions.MidlineRight);
            secTmp.characterSpacing = 8f;

            var menuClock = hudClockGo.AddComponent<MenuClock>();
            SetSerializedField(menuClock, "recDotImage", recDotImg);
            SetSerializedField(menuClock, "recLabelText", recTmp);
            SetSerializedField(menuClock, "clockText", clockTmp);
            SetSerializedField(menuClock, "secOsTagText", secTmp);

            // =================================================================
            // TITLE BLOCK: "JOB HAUNTER" + Stamp Subtitle
            // =================================================================
            var titleBlockGo = CreateUIObject("TitleBlock_Root", canvasUi.transform);
            SetTopLeft(titleBlockGo.GetComponent<RectTransform>(), 118f, -110f, 1180f, 250f);

            var title3DGo = BuildGlitch3DTitle(titleBlockGo.transform, fontTitle, sprScanline, "JOB HAUNTER", 112f, 12f, out var titleExtrude, out var titleGlitch);
            TypewriterText stampTypewriter = null;

            // =================================================================
            // PANEL 1: BOOT SCREEN
            // =================================================================
            var panelBoot = CreateUIObject("Panel_Boot", canvasUi.transform);
            StretchFull(panelBoot.GetComponent<RectTransform>(), 0f);
            var bootCg = panelBoot.AddComponent<CanvasGroup>();

            var playIconGo = CreateUIObject("PlayIcon", panelBoot.transform);
            var playRt = playIconGo.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.5f, 0.54f);
            playRt.anchorMax = new Vector2(0.5f, 0.54f);
            playRt.sizeDelta = new Vector2(132f, 132f);
            var playImg = playIconGo.AddComponent<Image>();
            playImg.sprite = sprPlay;
            playImg.color = new Color(0.96f, 0.96f, 0.94f, 0.94f);
            playImg.raycastTarget = false;

            var pressKeyBgGo = CreateUIObject("PressAnyKey_Badge", panelBoot.transform);
            var pkRt = pressKeyBgGo.GetComponent<RectTransform>();
            pkRt.anchorMin = new Vector2(0.5f, 0.20f);
            pkRt.anchorMax = new Vector2(0.5f, 0.20f);
            pkRt.sizeDelta = new Vector2(460f, 44f);
            var pkBgImg = pressKeyBgGo.AddComponent<Image>();
            pkBgImg.color = new Color(0.04f, 0.04f, 0.04f, 0.78f);

            var pressKeyTxtGo = CreateUIObject("PressAnyKey_Text", pressKeyBgGo.transform);
            StretchFull(pressKeyTxtGo.GetComponent<RectTransform>(), 0f);
            var pressKeyTmp = CreateTMP(pressKeyTxtGo, fontType, "[ PRESS ANY KEY ]", 24f, new Color(0.94f, 0.94f, 0.92f, 1f), TextAlignmentOptions.Center);
            pressKeyTmp.characterSpacing = 12f;

            // =================================================================
            // PANEL 2: MAIN MENU (ONLY 3 BUTTONS: Find Job, Settings, Quit)
            // =================================================================
            var panelMain = CreateUIObject("Panel_Main", canvasUi.transform);
            StretchFull(panelMain.GetComponent<RectTransform>(), 0f);

            var mainHeaderGo = CreateUIObject("Header_MainMenu", panelMain.transform);
            SetTopLeft(mainHeaderGo.GetComponent<RectTransform>(), 124f, -430f, 420f, 38f);
            var mainHeaderTmp = CreateTMP(mainHeaderGo, fontType, "MAIN MENU", 26f, new Color(0.90f, 0.89f, 0.86f, 0.92f), TextAlignmentOptions.MidlineLeft);
            mainHeaderTmp.characterSpacing = 10f;

            var mainRuleGo = CreateUIObject("Header_Line", panelMain.transform);
            SetTopLeft(mainRuleGo.GetComponent<RectTransform>(), 124f, -472f, 340f, 2f);
            mainRuleGo.AddComponent<Image>().color = new Color(0.90f, 0.89f, 0.86f, 0.32f);

            var mainItemsList = new List<MenuItemFX>();
            string[] mainTitles =
            {
                "Find Job",
                "Settings",
                "Quit"
            };
            for (int i = 0; i < 3; i++)
            {
                var item = CreateMenuItemRow(panelMain.transform, fontType, i, mainTitles[i], 144f, -505f - i * 72f, 340f, 54f, i == 2);
                mainItemsList.Add(item);
            }
            mainItemsList[0].Interactable = true;
            mainItemsList[0].SetSelected(true, false);

            // Description Box next to Main Menu Buttons
            var mainDescGo = CreateUIObject("MainMenu_Description", panelMain.transform);
            SetTopLeft(mainDescGo.GetComponent<RectTransform>(), 520f, -505f, 680f, 64f);
            var mainDescTmp = CreateTMP(mainDescGo, fontType,
                "Clock in at 00:00 AM. Check emails, inspect websites, answer calls, and survive until 06:00 AM.",
                20f, new Color(0.75f, 0.78f, 0.80f, 0.88f), TextAlignmentOptions.TopLeft);
            var mainDescTypewriter = mainDescGo.AddComponent<TypewriterText>();
            SetSerializedField(mainDescTypewriter, "targetText", mainDescTmp);
            SetSerializedField(mainDescTypewriter, "charDelay", 0.015f);

            // =================================================================
            // PANEL 3: GAME MODE SELECT (Kept hidden for compatibility)
            // =================================================================
            var panelMode = CreateUIObject("Panel_GameMode", canvasUi.transform);
            StretchFull(panelMode.GetComponent<RectTransform>(), 0f);

            var modeHeaderGo = CreateUIObject("Header_GameMode", panelMode.transform);
            SetTopLeft(modeHeaderGo.GetComponent<RectTransform>(), 124f, -430f, 460f, 38f);
            var modeHeaderTmp = CreateTMP(modeHeaderGo, fontType, "GAME MODE", 26f, new Color(0.90f, 0.89f, 0.86f, 0.92f), TextAlignmentOptions.MidlineLeft);
            modeHeaderTmp.characterSpacing = 10f;

            var modeItemsList = new List<MenuItemFX>();
            string[] modeTitles = { "Casual", "Standard", "Insanity" };
            for (int i = 0; i < 3; i++)
            {
                var item = CreateMenuItemRow(panelMode.transform, fontType, i, modeTitles[i], 144f, -500f - i * 64f, 340f, 52f, i == 2);
                modeItemsList.Add(item);
            }
            modeItemsList[1].SetSelected(true, false);

            // =================================================================
            // PANEL 4: SETTINGS
            // =================================================================
            var panelSettings = BuildSettingsPanel(canvasUi.transform, fontTitle, fontType, fontThai, sprScanline, sprShrine, settingsMgr, staticCtrl);

            // =================================================================
            // PANEL 5: CREDITS
            // =================================================================
            var panelCredits = BuildCreditsPanel(canvasUi.transform, fontType, fontThai, out var creditsScrollRt);

            // =================================================================
            // MODAL: CONFIRM QUIT / OVERWRITE
            // =================================================================
            var modalConfirm = BuildConfirmModal(canvasUi.transform, fontType, out var confirmPromptTmp, out var confirmItemsList);

            // 5. Canvas_FX (SortOrder 100)
            var canvasFx = CreateScreenCanvas("Canvas_FX", 100);
            var brightGo = CreateUIObject("FX_BrightnessOverlay", canvasFx.transform);
            StretchFull(brightGo.GetComponent<RectTransform>(), 0f);
            var brightImg = brightGo.AddComponent<Image>();
            brightImg.color = new Color(0f, 0f, 0f, 0f);
            brightImg.raycastTarget = false;

            var fadeBlackGo = CreateUIObject("FX_FadeBlack", canvasFx.transform);
            StretchFull(fadeBlackGo.GetComponent<RectTransform>(), 0f);
            var fadeBlackImg = fadeBlackGo.AddComponent<Image>();
            fadeBlackImg.color = new Color(0.02f, 0.02f, 0.02f, 0f);
            fadeBlackImg.raycastTarget = false;
            fadeBlackGo.SetActive(false);

            var powerLineGo = CreateUIObject("FX_CRTPowerLine", canvasFx.transform);
            var powerLineRt = powerLineGo.GetComponent<RectTransform>();
            powerLineRt.anchorMin = new Vector2(0.5f, 0.5f);
            powerLineRt.anchorMax = new Vector2(0.5f, 0.5f);
            powerLineRt.sizeDelta = new Vector2(1920f, 4f);
            var powerLineImg = powerLineGo.AddComponent<Image>();
            powerLineImg.color = new Color(0.95f, 0.96f, 0.94f, 0f);
            powerLineImg.raycastTarget = false;
            powerLineGo.SetActive(false);

            var powerTrans = sysGo.AddComponent<CRTPowerTransition>();
            SetSerializedField(powerTrans, "powerLineRect", powerLineRt);
            SetSerializedField(powerTrans, "powerLineImage", powerLineImg);
            SetSerializedField(powerTrans, "fadeBlackImage", fadeBlackImg);

            SetSerializedField(settingsMgr, "brightnessOverlayImage", brightImg);

            // Wire MainMenuController
            SetSerializedField(mainCtrl, "gameSceneName", "test");
            SetSerializedField(mainCtrl, "saveKey", "SC_HasSave");
            SetSerializedField(mainCtrl, "inSceneOverlayMode", false);
            SetSerializedField(mainCtrl, "panelBoot", panelBoot);
            SetSerializedField(mainCtrl, "panelMain", panelMain);
            SetSerializedField(mainCtrl, "panelGameMode", panelMode);
            SetSerializedField(mainCtrl, "panelSettings", panelSettings);
            SetSerializedField(mainCtrl, "panelCredits", panelCredits);
            SetSerializedField(mainCtrl, "modalConfirm", modalConfirm);
            SetSerializedField(mainCtrl, "titleBlockRoot", titleBlockGo);
            SetSerializedField(mainCtrl, "bootPlayIconRect", playRt);
            SetSerializedField(mainCtrl, "bootCanvasGroup", bootCg);
            SetSerializedField(mainCtrl, "bootPressKeyText", pressKeyTmp);
            SetSerializedField(mainCtrl, "titleGlitch", titleGlitch);
            SetSerializedField(mainCtrl, "titleExtrude", titleExtrude);
            SetSerializedField(mainCtrl, "stampTypewriter", stampTypewriter);
            SetSerializedField(mainCtrl, "mainDescTypewriter", mainDescTypewriter);
            SetSerializedField(mainCtrl, "mainDescRect", mainDescGo.GetComponent<RectTransform>());
            SetSerializedList(mainCtrl, "mainItems", mainItemsList);
            SetSerializedList(mainCtrl, "gameModeItems", modeItemsList);
            SetSerializedField(mainCtrl, "confirmPromptText", confirmPromptTmp);
            SetSerializedList(mainCtrl, "confirmItems", confirmItemsList);
            SetSerializedField(mainCtrl, "creditsScrollRect", creditsScrollRt);
            SetSerializedField(mainCtrl, "staticController", staticCtrl);
            SetSerializedField(mainCtrl, "powerTransition", powerTrans);
            SetSerializedField(mainCtrl, "settingsManager", settingsMgr);
            SetSerializedField(mainCtrl, "easterEggs", easterEggs);
            SetSerializedField(mainCtrl, "bottomHintText", hintTmp);

            // Set initial active state
            panelBoot.SetActive(!startInMainPreview);
            panelMain.SetActive(startInMainPreview);
            panelMode.SetActive(false);
            panelSettings.SetActive(false);
            panelCredits.SetActive(false);
            modalConfirm.SetActive(false);
            titleBlockGo.SetActive(startInMainPreview);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            return "Main menu scene built and saved at " + TargetScenePath;
        }

        // =====================================================================
        // HELPER BUILDERS
        // =====================================================================
        private static void BuildFilmStripLeft(Transform parent)
        {
            var stripGo = CreateUIObject("FilmStripLeft", parent);
            var rt = stripGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(48f, 0f);
            rt.sizeDelta = new Vector2(2f, 0f);
            var lineImg = stripGo.AddComponent<Image>();
            lineImg.color = new Color(0.88f, 0.88f, 0.85f, 0.32f);
            lineImg.raycastTarget = false;

            for (int i = -7; i <= 7; i++)
            {
                var tickGo = CreateUIObject($"Tick_{i + 7:00}", stripGo.transform);
                var trt = tickGo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 0.5f);
                trt.anchorMax = new Vector2(0f, 0.5f);
                trt.anchoredPosition = new Vector2(6f, i * 68f);
                trt.sizeDelta = new Vector2(12f, 2f);
                var tImg = tickGo.AddComponent<Image>();
                tImg.color = new Color(0.88f, 0.88f, 0.85f, 0.28f);
                tImg.raycastTarget = false;
            }
        }

        private static GameObject BuildGlitch3DTitle(
            Transform parent,
            TMP_FontAsset fontTitle,
            Sprite sprScanline,
            string titleString,
            float fontSize,
            float tiltAngleY,
            out Extrude3DText extrudeComp,
            out GlitchText glitchComp)
        {
            var rootGo = CreateUIObject("Title_" + titleString.Replace(" ", ""), parent);
            SetTopLeft(rootGo.GetComponent<RectTransform>(), 0f, 0f, 1180f, 132f);
            rootGo.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, tiltAngleY, 0f);

            var extrudeGo = CreateUIObject("ExtrudeContainer", rootGo.transform);
            StretchFull(extrudeGo.GetComponent<RectTransform>(), 0f);

            var redGo = CreateUIObject("Glitch_Red", rootGo.transform);
            StretchFull(redGo.GetComponent<RectTransform>(), 0f);
            redGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2.5f, 0f);
            var redTmp = CreateTMP(redGo, fontTitle, titleString, fontSize, new Color(1.0f, 0.16f, 0.16f, 0.62f), TextAlignmentOptions.MidlineLeft);
            redTmp.characterSpacing = 4f;
            redTmp.textWrappingMode = TextWrappingModes.NoWrap;

            var cyanGo = CreateUIObject("Glitch_Cyan", rootGo.transform);
            StretchFull(cyanGo.GetComponent<RectTransform>(), 0f);
            cyanGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(2.5f, 0f);
            var cyanTmp = CreateTMP(cyanGo, fontTitle, titleString, fontSize, new Color(0.0f, 0.90f, 1.0f, 0.62f), TextAlignmentOptions.MidlineLeft);
            cyanTmp.characterSpacing = 4f;
            cyanTmp.textWrappingMode = TextWrappingModes.NoWrap;

            var faceGo = CreateUIObject("Face", rootGo.transform);
            StretchFull(faceGo.GetComponent<RectTransform>(), 0f);
            var faceTmp = CreateTMP(faceGo, fontTitle, titleString, fontSize, Color.white, TextAlignmentOptions.MidlineLeft);
            faceTmp.characterSpacing = 4f;
            faceTmp.textWrappingMode = TextWrappingModes.NoWrap;
            faceTmp.enableVertexGradient = true;
            faceTmp.colorGradient = new VertexGradient(
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 1f),
                new Color(0.72f, 0.75f, 0.78f, 1f),
                new Color(0.72f, 0.75f, 0.78f, 1f)
            );

            var scanGo = CreateUIObject("ScanlineMask", rootGo.transform);
            StretchFull(scanGo.GetComponent<RectTransform>(), 0f);
            var scanImg = scanGo.AddComponent<Image>();
            scanImg.sprite = sprScanline;
            scanImg.type = Image.Type.Tiled;
            scanImg.color = new Color(1f, 1f, 1f, 0.35f);
            scanImg.raycastTarget = false;

            extrudeComp = rootGo.AddComponent<Extrude3DText>();
            SetSerializedField(extrudeComp, "faceText", faceTmp);
            SetSerializedField(extrudeComp, "extrudeContainer", extrudeGo.GetComponent<RectTransform>());
            extrudeComp.RebuildExtrudeLayers();

            glitchComp = rootGo.AddComponent<GlitchText>();
            SetSerializedField(glitchComp, "rootRect", rootGo.GetComponent<RectTransform>());
            SetSerializedField(glitchComp, "redLayerRect", redGo.GetComponent<RectTransform>());
            SetSerializedField(glitchComp, "cyanLayerRect", cyanGo.GetComponent<RectTransform>());
            SetSerializedField(glitchComp, "faceText", faceTmp);
            SetSerializedField(glitchComp, "redText", redTmp);
            SetSerializedField(glitchComp, "cyanText", cyanTmp);

            return rootGo;
        }

        private static MenuItemFX CreateMenuItemRow(
            Transform parent,
            TMP_FontAsset font,
            int index,
            string label,
            float x,
            float y,
            float width,
            float height,
            bool isDanger)
        {
            var rowGo = CreateUIObject($"MenuItem_{index:00}", parent);
            SetTopLeft(rowGo.GetComponent<RectTransform>(), x, y, width, height);
            var hitImg = rowGo.AddComponent<Image>();
            hitImg.color = new Color(0f, 0f, 0f, 0.01f); // transparent raycast target

            var barGo = CreateUIObject("SelectBar", rowGo.transform);
            StretchFull(barGo.GetComponent<RectTransform>(), 0f);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.localScale = new Vector3(0f, 1f, 1f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = isDanger ? new Color(0.90f, 0.10f, 0.10f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
            barImg.raycastTarget = false;

            var lblGo = CreateUIObject("Label", rowGo.transform);
            StretchFull(lblGo.GetComponent<RectTransform>(), 0f);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchoredPosition = new Vector2(8f, 0f);
            var lblTmp = CreateTMP(lblGo, font, label, 30f, new Color(0.55f, 0.55f, 0.55f, 1f), TextAlignmentOptions.MidlineLeft);
            lblTmp.fontStyle = FontStyles.Bold;

            var fx = rowGo.AddComponent<MenuItemFX>();
            SetSerializedField(fx, "itemIndex", index);
            SetSerializedField(fx, "barRect", barRt);
            SetSerializedField(fx, "barImage", barImg);
            SetSerializedField(fx, "labelRect", lblRt);
            SetSerializedField(fx, "labelText", lblTmp);
            SetSerializedField(fx, "isDanger", isDanger);
            SetSerializedField(fx, "interactable", true);
            return fx;
        }

        private static GameObject BuildSettingsPanel(
            Transform parent,
            TMP_FontAsset fontTitle,
            TMP_FontAsset fontType,
            TMP_FontAsset fontThai,
            Sprite sprScanline,
            Sprite sprShrine,
            SettingsManager settingsMgr,
            CRTStaticController staticCtrl)
        {
            var panel = CreateUIObject("Panel_Settings", parent);
            StretchFull(panel.GetComponent<RectTransform>(), 0f);

            // Top-Left Breadcrumb
            var bcGo = CreateUIObject("Breadcrumb", panel.transform);
            SetTopLeft(bcGo.GetComponent<RectTransform>(), 118f, -92f, 680f, 32f);
            var bcTmp = CreateTMP(bcGo, fontType, "OPTIONS  /  VIDEO  (Q / E)", 20f, new Color(0.68f, 0.72f, 0.74f, 0.85f), TextAlignmentOptions.MidlineLeft);

            // 3D Extruded "OPTIONS" Header on Left
            var optHeaderHolder = CreateUIObject("Header3D_Holder", panel.transform);
            SetTopLeft(optHeaderHolder.GetComponent<RectTransform>(), 118f, -135f, 620f, 150f);
            BuildGlitch3DTitle(optHeaderHolder.transform, fontTitle, sprScanline, "OPTIONS", 124f, 18f, out var optExtrude, out _);

            // Left Column: 5 Right-Aligned Categories
            var catListGo = CreateUIObject("CategoryList", panel.transform);
            SetTopLeft(catListGo.GetComponent<RectTransform>(), 80f, -310f, 640f, 360f);
            var catListComp = catListGo.AddComponent<SettingsCategoryList>();

            string[] catLabels =
            {
                "VIDEO",
                "AUDIO",
                "CONTROLS",
                "GAMEPLAY",
                "ACCESSIBILITY"
            };
            var catBtns = new List<SettingsCategoryButton>();
            for (int i = 0; i < catLabels.Length; i++)
            {
                var cGo = CreateUIObject($"Category_{i:00}", catListGo.transform);
                SetTopLeft(cGo.GetComponent<RectTransform>(), 0f, -i * 62f, 640f, 54f);
                var hitImg = cGo.AddComponent<Image>();
                hitImg.color = new Color(0f, 0f, 0f, 0.01f); // full row clickable

                var lblGo = CreateUIObject("Label", cGo.transform);
                StretchFull(lblGo.GetComponent<RectTransform>(), 0f);
                var cTmp = CreateTMP(lblGo, fontTitle, catLabels[i], 38f,
                    i == 0 ? new Color(0.96f, 0.96f, 0.94f, 1f) : new Color(0.45f, 0.48f, 0.50f, 0.55f),
                    TextAlignmentOptions.MidlineRight);
                cTmp.fontStyle = FontStyles.Bold;
                cTmp.characterSpacing = 6f;
                cTmp.textWrappingMode = TextWrappingModes.NoWrap;
                cTmp.raycastTarget = false;

                var cBtn = cGo.AddComponent<SettingsCategoryButton>();
                SetSerializedField(cBtn, "categoryIndex", i);
                SetSerializedField(cBtn, "rectTransform", cGo.GetComponent<RectTransform>());
                SetSerializedField(cBtn, "labelText", cTmp);
                catBtns.Add(cBtn);
            }
            SetSerializedList(catListComp, "categoryButtons", catBtns);

            // Shrine Calibration Box
            var calibBoxGo = CreateUIObject("Shrine_CalibrationBox", panel.transform);
            SetTopLeft(calibBoxGo.GetComponent<RectTransform>(), 118f, -690f, 520f, 116f);
            var calibBg = calibBoxGo.AddComponent<Image>();
            calibBg.color = new Color(0.03f, 0.03f, 0.03f, 0.92f);

            var shrineIconGo = CreateUIObject("Shrine_Silhouette", calibBoxGo.transform);
            SetTopLeft(shrineIconGo.GetComponent<RectTransform>(), 22f, -10f, 96f, 96f);
            var shrineImg = shrineIconGo.AddComponent<Image>();
            shrineImg.sprite = sprShrine;
            shrineImg.color = new Color(0.88f, 0.88f, 0.85f, 0.42f);

            var calibTxtGo = CreateUIObject("Calibration_Hint", calibBoxGo.transform);
            SetTopLeft(calibTxtGo.GetComponent<RectTransform>(), 132f, -20f, 360f, 76f);
            var calibTmp = CreateTMP(calibTxtGo, fontType, "BRIGHTNESS CALIBRATION:\nAdjust until the shrine is barely visible", 17f, new Color(0.72f, 0.74f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft);
            calibTmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Clickable Back Button in Settings Panel
            var backBtnGo = CreateUIObject("Btn_BackToMenu", panel.transform);
            SetTopLeft(backBtnGo.GetComponent<RectTransform>(), 118f, -828f, 310f, 50f);
            var backBtnImg = backBtnGo.AddComponent<Image>();
            backBtnImg.color = new Color(0.14f, 0.16f, 0.18f, 0.92f);
            var backBtn = backBtnGo.AddComponent<Button>();
            var backLblGo = CreateUIObject("Label", backBtnGo.transform);
            StretchFull(backLblGo.GetComponent<RectTransform>(), 0f);
            var backLblTmp = CreateTMP(backLblGo, fontType, "<  BACK TO MENU", 22f, new Color(0.95f, 0.95f, 0.92f, 1f), TextAlignmentOptions.Center);
            backLblTmp.fontStyle = FontStyles.Bold;

            // Right Panel: Dark Charcoal Rows Table
            var rightPanelGo = CreateUIObject("RowsPanel_Right", panel.transform);
            SetTopLeft(rightPanelGo.GetComponent<RectTransform>(), 790f, -180f, 1020f, 698f);
            var rpImg = rightPanelGo.AddComponent<Image>();
            rpImg.color = new Color(0.06f, 0.07f, 0.08f, 0.86f);

            var topBorderGo = CreateUIObject("TopBorder", rightPanelGo.transform);
            SetTopLeft(topBorderGo.GetComponent<RectTransform>(), 0f, 0f, 1020f, 3f);
            topBorderGo.AddComponent<Image>().color = new Color(0.85f, 0.88f, 0.90f, 0.45f);

            var rowsList = new List<SettingsRow>();
            string[] previewLabels =
            {
                "BRIGHTNESS",
                "CRT / SCANLINE EFFECT",
                "FILM GRAIN",
                "DISPLAY MODE",
                "RESOLUTION",
                "V-SYNC",
                "FPS LIMIT"
            };
            string[] previewValues =
            {
                "< [|||||.....] 50 >",
                "HIGH",
                "ON",
                "FULLSCREEN",
                "1920x1080",
                "ON",
                "60 FPS"
            };

            for (int i = 0; i < 7; i++)
            {
                var rGo = CreateUIObject($"SettingsRow_{i:00}", rightPanelGo.transform);
                SetTopLeft(rGo.GetComponent<RectTransform>(), 12f, -18f - i * 64f, 996f, 54f);
                var hlImg = rGo.AddComponent<Image>();
                hlImg.color = i == 0 ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0f, 0f, 0f, 0f);

                var lGo = CreateUIObject("RowLabel", rGo.transform);
                SetTopLeft(lGo.GetComponent<RectTransform>(), 24f, 0f, 520f, 54f);
                var lTmp = CreateTMP(lGo, fontType, previewLabels[i], 23f,
                    i == 0 ? new Color(0.04f, 0.04f, 0.04f, 1f) : new Color(0.82f, 0.85f, 0.86f, 1f),
                    TextAlignmentOptions.MidlineLeft);
                lTmp.fontStyle = FontStyles.Bold;
                var scram = lGo.AddComponent<ScrambleText>();
                SetSerializedField(scram, "targetText", lTmp);

                var vGo = CreateUIObject("RowValue", rGo.transform);
                SetTopLeft(vGo.GetComponent<RectTransform>(), 540f, 0f, 432f, 54f);
                var vTmp = CreateTMP(vGo, fontType, previewValues[i], 23f,
                    i == 0 ? new Color(0.04f, 0.04f, 0.04f, 1f) : new Color(0.92f, 0.92f, 0.90f, 1f),
                    TextAlignmentOptions.MidlineRight);
                vTmp.fontStyle = FontStyles.Bold;

                var rowComp = rGo.AddComponent<SettingsRow>();
                SetSerializedField(rowComp, "rowIndex", i);
                SetSerializedField(rowComp, "highlightBar", hlImg);
                SetSerializedField(rowComp, "labelText", lTmp);
                SetSerializedField(rowComp, "valueText", vTmp);
                SetSerializedField(rowComp, "scrambleLabel", scram);
                rowsList.Add(rowComp);
            }

            // Bottom description box inside RowsPanel_Right
            var descDividerGo = CreateUIObject("DescDivider", rightPanelGo.transform);
            SetTopLeft(descDividerGo.GetComponent<RectTransform>(), 20f, -482f, 980f, 2f);
            descDividerGo.AddComponent<Image>().color = new Color(0.85f, 0.88f, 0.90f, 0.22f);

            var descTxtGo = CreateUIObject("SettingDescriptionText", rightPanelGo.transform);
            SetTopLeft(descTxtGo.GetComponent<RectTransform>(), 28f, -500f, 964f, 175f);
            var descTmp = CreateTMP(descTxtGo, fontType,
                "Adjust overall room & monitor brightness so the shrine silhouette is barely visible.",
                20f, new Color(0.75f, 0.82f, 0.85f, 0.92f), TextAlignmentOptions.TopLeft);
            var descTypewriter = descTxtGo.AddComponent<TypewriterText>();
            SetSerializedField(descTypewriter, "targetText", descTmp);
            SetSerializedField(descTypewriter, "charDelay", 0.01f);

            SetSerializedField(settingsMgr, "breadcrumbText", bcTmp);
            SetSerializedField(settingsMgr, "header3DText", optExtrude);
            SetSerializedField(settingsMgr, "descriptionTypewriter", descTypewriter);
            SetSerializedField(settingsMgr, "categoryList", catListComp);
            SetSerializedList(settingsMgr, "rows", rowsList);
            SetSerializedField(settingsMgr, "backButton", backBtn);
            SetSerializedField(settingsMgr, "staticController", staticCtrl);
            SetSerializedField(settingsMgr, "shrineCalibrationBox", calibBoxGo);
            SetSerializedField(settingsMgr, "shrineSilhouetteImage", shrineImg);

            return panel;
        }

        private static GameObject BuildCreditsPanel(Transform parent, TMP_FontAsset fontType, TMP_FontAsset fontThai, out RectTransform scrollRt)
        {
            var panel = CreateUIObject("Panel_Credits", parent);
            StretchFull(panel.GetComponent<RectTransform>(), 0f);

            var maskGo = CreateUIObject("Credits_Mask", panel.transform);
            SetTopLeft(maskGo.GetComponent<RectTransform>(), 620f, -420f, 980f, 520f);
            maskGo.AddComponent<RectMask2D>();

            var scrollGo = CreateUIObject("Credits_ScrollContent", maskGo.transform);
            scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0f);
            scrollRt.anchorMax = new Vector2(0.5f, 0f);
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.anchoredPosition = new Vector2(0f, 480f);
            scrollRt.sizeDelta = new Vector2(920f, 640f);

            var bannerGo = CreateUIObject("Credits_Banner", scrollGo.transform);
            SetTopLeft(bannerGo.GetComponent<RectTransform>(), 120f, -10f, 680f, 44f);
            bannerGo.AddComponent<Image>().color = new Color(0.94f, 0.94f, 0.92f, 1f);
            var bTxtGo = CreateUIObject("BannerText", bannerGo.transform);
            StretchFull(bTxtGo.GetComponent<RectTransform>(), 0f);
            var bTmp = CreateTMP(bTxtGo, fontType, "JOB HAUNTER  //  SECURITY LOG", 24f, new Color(0.05f, 0.05f, 0.05f, 1f), TextAlignmentOptions.Center);
            bTmp.characterSpacing = 12f;

            var bodyGo = CreateUIObject("Credits_Body", scrollGo.transform);
            SetTopLeft(bodyGo.GetComponent<RectTransform>(), 60f, -80f, 800f, 520f);
            string creditsBody =
                "CORE GAME DESIGN & DIRECTION\n" +
                "Job Haunter Development Team\n\n" +
                "SYSTEMS & TERMINAL UI\n" +
                "- Cyber-Scam Inspection Terminal (GMAIL & WEBSITE)\n" +
                "- Midnight Ghost Phone System\n" +
                "- Shrine Ward, 7-Second Prayer & Sanity Mechanic\n\n" +
                "-- 06:00 AM : NIGHT COMPLETE --";
            CreateTMP(bodyGo, fontType, creditsBody, 24f, new Color(0.90f, 0.90f, 0.88f, 0.95f), TextAlignmentOptions.Top);

            return panel;
        }

        private static GameObject BuildConfirmModal(
            Transform parent,
            TMP_FontAsset fontType,
            out TMP_Text promptTmp,
            out List<MenuItemFX> confirmItems)
        {
            var modal = CreateUIObject("Modal_Confirm", parent);
            StretchFull(modal.GetComponent<RectTransform>(), 0f);
            var dimImg = modal.AddComponent<Image>();
            dimImg.color = new Color(0.01f, 0.01f, 0.01f, 0.78f);

            var boxGo = CreateUIObject("Dialog_Box", modal.transform);
            var boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(780f, 270f);
            boxGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.07f, 0.98f);

            var borderGo = CreateUIObject("Border_Top", boxGo.transform);
            SetTopLeft(borderGo.GetComponent<RectTransform>(), 0f, 0f, 780f, 4f);
            borderGo.AddComponent<Image>().color = new Color(0.90f, 0.10f, 0.10f, 0.9f);

            var pGo = CreateUIObject("Prompt_Text", boxGo.transform);
            SetTopLeft(pGo.GetComponent<RectTransform>(), 40f, -42f, 700f, 90f);
            promptTmp = CreateTMP(pGo, fontType, "LEAVE SHIFT BEFORE 06:00 AM? ARE YOU SURE YOU WANT TO QUIT?", 24f, new Color(0.94f, 0.94f, 0.92f, 1f), TextAlignmentOptions.Center);

            confirmItems = new List<MenuItemFX>();
            var stayBtn = CreateMenuItemRow(boxGo.transform, fontType, 0, "Stay", 90f, -165f, 270f, 52f, false);
            var quitBtn = CreateMenuItemRow(boxGo.transform, fontType, 1, "Quit", 420f, -165f, 270f, 52f, true);
            confirmItems.Add(stayBtn);
            confirmItems.Add(quitBtn);

            return modal;
        }

        private static void WireAudioSystem(GameObject sysGo, MenuAudioManager audioMgr)
        {
            var humSrc = sysGo.AddComponent<AudioSource>();
            humSrc.loop = true;
            humSrc.playOnAwake = false;
            humSrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BaseDir + "/Audio/amb_crt_hum.wav");

            var musSrc = sysGo.AddComponent<AudioSource>();
            musSrc.loop = true;
            musSrc.playOnAwake = false;
            musSrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BaseDir + "/Audio/music_menu.wav");

            var sfxSrc = sysGo.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false;

            var ghostSrc = sysGo.AddComponent<AudioSource>();
            ghostSrc.playOnAwake = false;
            ghostSrc.panStereo = -0.55f;

            SetSerializedField(audioMgr, "humLoopSource", humSrc);
            SetSerializedField(audioMgr, "musicLoopSource", musSrc);
            SetSerializedField(audioMgr, "sfxSource", sfxSrc);
            SetSerializedField(audioMgr, "ghostSource", ghostSrc);

            string[] clipKeys =
            {
                "crt_on", "crt_off", "ui_hover_tick", "ui_select", "ui_back",
                "ui_deny", "ui_value_tick", "typewriter_key", "typewriter_ding",
                "glitch_burst", "phone_ring_far"
            };

            var so = new SerializedObject(audioMgr);
            var clipsProp = so.FindProperty("clips");
            if (clipsProp != null)
            {
                clipsProp.arraySize = clipKeys.Length;
                for (int i = 0; i < clipKeys.Length; i++)
                {
                    string k = clipKeys[i];
                    var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{BaseDir}/Audio/{k}.wav");
                    var elem = clipsProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("key").stringValue = k;
                    elem.FindPropertyRelative("clip").objectReferenceValue = c;
                    elem.FindPropertyRelative("defaultVolume").floatValue = (k == "typewriter_key") ? 0.35f : 0.75f;
                    elem.FindPropertyRelative("isGhostChannel").boolValue = (k == "phone_ring_far");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Canvas CreateScreenCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;

            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
            return c;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static TextMeshProUGUI CreateTMP(GameObject go, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions align)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetSerializedField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.stringValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedField(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedList<T>(Object target, string fieldName, List<T> list) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
