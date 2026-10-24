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
using Pete.UI;
using SecurityCenter.MainMenu;

namespace SecurityCenter.MainMenu.Editor
{
    public static class TestSceneFullUIBuilder
    {
        private const string TestScenePath = "Assets/Cee/test.unity";
        private const string BaseDir = "Assets/MainMenu";

        [MenuItem("Tools/Security Center/Build All UIs In test.unity")]
        public static string BuildAllInTestScene()
        {
            // First rebuild Main menu.unity so it stays in sync, and generate all procedural textures/fonts
            MainMenuAssetGenerator.GenerateAllAssets();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TestScenePath)
            {
                scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            }

            var fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_TitleFont_SDF.asset");
            var fontType = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_Typewriter_SDF.asset");
            var fontThai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BaseDir + "/Fonts/MM_ThaiUI_SDF.asset");
            if (fontThai == null) fontThai = TMP_Settings.defaultFontAsset;
            if (fontType == null) fontType = fontThai;
            if (fontTitle == null) fontTitle = fontThai;

            var sprScanline = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/ScanlineMask.png");
            var sprFolder = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/RetroFolderGreen.png");
            var sprGlassCrack = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/GlassCrackOverlay.png");
            var sprPhoneAccept = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/PhoneBtnAccept.png");
            var sprPhoneDecline = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/PhoneBtnDecline.png");
            var sprAlmondEye = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/HorrorAlmondEye.png");
            var sprEyePupil = AssetDatabase.LoadAssetAtPath<Sprite>(BaseDir + "/Textures/HorrorEyePupil.png");

            // Ensure EventSystem exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            // Locate GameUI_Canvas
            var gameCanvasGo = GameObject.Find("GameUI_Canvas");
            if (gameCanvasGo == null)
            {
                return "Error: GameUI_Canvas not found in test.unity";
            }
            var gameCanvas = gameCanvasGo.GetComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameCanvas.sortingOrder = 10;

            // 1. Rebuild ComputerUI inside GameUI_Canvas
            var compUiGo = FindChildTransform(gameCanvasGo.transform, "ComputerUI")?.gameObject;
            if (compUiGo == null)
            {
                compUiGo = CreateUI("ComputerUI", gameCanvasGo.transform);
                Stretch(compUiGo.GetComponent<RectTransform>(), 0f);
            }
            BuildComputerUIHierarchy(compUiGo, fontThai, fontType, sprScanline, sprFolder, sprGlassCrack);

            // 2. Rebuild PhoneUI inside GameUI_Canvas
            var phoneUiGo = FindChildTransform(gameCanvasGo.transform, "PhoneUI")?.gameObject;
            if (phoneUiGo == null)
            {
                phoneUiGo = CreateUI("PhoneUI", gameCanvasGo.transform);
                Stretch(phoneUiGo.GetComponent<RectTransform>(), 0f);
            }
            BuildPhoneUIHierarchy(phoneUiGo, fontThai, fontType, fontTitle, sprScanline, sprPhoneAccept, sprPhoneDecline, sprAlmondEye, sprEyePupil);

            // 3. Embed Main Menu Overlay Canvases in test.unity (SortingOrder 50, 60, 100)
            BuildEmbeddedMainMenuInTestScene(fontTitle, fontType, fontThai, sprScanline);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Successfully built ComputerUI, PhoneUI, and MainMenu Overlay inside " + TestScenePath;
        }

        // =====================================================================
        // 1. COMPUTER UI BUILDER (Exact match to Screenshot 1 & computer_desktop_ui.html)
        // =====================================================================
        private static void BuildComputerUIHierarchy(
            GameObject compUiGo,
            TMP_FontAsset fontThai,
            TMP_FontAsset fontType,
            Sprite sprScanline,
            Sprite sprFolder,
            Sprite sprGlassCrack)
        {
            ClearChildren(compUiGo.transform);
            Stretch(compUiGo.GetComponent<RectTransform>(), 0f);

            var ctrl = compUiGo.GetComponent<ComputerUIController>();
            if (ctrl == null) ctrl = compUiGo.AddComponent<ComputerUIController>();

            // DesktopRootPanel (Full-screen FNaF viewport container when inspecting Computer)
            var rootPanel = CreateUI("DesktopRootPanel", compUiGo.transform);
            Stretch(rootPanel.GetComponent<RectTransform>(), 0f);
            var rootBg = rootPanel.AddComponent<Image>();
            rootBg.color = new Color(0.02f, 0.04f, 0.03f, 0.94f);

            // Outer Viewport Frame (1640 x 960 centered)
            var viewport = CreateUI("CRT_ViewportContainer", rootPanel.transform);
            SetCentered(viewport.GetComponent<RectTransform>(), 0f, 10f, 1620f, 940f);
            AddBorderedBox(viewport, new Color(0.03f, 0.05f, 0.04f, 1f), new Color(0.11f, 0.22f, 0.14f, 1f), 3f);

            // Top-Left Red Recording Dot ● (Matches Screenshot 1)
            var recDot = CreateUI("RecDot_TopLeft", viewport.transform);
            SetTopLeft(recDot.GetComponent<RectTransform>(), 24f, -20f, 26f, 26f);
            var recDotImg = recDot.AddComponent<Image>();
            recDotImg.color = new Color(0.90f, 0.12f, 0.12f, 0.92f);
            recDotImg.raycastTarget = false;

            // Top-Right Night & Clock HUD (Subtle)
            var hudBar = CreateUI("HUD_ClockNight", viewport.transform);
            SetTopRight(hudBar.GetComponent<RectTransform>(), -28f, -16f, 260f, 30f);
            var timeTmp = CreateText(CreateUI("Time12H", hudBar.transform), fontType, "12 AM", 18f, new Color(0.29f, 0.87f, 0.50f, 0.7f), TextAlignmentOptions.MidlineRight);
            Stretch(timeTmp.GetComponent<RectTransform>(), 0f);
            var nightTmp = CreateText(CreateUI("NightTxt", hudBar.transform), fontType, "NIGHT 1", 18f, new Color(0.29f, 0.87f, 0.50f, 0.7f), TextAlignmentOptions.MidlineLeft);
            Stretch(nightTmp.GetComponent<RectTransform>(), 0f);

            // Retro CRT Monitor Bezel (#cfeacc light retro green-beige frame: 1520 x 810)
            var monitorBezel = CreateUI("CRT_MonitorBezel", viewport.transform);
            var monitorBezelRt = monitorBezel.GetComponent<RectTransform>();
            SetCentered(monitorBezelRt, 0f, 24f, 1520f, 810f);
            AddBorderedBox(monitorBezel, new Color(0.81f, 0.92f, 0.80f, 1f), new Color(0.03f, 0.08f, 0.05f, 1f), 4f);

            // Horizontal stripes across the beige bezel (retro striped look from Screenshot 1)
            var bezelStripes = CreateUI("BezelScanlines", monitorBezel.transform);
            Stretch(bezelStripes.GetComponent<RectTransform>(), 4f);
            var bzScanImg = bezelStripes.AddComponent<Image>();
            bzScanImg.sprite = sprScanline;
            bzScanImg.type = Image.Type.Tiled;
            bzScanImg.color = new Color(0.05f, 0.16f, 0.08f, 0.55f);
            bzScanImg.raycastTarget = false;

            // Inner Dithered Green Frame (1484 x 736 at top of bezel)
            var innerDither = CreateUI("CRT_InnerDitherBezel", monitorBezel.transform);
            SetTopLeft(innerDither.GetComponent<RectTransform>(), 18f, -16f, 1484f, 732f);
            AddBorderedBox(innerDither, new Color(0.15f, 0.28f, 0.16f, 1f), new Color(0.03f, 0.08f, 0.05f, 1f), 3f);

            // Dark Phosphor Green Screen (#020b06) inside inner bezel
            var crtScreen = CreateUI("CRT_PhosphorScreen", innerDither.transform);
            Stretch(crtScreen.GetComponent<RectTransform>(), 16f);
            AddBorderedBox(crtScreen, new Color(0.008f, 0.043f, 0.024f, 1f), new Color(0.08f, 0.24f, 0.14f, 1f), 2f);

            // Power button [O|] on bottom-right of monitor bezel (Matches Screenshot 1)
            var powerBtnGo = CreateUI("Btn_CRTPower", monitorBezel.transform);
            SetBottomRight(powerBtnGo.GetComponent<RectTransform>(), -26f, 10f, 54f, 42f);
            AddBorderedBox(powerBtnGo, new Color(0.81f, 0.92f, 0.80f, 1f), new Color(0.03f, 0.08f, 0.05f, 1f), 3f);
            var powerBtn = powerBtnGo.AddComponent<Button>();
            var pwrCircle = CreateUI("PowerCircle", powerBtnGo.transform);
            SetCentered(pwrCircle.GetComponent<RectTransform>(), 0f, -1f, 22f, 22f);
            AddBorderedBox(pwrCircle, new Color(0.81f, 0.92f, 0.80f, 1f), new Color(0.03f, 0.08f, 0.05f, 1f), 3f);
            var pwrTick = CreateUI("PowerTick", powerBtnGo.transform);
            SetCentered(pwrTick.GetComponent<RectTransform>(), 0f, 7f, 4f, 11f);
            pwrTick.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.05f, 1f);

            // =================================================================
            // SCREEN 1: DESKTOP FOLDERS VIEW (3 Folders with Hover Zoom!)
            // =================================================================
            var desktopFoldersView = CreateUI("Screen1_DesktopFoldersView", crtScreen.transform);
            Stretch(desktopFoldersView.GetComponent<RectTransform>(), 12f);

            // Top-Left Corner Bracket Glint ┏━━ (Matches Screenshot 1)
            var glintH = CreateUI("Glint_H", desktopFoldersView.transform);
            SetTopLeft(glintH.GetComponent<RectTransform>(), 22f, -20f, 136f, 10f);
            glintH.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 0.75f);
            var glintV = CreateUI("Glint_V", desktopFoldersView.transform);
            SetTopLeft(glintV.GetComponent<RectTransform>(), 22f, -20f, 10f, 58f);
            glintV.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 0.75f);

            // Top-Right Replay Animation Button (Matches Screenshot 1: "► เล่นอนิเมชันเข้าหน้านี้ซ้ำ")
            var replayDeskBtnGo = CreateTerminalButton(
                desktopFoldersView.transform, "Btn_ReplayDesktopAnim", fontThai,
                "► เล่นอนิเมชันเข้าหน้านี้ซ้ำ", 20f,
                new Color(0.015f, 0.11f, 0.06f, 1f), new Color(0.08f, 0.50f, 0.24f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f));
            SetTopRight(replayDeskBtnGo.GetComponent<RectTransform>(), -22f, -18f, 360f, 40f);

            // 3 Centered Folder Cards (GMAIL, WEBSITE, RULES)
            var gmailBtn = BuildDesktopFolderCard(
                desktopFoldersView.transform, "Btn_GMAIL", fontType, sprFolder,
                "GMAIL", -390f, -10f, true, new Color(0.94f, 0.22f, 0.22f, 1f), Color.white, "3",
                out var gmailBadgeGo, out var gmailBadgeTxt);

            var webBtn = BuildDesktopFolderCard(
                desktopFoldersView.transform, "Btn_WEBSITE", fontType, sprFolder,
                "WEBSITE", 0f, -10f, true, new Color(0.96f, 0.62f, 0.04f, 1f), Color.black, "3",
                out var webBadgeGo, out var webBadgeTxt);

            var rulesBtn = BuildDesktopFolderCard(
                desktopFoldersView.transform, "Btn_RULES", fontType, sprFolder,
                "RULES", 390f, -10f, false, Color.clear, Color.white, "",
                out _, out _);

            // =================================================================
            // SCREEN 2: GMAIL 3-COLUMN TERMINAL VIEW (Non-Overlapping!)
            // =================================================================
            var gmailWindow = CreateUI("Screen2_GmailTerminalView", crtScreen.transform);
            Stretch(gmailWindow.GetComponent<RectTransform>(), 16f);

            BuildTerminalHeaderBar(
                gmailWindow.transform, fontThai,
                "GMAIL // ข้อความเสนองานทั่วไป (ไม่มีลิงก์แนบ)",
                out var gmailReplayBtn, out var gmailBackBtn);

            var gmailCardsList = new List<CRTMessageCardRefs>();
            for (int i = 0; i < 3; i++)
            {
                float cardX = -465f + i * 465f;
                var cardRefs = BuildMessageCard(gmailWindow.transform, $"GmailCard_{i:00}", fontThai, cardX, -62f, 445f, 595f, false);
                gmailCardsList.Add(cardRefs);
            }
            gmailWindow.SetActive(false);

            // =================================================================
            // SCREEN 3A: WEBSITE 3-COLUMN MESSAGE SELECTOR VIEW (Non-Overlapping!)
            // =================================================================
            var webListWindow = CreateUI("Screen3A_WebsiteListView", crtScreen.transform);
            Stretch(webListWindow.GetComponent<RectTransform>(), 16f);

            BuildTerminalHeaderBar(
                webListWindow.transform, fontThai,
                "WEBSITE // เลือกข้อความที่มีลิงก์เว็บไซต์แนบมา (3)",
                out var webListReplayBtn, out var webListBackBtn);

            var webCardsList = new List<CRTMessageCardRefs>();
            for (int i = 0; i < 3; i++)
            {
                float cardX = -465f + i * 465f;
                var cardRefs = BuildMessageCard(webListWindow.transform, $"WebCard_{i:00}", fontThai, cardX, -62f, 445f, 595f, true);
                webCardsList.Add(cardRefs);
            }
            webListWindow.SetActive(false);

            // =================================================================
            // SCREEN 3B: WEBSITE BROWSER PREVIEW SCREEN (When Link Is Opened!)
            // =================================================================
            var websiteWindow = BuildWebsiteBrowserScreen(
                crtScreen.transform, fontThai,
                out var webCloseBtn, out var webBackToListBtn,
                out var webTabBtns, out var webSecBadgeBg, out var webSecBadgeTxt,
                out var webUrlTxt, out var webNoteTxt, out var webTitleTxt, out var webDescTxt,
                out var webFieldTxts, out var webApproveBtn, out var webDeclineBtn);
            websiteWindow.SetActive(false);

            // =================================================================
            // SCREEN 4: RULES TERMINAL VIEW (Dedicated Non-Overlapping Screen!)
            // =================================================================
            var rulesWindow = BuildRulesTerminalScreen(
                crtScreen.transform, fontThai,
                out var rulesCloseBtn, out var rulesAckBtn, out var rulesContentTmp);
            rulesWindow.SetActive(false);

            // =================================================================
            // SCANLINE OVERLAY, SCAN BEAM, BREACH CRACKS & POWER-OFF OVERLAY
            // =================================================================
            var scanBeamGo = CreateUI("CRT_ScanBeam", crtScreen.transform);
            var scanBeamRt = scanBeamGo.GetComponent<RectTransform>();
            scanBeamRt.anchorMin = new Vector2(0f, 0.5f);
            scanBeamRt.anchorMax = new Vector2(1f, 0.5f);
            scanBeamRt.sizeDelta = new Vector2(0f, 36f);
            scanBeamGo.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 0.28f);
            scanBeamGo.GetComponent<Image>().raycastTarget = false;
            scanBeamGo.SetActive(false);

            var screenScanlines = CreateUI("CRT_ScreenScanlines", crtScreen.transform);
            Stretch(screenScanlines.GetComponent<RectTransform>(), 0f);
            var scImg = screenScanlines.AddComponent<Image>();
            scImg.sprite = sprScanline;
            scImg.type = Image.Type.Tiled;
            scImg.color = new Color(0f, 0f, 0f, 0.36f);
            scImg.raycastTarget = false;

            var breachRedGo = CreateUI("FX_BreachRedOverlay", crtScreen.transform);
            Stretch(breachRedGo.GetComponent<RectTransform>(), 0f);
            var brImg = breachRedGo.AddComponent<Image>();
            brImg.color = new Color(0.85f, 0.08f, 0.08f, 0.55f);
            brImg.raycastTarget = false;
            var breachRedCg = breachRedGo.AddComponent<CanvasGroup>();
            breachRedCg.alpha = 0f;
            breachRedCg.blocksRaycasts = false;

            var breachCrackGo = CreateUI("FX_BreachGlassCracks", crtScreen.transform);
            Stretch(breachCrackGo.GetComponent<RectTransform>(), 0f);
            var crImg = breachCrackGo.AddComponent<Image>();
            crImg.sprite = sprGlassCrack;
            crImg.color = Color.white;
            crImg.raycastTarget = false;
            var breachCrackCg = breachCrackGo.AddComponent<CanvasGroup>();
            breachCrackCg.alpha = 0f;
            breachCrackCg.blocksRaycasts = false;

            var powerOffGo = CreateUI("CRT_PowerOffOverlay", crtScreen.transform);
            Stretch(powerOffGo.GetComponent<RectTransform>(), 0f);
            powerOffGo.AddComponent<Image>().color = new Color(0.005f, 0.02f, 0.01f, 0.99f);
            var pwrLine = CreateUI("CenterLine", powerOffGo.transform);
            SetCentered(pwrLine.GetComponent<RectTransform>(), 0f, 0f, 1400f, 3f);
            pwrLine.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 0.45f);
            powerOffGo.SetActive(false);

            // Bottom-Center Exit Bar: "▼ ถอยออกจากหน้าจอคอม ▼" (Matches Screenshot 1)
            var exitBtnGo = CreateTerminalButton(
                viewport.transform, "Btn_ExitComputer", fontThai,
                "▼  ถอยออกจากหน้าจอคอม  ▼", 22f,
                new Color(0.22f, 0.25f, 0.24f, 0.85f), new Color(0.68f, 0.72f, 0.70f, 0.85f), Color.white);
            SetBottomCenter(exitBtnGo.GetComponent<RectTransform>(), 0f, 16f, 540f, 48f);

            // Wire ComputerUIController
            SetField(ctrl, "desktopRootPanel", rootPanel);
            SetField(ctrl, "desktopFoldersView", desktopFoldersView);
            SetField(ctrl, "timeDisplay12HText", timeTmp);
            SetField(ctrl, "nightDisplayText", nightTmp);
            SetField(ctrl, "exitComputerButton", exitBtnGo.GetComponent<Button>());
            SetField(ctrl, "replayDesktopAnimButton", replayDeskBtnGo.GetComponent<Button>());
            SetField(ctrl, "crtPowerButton", powerBtn);
            SetField(ctrl, "crtPowerOffOverlay", powerOffGo);
            SetField(ctrl, "scanBeamRect", scanBeamRt);

            SetField(ctrl, "gmailIconButton", gmailBtn);
            SetField(ctrl, "gmailBadgeObject", gmailBadgeGo);
            SetField(ctrl, "gmailUnreadCountText", gmailBadgeTxt);

            SetField(ctrl, "websiteIconButton", webBtn);
            SetField(ctrl, "websiteBadgeObject", webBadgeGo);
            SetField(ctrl, "websiteAlertCountText", webBadgeTxt);

            SetField(ctrl, "rulesIconButton", rulesBtn);

            SetField(ctrl, "gmailWindow", gmailWindow);
            SetField(ctrl, "gmailCloseButton", gmailBackBtn);
            SetField(ctrl, "gmailReplayAnimButton", gmailReplayBtn);
            SetList(ctrl, "gmailCards", gmailCardsList);

            SetField(ctrl, "websiteListWindow", webListWindow);
            SetField(ctrl, "websiteListCloseButton", webListBackBtn);
            SetField(ctrl, "websiteListReplayAnimButton", webListReplayBtn);
            SetList(ctrl, "websiteCards", webCardsList);

            SetField(ctrl, "websiteWindow", websiteWindow);
            SetField(ctrl, "websiteCloseButton", webCloseBtn);
            SetField(ctrl, "websiteBackToListButton", webBackToListBtn);
            SetField(ctrl, "websiteUrlText", webUrlTxt);
            SetField(ctrl, "websiteSenderNoteText", webNoteTxt);
            SetField(ctrl, "websiteSecurityBadgeText", webSecBadgeTxt);
            SetField(ctrl, "websiteSecurityBadgeBg", webSecBadgeBg);
            SetField(ctrl, "websiteTitleText", webTitleTxt);
            SetField(ctrl, "websiteDescText", webDescTxt);
            SetList(ctrl, "websiteFormFieldTexts", webFieldTxts);
            SetList(ctrl, "websiteTabButtons", webTabBtns);
            SetField(ctrl, "websiteAcceptButton", webApproveBtn);
            SetField(ctrl, "websiteDeclineButton", webDeclineBtn);

            SetField(ctrl, "rulesWindow", rulesWindow);
            SetField(ctrl, "rulesCloseButton", rulesCloseBtn);
            SetField(ctrl, "rulesAcknowledgeButton", rulesAckBtn);
            SetField(ctrl, "rulesContentText", rulesContentTmp);

            SetField(ctrl, "monitorFrameRect", monitorBezelRt);
            SetField(ctrl, "breachRedOverlay", breachRedCg);
            SetField(ctrl, "breachCrackOverlay", breachCrackCg);

            rootPanel.SetActive(false);
        }

        private static Button BuildDesktopFolderCard(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Sprite sprFolder,
            string label,
            float centerX,
            float centerY,
            bool hasBadge,
            Color badgeBgCol,
            Color badgeTextCol,
            string badgeStr,
            out GameObject badgeGo,
            out TextMeshProUGUI badgeTmp)
        {
            var cardGo = CreateUI(name, parent);
            SetCentered(cardGo.GetComponent<RectTransform>(), centerX, centerY, 340f, 300f);
            AddBorderedBox(cardGo, new Color(0.015f, 0.10f, 0.05f, 0.82f), new Color(0.13f, 0.77f, 0.37f, 0.85f), 2f, out var bgImg, out var borderImg);

            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            var iconWrap = CreateUI("FolderIconWrap", cardGo.transform);
            SetCentered(iconWrap.GetComponent<RectTransform>(), 0f, 26f, 150f, 120f);
            var folderImg = iconWrap.AddComponent<Image>();
            folderImg.sprite = sprFolder;
            folderImg.color = Color.white;
            folderImg.raycastTarget = false;

            badgeGo = CreateUI("Badge", iconWrap.transform);
            SetTopRight(badgeGo.GetComponent<RectTransform>(), 8f, 10f, 44f, 36f);
            AddBorderedBox(badgeGo, badgeBgCol, new Color(0.01f, 0.04f, 0.02f, 1f), 2f);
            badgeTmp = CreateText(CreateUI("Count", badgeGo.transform), font, badgeStr, 22f, badgeTextCol, TextAlignmentOptions.Center);
            badgeTmp.fontStyle = FontStyles.Bold;
            Stretch(badgeTmp.GetComponent<RectTransform>(), 0f);
            badgeGo.SetActive(hasBadge);

            var lblGo = CreateUI("Label", cardGo.transform);
            SetCentered(lblGo.GetComponent<RectTransform>(), 0f, -72f, 280f, 40f);
            var lblTmp = CreateText(lblGo, font, label, 25f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.Center);
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.characterSpacing = 12f;

            var hoverFx = cardGo.AddComponent<FolderHoverZoomFX>();
            hoverFx.iconContainer = iconWrap.GetComponent<RectTransform>();
            hoverFx.cardBorder = borderImg;
            hoverFx.cardBackground = bgImg;
            hoverFx.folderLabel = lblTmp;

            return btn;
        }

        private static void BuildTerminalHeaderBar(
            Transform parent,
            TMP_FontAsset fontThai,
            string title,
            out Button replayBtn,
            out Button backBtn)
        {
            var barGo = CreateUI("TopHeaderBar", parent);
            var rt = barGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 48f);

            var line = CreateUI("BottomLine", barGo.transform);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.sizeDelta = new Vector2(0f, 2f);
            line.AddComponent<Image>().color = new Color(0.09f, 0.40f, 0.20f, 1f);

            var titleGo = CreateUI("HeaderTitle", barGo.transform);
            SetTopLeft(titleGo.GetComponent<RectTransform>(), 4f, -4f, 820f, 38f);
            var tTmp = CreateText(titleGo, fontThai, title, 22f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.MidlineLeft);
            tTmp.fontStyle = FontStyles.Bold;

            var replayGo = CreateTerminalButton(
                barGo.transform, "Btn_ReplayAnim", fontThai,
                "► เล่นอนิเมชันซ้ำ", 19f,
                new Color(0.08f, 0.50f, 0.24f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopRight(replayGo.GetComponent<RectTransform>(), -230f, -4f, 210f, 36f);
            replayBtn = replayGo.GetComponent<Button>();

            var backGo = CreateTerminalButton(
                barGo.transform, "Btn_BackToFolders", fontThai,
                "◄ กลับหน้าโฟลเดอร์", 19f,
                new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.53f, 0.94f, 0.67f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopRight(backGo.GetComponent<RectTransform>(), -4f, -4f, 216f, 36f);
            backBtn = backGo.GetComponent<Button>();
        }

        private static CRTMessageCardRefs BuildMessageCard(
            Transform parent,
            string name,
            TMP_FontAsset fontThai,
            float centerX,
            float topY,
            float w,
            float h,
            bool hasOpenWebButton)
        {
            var refs = new CRTMessageCardRefs();
            var cardGo = CreateUI(name, parent);
            var rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(centerX, topY);
            rt.sizeDelta = new Vector2(w, h);

            AddBorderedBox(cardGo, new Color(0.012f, 0.065f, 0.035f, 1f), new Color(0.18f, 0.65f, 0.34f, 1f), 2f, out var bgImg, out var borderImg);
            refs.cardRoot = cardGo;
            refs.cardBgImage = bgImg;
            refs.cardBorderImage = borderImg;

            // Header Row: ID Label + Category/Status Badge
            var idGo = CreateUI("IdLabel", cardGo.transform);
            SetTopLeft(idGo.GetComponent<RectTransform>(), 16f, -14f, 220f, 32f);
            refs.idText = CreateText(idGo, fontThai, "ข้อความ 01", 21f, new Color(0.53f, 0.94f, 0.67f, 1f), TextAlignmentOptions.MidlineLeft);
            refs.idText.fontStyle = FontStyles.Bold;

            var badgeGo = CreateUI("StatusBadge", cardGo.transform);
            SetTopRight(badgeGo.GetComponent<RectTransform>(), -16f, -14f, 165f, 32f);
            AddBorderedBox(badgeGo, new Color(0.08f, 0.35f, 0.18f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), 1.5f, out var bBg, out _);
            refs.badgeBgImage = bBg;
            refs.badgeText = CreateText(CreateUI("Txt", badgeGo.transform), fontThai, "งานออนไลน์", 18f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.Center);
            refs.badgeText.fontStyle = FontStyles.Bold;
            Stretch(refs.badgeText.GetComponent<RectTransform>(), 2f);

            // Sci-Fi Accent Rule Line
            var ruleLine = CreateUI("RuleDivider", cardGo.transform);
            SetTopLeft(ruleLine.GetComponent<RectTransform>(), 16f, -54f, w - 32f, 3f);
            ruleLine.AddComponent<Image>().color = new Color(0.22f, 0.78f, 0.42f, 0.75f);

            // Sender & Meta Bracket Box
            var metaBox = CreateUI("SenderMetaBox", cardGo.transform);
            SetTopLeft(metaBox.GetComponent<RectTransform>(), 16f, -68f, w - 32f, 92f);
            AddBorderedBox(metaBox, new Color(0.01f, 0.04f, 0.02f, 0.9f), new Color(0.13f, 0.55f, 0.28f, 0.8f), 1.5f);

            var fromGo = CreateUI("FromText", metaBox.transform);
            SetTopLeft(fromGo.GetComponent<RectTransform>(), 12f, -10f, w - 56f, 34f);
            refs.fromText = CreateText(fromGo, fontThai, "จาก: vip-job@fast-money.net", 19f, new Color(0.53f, 0.94f, 0.67f, 1f), TextAlignmentOptions.MidlineLeft);
            refs.fromText.fontStyle = FontStyles.Bold;

            var metaGo = CreateUI("MetaText", metaBox.transform);
            SetTopLeft(metaGo.GetComponent<RectTransform>(), 12f, -48f, w - 56f, 34f);
            refs.metaText = CreateText(metaGo, fontThai, "เงื่อนไข: โอนค่ามัดจำ 1,500฿", 19f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.MidlineLeft);

            // Body Text Box
            float bodyH = hasOpenWebButton ? 280f : 330f;
            var bodyBox = CreateUI("BodyBox", cardGo.transform);
            SetTopLeft(bodyBox.GetComponent<RectTransform>(), 16f, -172f, w - 32f, bodyH);
            AddBorderedBox(bodyBox, new Color(0.015f, 0.08f, 0.04f, 0.95f), new Color(0.29f, 0.87f, 0.50f, 0.85f), 2f);

            var bodyTxtGo = CreateUI("BodyText", bodyBox.transform);
            Stretch(bodyTxtGo.GetComponent<RectTransform>(), 14f);
            refs.bodyText = CreateText(bodyTxtGo, fontThai, "...", 21f, new Color(0.86f, 0.96f, 0.82f, 1f), TextAlignmentOptions.TopLeft);

            // Optional Open Website Button
            if (hasOpenWebButton)
            {
                var openWebGo = CreateTerminalButton(
                    cardGo.transform, "Btn_OpenWebsite", fontThai,
                    "► [ กดเปิดดูหน้าเว็บไซต์ ]", 20f,
                    new Color(0.03f, 0.20f, 0.10f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.53f, 0.94f, 0.67f, 1f));
                SetBottomCenter(openWebGo.GetComponent<RectTransform>(), 0f, 70f, w - 32f, 44f);
                refs.openWebButton = openWebGo.GetComponent<Button>();
            }

            // Bottom Action Row: [+] APPROVE & [X] DECLINE
            float btnW = (w - 44f) * 0.5f;
            var appGo = CreateTerminalButton(
                cardGo.transform, "Btn_Approve", fontThai,
                "[+] APPROVE", 21f,
                new Color(0.13f, 0.77f, 0.37f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetBottomLeft(appGo.GetComponent<RectTransform>(), 16f, 14f, btnW, 46f);
            refs.approveButton = appGo.GetComponent<Button>();

            var decGo = CreateTerminalButton(
                cardGo.transform, "Btn_Decline", fontThai,
                "[X] DECLINE", 21f,
                new Color(0.01f, 0.04f, 0.02f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f));
            SetBottomRight(decGo.GetComponent<RectTransform>(), -16f, 14f, btnW, 46f);
            refs.declineButton = decGo.GetComponent<Button>();

            return refs;
        }

        private static GameObject BuildWebsiteBrowserScreen(
            Transform parent,
            TMP_FontAsset fontThai,
            out Button closeToDesktopBtn,
            out Button backToListBtn,
            out List<Button> tabButtons,
            out Image secBadgeBg,
            out TextMeshProUGUI secBadgeTxt,
            out TextMeshProUGUI urlTxt,
            out TextMeshProUGUI noteTxt,
            out TextMeshProUGUI webTitleTxt,
            out TextMeshProUGUI webDescTxt,
            out List<TextMeshProUGUI> fieldTexts,
            out Button approveBtn,
            out Button declineBtn)
        {
            var win = CreateUI("Screen3B_WebsiteBrowserView", parent);
            Stretch(win.GetComponent<RectTransform>(), 16f);

            // Header bar
            var barGo = CreateUI("TopHeaderBar", win.transform);
            var brt = barGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 48f);

            var tGo = CreateUI("Title", barGo.transform);
            SetTopLeft(tGo.GetComponent<RectTransform>(), 4f, -4f, 680f, 38f);
            var tTmp = CreateText(tGo, fontThai, "BROWSER // หน้าเว็บไซต์ที่เปิดจากลิงก์แนบ", 22f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.MidlineLeft);
            tTmp.fontStyle = FontStyles.Bold;

            var backListGo = CreateTerminalButton(
                barGo.transform, "Btn_BackToWebList", fontThai,
                "◄ กลับหน้าเลือกข้อความเว็บ", 18f,
                new Color(0.08f, 0.50f, 0.24f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopRight(backListGo.GetComponent<RectTransform>(), -230f, -4f, 290f, 36f);
            backToListBtn = backListGo.GetComponent<Button>();

            var closeDeskGo = CreateTerminalButton(
                barGo.transform, "Btn_BackToDesktop", fontThai,
                "◄ กลับหน้าโฟลเดอร์", 18f,
                new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.53f, 0.94f, 0.67f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopRight(closeDeskGo.GetComponent<RectTransform>(), -4f, -4f, 216f, 36f);
            closeToDesktopBtn = closeDeskGo.GetComponent<Button>();

            // Row 1: 3 Website Item Tabs + Security Badge
            tabButtons = new List<Button>();
            for (int i = 0; i < 3; i++)
            {
                var tabGo = CreateTerminalButton(
                    win.transform, $"Btn_WebTab_{i:00}", fontThai,
                    $"► เว็บไซต์ 0{i + 1}", 19f,
                    i == 0 ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.015f, 0.08f, 0.04f, 1f),
                    new Color(0.29f, 0.87f, 0.50f, 1f),
                    i == 0 ? new Color(0.01f, 0.04f, 0.02f, 1f) : new Color(0.29f, 0.87f, 0.50f, 1f));
                SetTopLeft(tabGo.GetComponent<RectTransform>(), i * 250f, -58f, 238f, 38f);
                tabButtons.Add(tabGo.GetComponent<Button>());
            }

            var secGo = CreateUI("SecurityBadge", win.transform);
            SetTopRight(secGo.GetComponent<RectTransform>(), 0f, -58f, 250f, 38f);
            AddBorderedBox(secGo, new Color(0.28f, 0.14f, 0.04f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), 1.5f, out secBadgeBg, out _);
            secBadgeTxt = CreateText(CreateUI("Txt", secGo.transform), fontThai, "[!] HTTP ไม่ปลอดภัย", 19f, new Color(0.98f, 0.80f, 0.20f, 1f), TextAlignmentOptions.Center);
            secBadgeTxt.fontStyle = FontStyles.Bold;
            Stretch(secBadgeTxt.GetComponent<RectTransform>(), 2f);

            // Row 2: URL Bar
            var urlBar = CreateUI("URL_Bar", win.transform);
            SetTopLeft(urlBar.GetComponent<RectTransform>(), 0f, -106f, 1420f, 44f);
            AddBorderedBox(urlBar, new Color(0.015f, 0.08f, 0.04f, 1f), new Color(0.18f, 0.65f, 0.34f, 1f), 2f);

            var urlLbl = CreateUI("UrlLabel", urlBar.transform);
            SetTopLeft(urlLbl.GetComponent<RectTransform>(), 14f, -4f, 230f, 36f);
            var uLblTmp = CreateText(urlLbl, fontThai, "ลิงก์ที่แนบมา (URL):", 19f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.MidlineLeft);
            uLblTmp.fontStyle = FontStyles.Bold;

            var urlValGo = CreateUI("UrlValue", urlBar.transform);
            SetTopLeft(urlValGo.GetComponent<RectTransform>(), 220f, -4f, 680f, 36f);
            urlTxt = CreateText(urlValGo, fontThai, "http://mea-free-refund.net/claim-pin", 20f, Color.white, TextAlignmentOptions.MidlineLeft);
            urlTxt.fontStyle = FontStyles.Bold;

            var noteGo = CreateUI("SenderNote", urlBar.transform);
            SetTopRight(noteGo.GetComponent<RectTransform>(), -14f, -4f, 460f, 36f);
            noteTxt = CreateText(noteGo, fontThai, "งานรับเงินคืนค่าไฟด่วน (แนบลิงก์)", 18f, new Color(0.53f, 0.94f, 0.67f, 1f), TextAlignmentOptions.MidlineRight);

            // Row 3: Webpage Preview Frame
            var previewBox = CreateUI("WebpagePreviewFrame", win.transform);
            SetTopLeft(previewBox.GetComponent<RectTransform>(), 0f, -162f, 1420f, 415f);
            AddBorderedBox(previewBox, new Color(0.012f, 0.065f, 0.035f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), 2f);

            var banner = CreateUI("WebTitleBanner", previewBox.transform);
            SetTopLeft(banner.GetComponent<RectTransform>(), 14f, -14f, 1392f, 42f);
            banner.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 1f);
            var wtGo = CreateUI("WebTitleText", banner.transform);
            Stretch(wtGo.GetComponent<RectTransform>(), 12f);
            webTitleTxt = CreateText(wtGo, fontThai, "[!] ระบบรับเงินคืนค่าไฟด่วน (MEA REFUND)", 22f, new Color(0.01f, 0.04f, 0.02f, 1f), TextAlignmentOptions.MidlineLeft);
            webTitleTxt.fontStyle = FontStyles.Bold;

            var wdGo = CreateUI("WebDescText", previewBox.transform);
            SetTopLeft(wdGo.GetComponent<RectTransform>(), 22f, -68f, 1370f, 44f);
            webDescTxt = CreateText(wdGo, fontThai, "กรุณากรอกข้อมูลเพื่อรับเงินคืน 4,500 บาท เข้าบัญชีทันที:", 21f, new Color(0.86f, 0.96f, 0.82f, 1f), TextAlignmentOptions.MidlineLeft);

            fieldTexts = new List<TextMeshProUGUI>();
            for (int i = 0; i < 3; i++)
            {
                var fRow = CreateUI($"FormField_{i:00}", previewBox.transform);
                SetTopLeft(fRow.GetComponent<RectTransform>(), 22f, -126f - i * 84f, 1376f, 68f);
                AddBorderedBox(fRow, new Color(0.008f, 0.043f, 0.024f, 1f), new Color(0.10f, 0.40f, 0.20f, 1f), 1.5f);

                var flGo = CreateUI("FieldLabel", fRow.transform);
                SetTopLeft(flGo.GetComponent<RectTransform>(), 18f, -10f, 820f, 48f);
                var flTmp = CreateText(flGo, fontThai, "• ข้อมูลที่ต้องกรอก", 21f, new Color(0.53f, 0.94f, 0.67f, 1f), TextAlignmentOptions.MidlineLeft);
                flTmp.fontStyle = FontStyles.Bold;
                fieldTexts.Add(flTmp);

                var fiGo = CreateUI("FieldInputMock", fRow.transform);
                SetTopRight(fiGo.GetComponent<RectTransform>(), -18f, -10f, 420f, 48f);
                CreateText(fiGo, fontThai, "[ ________________________ ]", 20f, new Color(0.18f, 0.75f, 0.38f, 1f), TextAlignmentOptions.MidlineRight);
            }

            // Bottom Action Bar
            var promptGo = CreateUI("JudgePrompt", win.transform);
            SetBottomLeft(promptGo.GetComponent<RectTransform>(), 4f, 10f, 620f, 48f);
            var pTmp = CreateText(promptGo, fontThai, "ตรวจสอบ URL และหน้าเว็บแล้ว เลือกตัดสินใจ:", 21f, new Color(0.53f, 0.94f, 0.67f, 1f), TextAlignmentOptions.MidlineLeft);
            pTmp.fontStyle = FontStyles.Bold;

            var appGo = CreateTerminalButton(
                win.transform, "Btn_WebApprove", fontThai,
                "[+] APPROVE (อนุมัติเว็บนี้)", 21f,
                new Color(0.13f, 0.77f, 0.37f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetBottomRight(appGo.GetComponent<RectTransform>(), -356f, 8f, 340f, 52f);
            approveBtn = appGo.GetComponent<Button>();

            var decGo = CreateTerminalButton(
                win.transform, "Btn_WebDecline", fontThai,
                "[X] DECLINE (บล็อกเว็บปลอม)", 21f,
                new Color(0.01f, 0.04f, 0.02f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f));
            SetBottomRight(decGo.GetComponent<RectTransform>(), -4f, 8f, 340f, 52f);
            declineBtn = decGo.GetComponent<Button>();

            return win;
        }

        private static GameObject BuildRulesTerminalScreen(
            Transform parent,
            TMP_FontAsset fontThai,
            out Button closeBtn,
            out Button ackBtn,
            out TextMeshProUGUI rulesBodyTmp)
        {
            var win = CreateUI("Screen4_RulesTerminalView", parent);
            Stretch(win.GetComponent<RectTransform>(), 16f);

            var barGo = CreateUI("TopHeaderBar", win.transform);
            var brt = barGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 48f);

            var tGo = CreateUI("Title", barGo.transform);
            SetTopLeft(tGo.GetComponent<RectTransform>(), 4f, -4f, 760f, 38f);
            var tTmp = CreateText(tGo, fontThai, "RULES // คู่มือสังเกตกลโกงมิจฉาชีพ", 23f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.MidlineLeft);
            tTmp.fontStyle = FontStyles.Bold;

            var backGo = CreateTerminalButton(
                barGo.transform, "Btn_CloseRules", fontThai,
                "◄ กลับหน้าโฟลเดอร์", 19f,
                new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.53f, 0.94f, 0.67f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopRight(backGo.GetComponent<RectTransform>(), -4f, -4f, 216f, 36f);
            closeBtn = backGo.GetComponent<Button>();

            // Rule Box 1
            var box1 = CreateUI("RuleBox_01", win.transform);
            SetTopLeft(box1.GetComponent<RectTransform>(), 16f, -84f, 1388f, 220f);
            AddBorderedBox(box1, new Color(0.012f, 0.065f, 0.035f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), 2f);
            var b1Head = CreateUI("Head", box1.transform);
            SetTopLeft(b1Head.GetComponent<RectTransform>(), 16f, -16f, 1356f, 42f);
            b1Head.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 1f);
            var b1HeadTmp = CreateText(CreateUI("Txt", b1Head.transform), fontThai, "  1. เกณฑ์ตรวจ GMAIL (ข้อความทั่วไป ไม่มีลิงก์)", 22f, new Color(0.01f, 0.04f, 0.02f, 1f), TextAlignmentOptions.MidlineLeft);
            b1HeadTmp.fontStyle = FontStyles.Bold;
            Stretch(b1HeadTmp.GetComponent<RectTransform>(), 0f);

            var b1Body = CreateUI("Body", box1.transform);
            SetTopLeft(b1Body.GetComponent<RectTransform>(), 24f, -72f, 1340f, 130f);
            rulesBodyTmp = CreateText(b1Body, fontThai,
                "• หากบังคับโอนเงินค่าเปิดรหัส VIP ก่อน, ขอสมุดบัญชี/รหัส ATM (บัญชีม้า), หรือแนบไฟล์นามสกุล <color=#4ade80>.exe</color> ให้กด <color=#86efac>✕ DECLINE</color> ทันที\n" +
                "• หากเป็นเอกสารสัญญาจ้างงานบริษัทจริงที่ไม่มีการเรียกเก็บเงินล่วงหน้า ให้กด <color=#4ade80>✓ APPROVE</color>",
                22f, new Color(0.86f, 0.96f, 0.82f, 1f), TextAlignmentOptions.TopLeft);

            // Rule Box 2
            var box2 = CreateUI("RuleBox_02", win.transform);
            SetTopLeft(box2.GetComponent<RectTransform>(), 16f, -330f, 1388f, 220f);
            AddBorderedBox(box2, new Color(0.012f, 0.065f, 0.035f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), 2f);
            var b2Head = CreateUI("Head", box2.transform);
            SetTopLeft(b2Head.GetComponent<RectTransform>(), 16f, -16f, 1356f, 42f);
            b2Head.AddComponent<Image>().color = new Color(0.29f, 0.87f, 0.50f, 1f);
            var b2HeadTmp = CreateText(CreateUI("Txt", b2Head.transform), fontThai, "  2. เกณฑ์ตรวจ WEBSITE (งานที่แนบลิงก์เว็บไซต์มา)", 22f, new Color(0.01f, 0.04f, 0.02f, 1f), TextAlignmentOptions.MidlineLeft);
            b2HeadTmp.fontStyle = FontStyles.Bold;
            Stretch(b2HeadTmp.GetComponent<RectTransform>(), 0f);

            var b2Body = CreateUI("Body", box2.transform);
            SetTopLeft(b2Body.GetComponent<RectTransform>(), 24f, -72f, 1340f, 130f);
            CreateText(b2Body, fontThai,
                "• ตรวจช่อง <color=#4ade80>URL ลิงก์แนบ</color> ต้องเป็น <color=#86efac>HTTPS</color> ที่ตรงกับชื่อหน่วยงานจริง และไม่หลอกให้กรอกรหัส PIN ธนาคารในหน้าเว็บ\n" +
                "• เว็บปลอมที่เป็น HTTP หรือขอรหัสผ่าน Mobile Banking / PIN ATM 6 หลัก ให้กด <color=#86efac>✕ DECLINE</color> ทันที",
                22f, new Color(0.86f, 0.96f, 0.82f, 1f), TextAlignmentOptions.TopLeft);

            var ackGo = CreateTerminalButton(
                win.transform, "Btn_AcknowledgeRules", fontThai,
                "รับทราบ // กลับหน้าโฟลเดอร์", 21f,
                new Color(0.13f, 0.77f, 0.37f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetBottomRight(ackGo.GetComponent<RectTransform>(), -16f, 12f, 340f, 50f);
            ackBtn = ackGo.GetComponent<Button>();

            return win;
        }

        // =====================================================================
        // 2. PHONE UI BUILDER (Exact match to Screenshots 2, 3, and 4)
        // =====================================================================
        private static void BuildPhoneUIHierarchy(
            GameObject phoneUiGo,
            TMP_FontAsset fontThai,
            TMP_FontAsset fontType,
            TMP_FontAsset fontTitle,
            Sprite sprScanline,
            Sprite sprPhoneAccept,
            Sprite sprPhoneDecline,
            Sprite sprAlmondEye,
            Sprite sprEyePupil)
        {
            ClearChildren(phoneUiGo.transform);
            Stretch(phoneUiGo.GetComponent<RectTransform>(), 0f);

            var ctrl = phoneUiGo.GetComponent<PhoneUIController>();
            if (ctrl == null) ctrl = phoneUiGo.AddComponent<PhoneUIController>();

            var mainPanel = CreateUI("PhoneMainPanel", phoneUiGo.transform);
            Stretch(mainPanel.GetComponent<RectTransform>(), 0f);
            mainPanel.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.94f);

            // Outer Viewport Container (1620 x 940 centered)
            var viewport = CreateUI("Phone_ViewportContainer", mainPanel.transform);
            SetCentered(viewport.GetComponent<RectTransform>(), 0f, 10f, 1620f, 940f);
            AddBorderedBox(viewport, new Color(0.02f, 0.04f, 0.06f, 1f), new Color(0.12f, 0.24f, 0.32f, 1f), 3f);

            // Backdrop 1: Normal Dark Blue/Cyan Scanline Backdrop (Screenshot 2)
            var normalBackdrop = CreateUI("NormalBackdropLayer", viewport.transform);
            Stretch(normalBackdrop.GetComponent<RectTransform>(), 3f);
            normalBackdrop.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.08f, 1f);

            // Backdrop 2: Horror Red/Black Backdrop (Screenshots 3 & 4)
            var horrorBackdrop = CreateUI("HorrorRedBackdropLayer", viewport.transform);
            Stretch(horrorBackdrop.GetComponent<RectTransform>(), 3f);
            horrorBackdrop.AddComponent<Image>().color = new Color(0.16f, 0.015f, 0.015f, 1f);
            horrorBackdrop.SetActive(false);

            var pupilList = new List<RectTransform>();
            var screamTextList = new List<RectTransform>();

            // Top-Left Red Recording Dot ● (Matches Screenshots 2, 3, 4)
            var recDot = CreateUI("RecDot_TopLeft", viewport.transform);
            SetTopLeft(recDot.GetComponent<RectTransform>(), 32f, -26f, 38f, 38f);
            recDot.AddComponent<Image>().color = new Color(0.86f, 0.12f, 0.12f, 0.92f);

            // Top-Right Mode Switcher Bar (Matches Screenshots 2, 3, 4)
            var topBar = CreateUI("TopModeSwitcherBar", viewport.transform);
            SetTopRight(topBar.GetComponent<RectTransform>(), -24f, -18f, 1060f, 58f);
            AddBorderedBox(topBar, new Color(0.02f, 0.04f, 0.06f, 0.95f), new Color(0.14f, 0.26f, 0.34f, 1f), 2f);

            var btnIncomingGo = CreateTerminalButton(
                topBar.transform, "Btn_ModeIncoming", fontThai,
                "► สายเรียกเข้า (Ref)", 19f,
                new Color(0.04f, 0.16f, 0.22f, 1f), new Color(0.22f, 0.68f, 0.82f, 1f), new Color(0.55f, 0.94f, 0.98f, 1f));
            SetTopLeft(btnIncomingGo.GetComponent<RectTransform>(), 10f, -8f, 235f, 42f);

            var btnEyeballGo = CreateTerminalButton(
                topBar.transform, "Btn_ModeGhostEyeball", fontThai,
                "● ผีกระซิบ (ลูกตา)", 19f,
                new Color(0.18f, 0.03f, 0.03f, 1f), new Color(0.85f, 0.18f, 0.18f, 1f), new Color(0.98f, 0.45f, 0.45f, 1f));
            SetTopLeft(btnEyeballGo.GetComponent<RectTransform>(), 255f, -8f, 225f, 42f);

            var btnScreamGo = CreateTerminalButton(
                topBar.transform, "Btn_ModeGhostScream", fontThai,
                "[!] ผีกรี๊ด! (DIE/HELP)", 19f,
                new Color(0.22f, 0.03f, 0.03f, 1f), new Color(0.95f, 0.22f, 0.22f, 1f), new Color(1f, 0.55f, 0.55f, 1f));
            SetTopLeft(btnScreamGo.GetComponent<RectTransform>(), 490f, -8f, 245f, 42f);

            var btnFakeGo = CreateTerminalButton(
                topBar.transform, "Btn_ModeFakeCase", fontThai,
                "เคสปลอม", 19f,
                new Color(0.28f, 0.05f, 0.05f, 1f), new Color(0.92f, 0.22f, 0.22f, 1f), new Color(0.98f, 0.45f, 0.45f, 1f));
            SetTopLeft(btnFakeGo.GetComponent<RectTransform>(), 750f, -8f, 140f, 42f);

            var btnRealGo = CreateTerminalButton(
                topBar.transform, "Btn_ModeRealCase", fontThai,
                "เคสจริง", 19f,
                new Color(0.05f, 0.10f, 0.14f, 1f), new Color(0.24f, 0.34f, 0.42f, 1f), new Color(0.82f, 0.88f, 0.92f, 1f));
            SetTopLeft(btnRealGo.GetComponent<RectTransform>(), 900f, -8f, 145f, 42f);

            // =================================================================
            // SMARTPHONE CENTER FRAME (540 x 760 centered)
            // =================================================================
            var phoneFrame = CreateUI("SmartphoneFrameBezel", viewport.transform);
            var phoneFrameRt = phoneFrame.GetComponent<RectTransform>();
            SetCentered(phoneFrameRt, 0f, -6f, 540f, 760f);
            AddBorderedBox(phoneFrame, new Color(0.05f, 0.07f, 0.09f, 1f), new Color(0.14f, 0.20f, 0.25f, 1f), 5f, out _, out var bezelBorderImg);

            // Top Speaker Slot & Camera Dot on Bezel
            var speakerSlot = CreateUI("SpeakerSlot", phoneFrame.transform);
            SetTopCenter(speakerSlot.GetComponent<RectTransform>(), 14f, -22f, 110f, 8f);
            speakerSlot.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.22f, 1f);
            var camDot = CreateUI("FrontCamDot", phoneFrame.transform);
            SetTopCenter(camDot.GetComponent<RectTransform>(), -62f, -20f, 12f, 12f);
            camDot.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.26f, 1f);

            // Bottom Home Bar on Bezel
            var homeBar = CreateUI("HomeBar", phoneFrame.transform);
            SetBottomCenter(homeBar.GetComponent<RectTransform>(), 0f, 16f, 160f, 7f);
            homeBar.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.26f, 1f);

            // Inner Smartphone Screen (496 x 676)
            var phoneScreen = CreateUI("SmartphoneScreen", phoneFrame.transform);
            SetCentered(phoneScreen.GetComponent<RectTransform>(), 0f, -2f, 496f, 676f);
            AddBorderedBox(phoneScreen, new Color(0.02f, 0.05f, 0.07f, 1f), new Color(0.12f, 0.22f, 0.28f, 1f), 2f, out var screenBgImg, out _);

            // Top Carrier Status Bar ("SEC-NET                  5G |||")
            var carrierGo = CreateUI("CarrierHeader", phoneScreen.transform);
            SetTopCenter(carrierGo.GetComponent<RectTransform>(), 0f, -14f, 450f, 32f);
            var carrierTmp = CreateText(carrierGo, fontType, "SEC-NET                  5G |||", 20f, new Color(0.58f, 0.72f, 0.78f, 0.9f), TextAlignmentOptions.Center);
            carrierTmp.fontStyle = FontStyles.Bold;

            var carrierRule = CreateUI("CarrierDivider", phoneScreen.transform);
            SetTopCenter(carrierRule.GetComponent<RectTransform>(), 0f, -50f, 456f, 2f);
            carrierRule.AddComponent<Image>().color = new Color(0.16f, 0.28f, 0.34f, 0.6f);

            // =================================================================
            // MODE A: RINGING PANEL (Screenshot 2: INCOMING CALL)
            // =================================================================
            var ringingPanel = CreateUI("RingingPanel", phoneScreen.transform);
            Stretch(ringingPanel.GetComponent<RectTransform>(), 0f);

            var incTitleGo = CreateUI("IncomingTitle", ringingPanel.transform);
            SetCentered(incTitleGo.GetComponent<RectTransform>(), 0f, 155f, 440f, 110f);
            var incTmp = CreateText(incTitleGo, fontType, "INCOMING\nCALL", 42f, new Color(0.95f, 0.97f, 0.98f, 1f), TextAlignmentOptions.Center);
            incTmp.fontStyle = FontStyles.Bold;
            incTmp.characterSpacing = 12f;

            var numGo = CreateUI("CallerNumber", ringingPanel.transform);
            SetCentered(numGo.GetComponent<RectTransform>(), 0f, 52f, 440f, 40f);
            var numTmp = CreateText(numGo, fontType, "098-712-XXXX", 24f, new Color(0.42f, 0.90f, 0.96f, 1f), TextAlignmentOptions.Center);
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.characterSpacing = 6f;

            var nameGo = CreateUI("CallerName", ringingPanel.transform);
            SetCentered(nameGo.GetComponent<RectTransform>(), 0f, 12f, 440f, 40f);
            var nameTmp = CreateText(nameGo, fontThai, "\"สภ.เมือง (ฝ่ายสืบสวน)\"", 22f, new Color(0.75f, 0.82f, 0.86f, 1f), TextAlignmentOptions.Center);

            // Round Green Answer Button & Round Red Decline Button (Screenshot 2)
            var ansBtnGo = CreateUI("Btn_AnswerGreen", ringingPanel.transform);
            SetCentered(ansBtnGo.GetComponent<RectTransform>(), -102f, -125f, 108f, 108f);
            var ansImg = ansBtnGo.AddComponent<Image>();
            ansImg.sprite = sprPhoneAccept;
            ansImg.color = Color.white;
            var ansBtn = ansBtnGo.AddComponent<Button>();

            var hangBtnGo = CreateUI("Btn_HangupRed", ringingPanel.transform);
            SetCentered(hangBtnGo.GetComponent<RectTransform>(), 102f, -125f, 108f, 108f);
            var hangImg = hangBtnGo.AddComponent<Image>();
            hangImg.sprite = sprPhoneDecline;
            hangImg.color = Color.white;
            var hangBtn = hangBtnGo.AddComponent<Button>();

            var clockBotGo = CreateUI("ClockBottom", ringingPanel.transform);
            SetBottomCenter(clockBotGo.GetComponent<RectTransform>(), 0f, 38f, 240f, 34f);
            var clockBotTmp = CreateText(clockBotGo, fontType, "3:17 AM", 22f, new Color(0.78f, 0.86f, 0.90f, 0.9f), TextAlignmentOptions.Center);
            clockBotTmp.characterSpacing = 8f;

            // =================================================================
            // MODE B: IN-CALL CONVERSATION PANEL
            // =================================================================
            var inCallPanel = CreateUI("InCallPanel", phoneScreen.transform);
            Stretch(inCallPanel.GetComponent<RectTransform>(), 0f);

            var icStatus = CreateUI("InCallHeader", inCallPanel.transform);
            SetTopCenter(icStatus.GetComponent<RectTransform>(), 0f, -72f, 420f, 36f);
            var icStatTmp = CreateText(icStatus, fontType, "● IN CALL  //  VOICE LINE", 20f, new Color(0.29f, 0.87f, 0.50f, 1f), TextAlignmentOptions.Center);
            icStatTmp.fontStyle = FontStyles.Bold;

            var icNumGo = CreateUI("InCallNumber", inCallPanel.transform);
            SetTopCenter(icNumGo.GetComponent<RectTransform>(), 0f, -112f, 440f, 34f);
            var icNumTmp = CreateText(icNumGo, fontType, "098-712-XXXX", 22f, new Color(0.42f, 0.90f, 0.96f, 1f), TextAlignmentOptions.Center);
            icNumTmp.fontStyle = FontStyles.Bold;

            var icNameGo = CreateUI("InCallName", inCallPanel.transform);
            SetTopCenter(icNameGo.GetComponent<RectTransform>(), 0f, -146f, 440f, 34f);
            var icNameTmp = CreateText(icNameGo, fontThai, "\"สภ.เมือง (ฝ่ายสืบสวน)\"", 20f, new Color(0.78f, 0.84f, 0.88f, 1f), TextAlignmentOptions.Center);

            var dlgBox = CreateUI("DialogueBox", inCallPanel.transform);
            SetCentered(dlgBox.GetComponent<RectTransform>(), 0f, 12f, 444f, 220f);
            AddBorderedBox(dlgBox, new Color(0.01f, 0.03f, 0.05f, 0.95f), new Color(0.20f, 0.55f, 0.68f, 1f), 2f);
            var dlgTxtGo = CreateUI("DialogueText", dlgBox.transform);
            Stretch(dlgTxtGo.GetComponent<RectTransform>(), 18f);
            var dlgTmp = CreateText(dlgTxtGo, fontThai, "...", 22f, new Color(0.92f, 0.98f, 1f, 1f), TextAlignmentOptions.Center);

            var nextDlgBtnGo = CreateTerminalButton(
                inCallPanel.transform, "Btn_NextDialogue", fontThai,
                "► ฟังประโยคถัดไป", 21f,
                new Color(0.05f, 0.24f, 0.34f, 1f), new Color(0.32f, 0.82f, 0.95f, 1f), new Color(0.55f, 0.95f, 1f, 1f));
            SetBottomCenter(nextDlgBtnGo.GetComponent<RectTransform>(), 0f, 110f, 420f, 52f);
            var nextDlgBtn = nextDlgBtnGo.GetComponent<Button>();

            var decisionPanel = CreateUI("DecisionPanel", inCallPanel.transform);
            SetBottomCenter(decisionPanel.GetComponent<RectTransform>(), 0f, 48f, 444f, 120f);

            var accOfferGo = CreateTerminalButton(
                decisionPanel.transform, "Btn_AcceptOffer", fontThai,
                "[+] APPROVE (รับข้อเสนอ)", 20f,
                new Color(0.13f, 0.77f, 0.37f, 1f), new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.01f, 0.04f, 0.02f, 1f));
            SetTopCenter(accOfferGo.GetComponent<RectTransform>(), 0f, 0f, 436f, 52f);
            var accOfferBtn = accOfferGo.GetComponent<Button>();

            var decOfferGo = CreateTerminalButton(
                decisionPanel.transform, "Btn_DeclineOffer", fontThai,
                "[X] DECLINE (วางสายมิจฉาชีพ)", 20f,
                new Color(0.85f, 0.18f, 0.18f, 1f), new Color(0.98f, 0.45f, 0.45f, 1f), Color.white);
            SetBottomCenter(decOfferGo.GetComponent<RectTransform>(), 0f, 0f, 436f, 52f);
            var decOfferBtn = decOfferGo.GetComponent<Button>();

            inCallPanel.SetActive(false);

            // =================================================================
            // MODE C: GHOST WHISPER / ROVING EYEBALLS (Screenshot 3!)
            // =================================================================
            var ghostEyeballRoot = CreateUI("GhostEyeballRoot", viewport.transform);
            Stretch(ghostEyeballRoot.GetComponent<RectTransform>(), 0f);

            // 8 Outer Almond Eyes floating in dark red background outside the phone (Screenshot 3)
            Vector2[] outerEyeCoords =
            {
                new Vector2(-540f, 210f), new Vector2(-410f, 65f), new Vector2(-560f, -65f), new Vector2(-490f, -255f),
                new Vector2(540f, 215f),  new Vector2(420f, 65f),  new Vector2(560f, -45f),  new Vector2(510f, -250f)
            };
            Vector2[] outerEyeSizes =
            {
                new Vector2(240f, 110f), new Vector2(150f, 72f), new Vector2(210f, 54f), new Vector2(220f, 96f),
                new Vector2(245f, 112f), new Vector2(155f, 74f), new Vector2(260f, 115f), new Vector2(225f, 98f)
            };

            // Inside Phone Screen: Black Sphere + Giant Central Eye + 5 Inner Eyes + Warning Box
            var phoneEyeOverlay = CreateUI("PhoneScreen_EyeballContainer", ghostEyeballRoot.transform);
            SetCentered(phoneEyeOverlay.GetComponent<RectTransform>(), 0f, -8f, 492f, 672f);

            var blackSphere = CreateUI("BlackVoidCircle", phoneEyeOverlay.transform);
            SetCentered(blackSphere.GetComponent<RectTransform>(), 0f, 36f, 430f, 430f);
            var sphereImg = blackSphere.AddComponent<Image>();
            sphereImg.sprite = sprPhoneAccept; // circular mask shape tinted pure black
            sphereImg.color = new Color(0.01f, 0.01f, 0.01f, 0.98f);

            // Giant Central Eye (pupilList[0] is the main central pupil!)
            var mainPupil = CreateHorrorAlmondEye(phoneEyeOverlay.transform, "GiantCenterEye", sprAlmondEye, sprEyePupil, Vector2.zero + new Vector2(0f, 22f), new Vector2(350f, 165f), 96f);
            pupilList.Add(mainPupil);

            // 5 Smaller Inner Eyes around the black sphere inside the phone screen (Screenshot 3)
            Vector2[] innerEyePos =
            {
                new Vector2(-150f, 195f), new Vector2(4f, 182f), new Vector2(155f, 192f),
                new Vector2(-165f, 88f),  new Vector2(175f, 82f)
            };
            for (int i = 0; i < innerEyePos.Length; i++)
            {
                var p = CreateHorrorAlmondEye(phoneEyeOverlay.transform, $"InnerEye_{i:00}", sprAlmondEye, sprEyePupil, innerEyePos[i], new Vector2(115f, 56f), 32f);
                pupilList.Add(p);
            }

            // Outer 8 Eyes
            for (int i = 0; i < outerEyeCoords.Length; i++)
            {
                var p = CreateHorrorAlmondEye(ghostEyeballRoot.transform, $"OuterEye_{i:00}", sprAlmondEye, sprEyePupil, outerEyeCoords[i], outerEyeSizes[i], outerEyeSizes[i].y * 0.56f);
                pupilList.Add(p);
            }

            // Bottom Warning Box inside Phone Screen (Screenshot 3: "[!] [สายผีตัดไปแล้ว!] รีบกด A/D หันไปคลิกศาลเจ้าใน 7 วินาที!")
            var eyeWarnBox = CreateUI("GhostWarningBox", phoneEyeOverlay.transform);
            SetBottomCenter(eyeWarnBox.GetComponent<RectTransform>(), 0f, 24f, 456f, 236f);
            AddBorderedBox(eyeWarnBox, new Color(0.02f, 0.01f, 0.01f, 0.96f), new Color(0.68f, 0.12f, 0.12f, 1f), 2f);

            var warnTxtGo = CreateUI("WarningText", eyeWarnBox.transform);
            SetTopCenter(warnTxtGo.GetComponent<RectTransform>(), 0f, -14f, 428f, 118f);
            var warnTmp = CreateText(warnTxtGo, fontThai, "[!] [สายผีตัดไปแล้ว!] รีบกด\nA/D หันไปคลิกศาลเจ้าใน 7\nวินาที!", 23f, new Color(0.98f, 0.72f, 0.72f, 1f), TextAlignmentOptions.Center);
            warnTmp.fontStyle = FontStyles.Bold;

            var warnLine = CreateUI("Divider", eyeWarnBox.transform);
            SetCentered(warnLine.GetComponent<RectTransform>(), 0f, -16f, 420f, 2f);
            warnLine.AddComponent<Image>().color = new Color(0.45f, 0.08f, 0.08f, 1f);

            var cdEyeGo = CreateUI("CountdownLabel", eyeWarnBox.transform);
            SetBottomLeft(cdEyeGo.GetComponent<RectTransform>(), 24f, 18f, 190f, 68f);
            var cdEyeTmp = CreateText(cdEyeGo, fontThai, "[!] สายผีตัด\nใน: 7.0s", 21f, new Color(0.95f, 0.32f, 0.32f, 1f), TextAlignmentOptions.Center);
            cdEyeTmp.fontStyle = FontStyles.Bold;

            var replayEyeBtnGo = CreateTerminalButton(
                eyeWarnBox.transform, "Btn_ReplayEyeballJumpscare", fontThai,
                "เล่นซ้ำ\nJumpscare", 20f,
                new Color(0.58f, 0.10f, 0.10f, 1f), new Color(0.92f, 0.25f, 0.25f, 1f), Color.white);
            SetBottomRight(replayEyeBtnGo.GetComponent<RectTransform>(), -18f, 16f, 206f, 74f);
            var replayEyeBtn = replayEyeBtnGo.GetComponent<Button>();

            ghostEyeballRoot.SetActive(false);

            // =================================================================
            // MODE D: GHOST SCREAM TEXT FLOOD (Screenshot 4: DIE / HAHAHA! / HELP ME / ??? / EVIL!)
            // =================================================================
            var ghostScreamRoot = CreateUI("GhostScreamRoot", viewport.transform);
            Stretch(ghostScreamRoot.GetComponent<RectTransform>(), 0f);

            string[] screamPhrases = { "DIE", "HAHAHA!", "HELP ME", "???", "EVIL!", "DIE DIE", "HELP!", "??????" };
            var rng = new System.Random(666);

            // 26 Background Scream Words flooding across the outer screen
            for (int i = 0; i < 26; i++)
            {
                string word = screamPhrases[i % screamPhrases.Length];
                float wx = (i < 13) ? Mathf.Lerp(-740f, -310f, (float)rng.NextDouble()) : Mathf.Lerp(310f, 740f, (float)rng.NextDouble());
                float wy = Mathf.Lerp(-380f, 360f, (float)rng.NextDouble());
                float fSize = Mathf.Lerp(34f, 84f, (float)rng.NextDouble());
                bool isWhite = (i % 5 == 0);
                Color col = isWhite
                    ? new Color(0.95f, 0.92f, 0.92f, 0.55f)
                    : new Color(0.88f, 0.08f, 0.08f, Mathf.Lerp(0.35f, 0.78f, (float)rng.NextDouble()));

                var wGo = CreateUI($"BgScreamWord_{i:00}", ghostScreamRoot.transform);
                var wrt = wGo.GetComponent<RectTransform>();
                SetCentered(wrt, wx, wy, 420f, 110f);
                var wTmp = CreateText(wGo, fontTitle, word, fSize, col, TextAlignmentOptions.Center);
                wTmp.fontStyle = FontStyles.Bold;
                screamTextList.Add(wrt);
            }

            // 22 Intense Foreground Scream Words inside the Phone Screen
            var phoneScreamContainer = CreateUI("PhoneScreen_ScreamContainer", ghostScreamRoot.transform);
            SetCentered(phoneScreamContainer.GetComponent<RectTransform>(), 0f, -8f, 488f, 668f);
            phoneScreamContainer.AddComponent<RectMask2D>();

            for (int i = 0; i < 22; i++)
            {
                string word = screamPhrases[(i * 3) % screamPhrases.Length];
                float wx = Mathf.Lerp(-185f, 185f, (float)rng.NextDouble());
                float wy = Mathf.Lerp(-260f, 270f, (float)rng.NextDouble());
                float fSize = Mathf.Lerp(28f, 58f, (float)rng.NextDouble());
                bool brightWhite = (i % 2 == 0);
                Color col = brightWhite
                    ? new Color(1f, 0.96f, 0.96f, 0.96f)
                    : new Color(0.98f, 0.12f, 0.12f, 0.92f);

                var wGo = CreateUI($"PhoneScreamWord_{i:00}", phoneScreamContainer.transform);
                var wrt = wGo.GetComponent<RectTransform>();
                SetCentered(wrt, wx, wy, 380f, 80f);
                var wTmp = CreateText(wGo, fontType, word, fSize, col, TextAlignmentOptions.Center);
                wTmp.fontStyle = FontStyles.Bold;
                screamTextList.Add(wrt);
            }

            // Scream Banner & Replay Jumpscare Button inside Phone Screen (Screenshot 4)
            var screamBanner = CreateUI("ScreamAlertBanner", phoneScreamContainer.transform);
            SetCentered(screamBanner.GetComponent<RectTransform>(), 0f, -75f, 420f, 48f);
            AddBorderedBox(screamBanner, new Color(0.04f, 0.01f, 0.01f, 0.88f), new Color(0.95f, 0.20f, 0.20f, 1f), 2f);
            var sbTmp = CreateText(CreateUI("Txt", screamBanner.transform), fontThai, "[!] \"กรี๊ดดดดดด!!!\"", 24f, Color.white, TextAlignmentOptions.Center);
            sbTmp.fontStyle = FontStyles.Bold;
            Stretch(sbTmp.GetComponent<RectTransform>(), 0f);

            var cdScreamGo = CreateUI("ScreamCountdown", phoneScreamContainer.transform);
            SetBottomLeft(cdScreamGo.GetComponent<RectTransform>(), 24f, 34f, 200f, 54f);
            var cdScreamTmp = CreateText(cdScreamGo, fontThai, "[!] ใน: 3.5s", 22f, new Color(1f, 0.85f, 0.85f, 1f), TextAlignmentOptions.MidlineLeft);
            cdScreamTmp.fontStyle = FontStyles.Bold;

            var replayScreamBtnGo = CreateTerminalButton(
                phoneScreamContainer.transform, "Btn_ReplayScreamJumpscare", fontThai,
                "เล่นซ้ำ\nJumpscare", 20f,
                new Color(0.58f, 0.10f, 0.10f, 0.95f), new Color(0.95f, 0.25f, 0.25f, 1f), Color.white);
            SetBottomRight(replayScreamBtnGo.GetComponent<RectTransform>(), -20f, 24f, 210f, 74f);
            var replayScreamBtn = replayScreamBtnGo.GetComponent<Button>();

            ghostScreamRoot.SetActive(false);

            // Global Scanline Overlay across Phone Viewport
            var phoneScanlines = CreateUI("PhoneViewport_Scanlines", viewport.transform);
            Stretch(phoneScanlines.GetComponent<RectTransform>(), 0f);
            var pscImg = phoneScanlines.AddComponent<Image>();
            pscImg.sprite = sprScanline;
            pscImg.type = Image.Type.Tiled;
            pscImg.color = new Color(0f, 0f, 0f, 0.34f);
            pscImg.raycastTarget = false;

            // Bottom-Left Status Pill (Matches Screenshots 2, 3, 4)
            var statusPill = CreateUI("BottomLeft_StatusBadge", viewport.transform);
            SetBottomLeft(statusPill.GetComponent<RectTransform>(), 28f, 18f, 440f, 46f);
            AddBorderedBox(statusPill, new Color(0.02f, 0.05f, 0.08f, 0.92f), new Color(0.18f, 0.56f, 0.68f, 1f), 2f, out _, out var statusBorderImg);
            var statusTxtGo = CreateUI("StatusText", statusPill.transform);
            Stretch(statusTxtGo.GetComponent<RectTransform>(), 12f);
            var statusTmp = CreateText(statusTxtGo, fontThai, "► กดปุ่มสีเขียวเพื่อรับสาย", 20f, new Color(0.45f, 0.92f, 0.98f, 1f), TextAlignmentOptions.Center);
            statusTmp.fontStyle = FontStyles.Bold;

            // Bottom-Center Exit Phone Button: "▼ วางโทรศัพท์ลง / กลับโต๊ะ ▼" (Matches Screenshots 2, 3, 4)
            var exitPhoneGo = CreateTerminalButton(
                viewport.transform, "Btn_ExitPhone", fontThai,
                "▼  วางโทรศัพท์ลง  /  กลับโต๊ะ  ▼", 22f,
                new Color(0.22f, 0.25f, 0.26f, 0.88f), new Color(0.68f, 0.72f, 0.74f, 0.9f), Color.white);
            SetBottomCenter(exitPhoneGo.GetComponent<RectTransform>(), 0f, 18f, 560f, 48f);

            // Wire PhoneUIController
            SetField(ctrl, "phoneMainPanel", mainPanel);
            SetField(ctrl, "ringingPanel", ringingPanel);
            SetField(ctrl, "inCallPanel", inCallPanel);
            SetField(ctrl, "decisionPanel", decisionPanel);

            SetField(ctrl, "normalBackdropLayer", normalBackdrop);
            SetField(ctrl, "horrorRedBackdropLayer", horrorBackdrop);
            SetField(ctrl, "ghostEyeballRoot", ghostEyeballRoot);
            SetField(ctrl, "ghostScreamRoot", ghostScreamRoot);
            SetField(ctrl, "phoneScreenBgImage", screenBgImg);
            SetField(ctrl, "phoneFrameBezelImage", bezelBorderImg);

            SetField(ctrl, "callerNameText", nameTmp);
            SetField(ctrl, "callerNumberText", numTmp);
            SetField(ctrl, "inCallCallerNameText", icNameTmp);
            SetField(ctrl, "inCallCallerNumberText", icNumTmp);
            SetField(ctrl, "dialogueText", dlgTmp);
            SetField(ctrl, "carrierHeaderText", carrierTmp);
            SetField(ctrl, "bottomStatusBadgeText", statusTmp);
            SetField(ctrl, "bottomStatusBadgeBorder", statusBorderImg);
            SetField(ctrl, "ghostEyeballCountdownText", cdEyeTmp);
            SetField(ctrl, "ghostScreamCountdownText", cdScreamTmp);

            SetField(ctrl, "answerButton", ansBtn);
            SetField(ctrl, "hangupRingingButton", hangBtn);
            SetField(ctrl, "nextDialogueButton", nextDlgBtn);
            SetField(ctrl, "acceptOfferButton", accOfferBtn);
            SetField(ctrl, "declineOfferButton", decOfferBtn);
            SetField(ctrl, "exitPhoneButton", exitPhoneGo.GetComponent<Button>());

            SetField(ctrl, "modeIncomingButton", btnIncomingGo.GetComponent<Button>());
            SetField(ctrl, "modeFakeCaseButton", btnFakeGo.GetComponent<Button>());
            SetField(ctrl, "modeRealCaseButton", btnRealGo.GetComponent<Button>());
            SetField(ctrl, "modeGhostEyeballButton", btnEyeballGo.GetComponent<Button>());
            SetField(ctrl, "modeGhostScreamButton", btnScreamGo.GetComponent<Button>());
            SetField(ctrl, "replayEyeballJumpscareButton", replayEyeBtn);
            SetField(ctrl, "replayScreamJumpscareButton", replayScreamBtn);

            SetList(ctrl, "eyeballPupils", pupilList);
            SetList(ctrl, "screamFloodTexts", screamTextList);
            SetField(ctrl, "phoneFrameShakeRect", phoneFrameRt);

            mainPanel.SetActive(false);
        }

        private static RectTransform CreateHorrorAlmondEye(
            Transform parent,
            string name,
            Sprite sprAlmond,
            Sprite sprPupil,
            Vector2 anchoredPos,
            Vector2 eyeSize,
            float pupilDiameter)
        {
            var eyeGo = CreateUI(name, parent);
            SetCentered(eyeGo.GetComponent<RectTransform>(), anchoredPos.x, anchoredPos.y, eyeSize.x, eyeSize.y);
            var eyeImg = eyeGo.AddComponent<Image>();
            eyeImg.sprite = sprAlmond;
            eyeImg.color = Color.white;
            eyeImg.raycastTarget = false;

            var pupilGo = CreateUI("Pupil", eyeGo.transform);
            var prt = pupilGo.GetComponent<RectTransform>();
            SetCentered(prt, 0f, 0f, pupilDiameter, pupilDiameter);
            var pImg = pupilGo.AddComponent<Image>();
            pImg.sprite = sprPupil;
            pImg.color = Color.white;
            pImg.raycastTarget = false;
            return prt;
        }

        // =====================================================================
        // 3. EMBED MAIN MENU OVERLAY INTO test.unity
        // =====================================================================
        private static void BuildEmbeddedMainMenuInTestScene(
            TMP_FontAsset fontTitle,
            TMP_FontAsset fontType,
            TMP_FontAsset fontThai,
            Sprite sprScanline)
        {
            // First ensure Main menu.unity is up to date, then copy its 4 roots into test.unity
            string[] menuRoots = { "_MenuSystems", "Canvas_Background", "Canvas_UI", "Canvas_FX" };
            foreach (var rName in menuRoots)
            {
                var existing = GameObject.Find(rName);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            Scene testScene = SceneManager.GetActiveScene();
            Scene menuScene = EditorSceneManager.OpenScene("Assets/Cee/Main menu.unity", OpenSceneMode.Additive);

            var copiedRoots = new List<GameObject>();
            foreach (var rootObj in menuScene.GetRootGameObjects())
            {
                foreach (var targetName in menuRoots)
                {
                    if (rootObj.name == targetName)
                    {
                        // Move the ORIGINAL objects (not Instantiate per-root) so that all cross-root
                        // serialized references (e.g. MainMenuController -> Canvas_UI panels) stay intact.
                        // The additive Main menu scene is closed afterwards WITHOUT saving, so the asset on disk is untouched.
                        SceneManager.MoveGameObjectToScene(rootObj, testScene);
                        copiedRoots.Add(rootObj);
                    }
                }
            }

            EditorSceneManager.CloseScene(menuScene, true);

            // Elevate sorting order of Main Menu canvases so they sit cleanly above GameUI_Canvas (sortingOrder 10)
            var overlayCanvases = new List<GameObject>();
            GameObject sysGo = null;
            foreach (var go in copiedRoots)
            {
                if (go.name == "_MenuSystems") sysGo = go;
                var c = go.GetComponent<Canvas>();
                if (c != null)
                {
                    if (go.name == "Canvas_Background") c.sortingOrder = 50;
                    else if (go.name == "Canvas_UI") c.sortingOrder = 60;
                    else if (go.name == "Canvas_FX") c.sortingOrder = 100;
                    overlayCanvases.Add(go);
                }
            }

            if (sysGo != null)
            {
                var mainCtrl = sysGo.GetComponent<MainMenuController>();
                if (mainCtrl != null)
                {
                    SetField(mainCtrl, "inSceneOverlayMode", true);
                    SetField(mainCtrl, "gameSceneName", "test");
                    SetList(mainCtrl, "overlayCanvasObjects", overlayCanvases);
                }
            }
        }

        // =====================================================================
        // UI HELPER UTILITIES
        // =====================================================================
        private static Transform FindChildTransform(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            return null;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(t.GetChild(i).gameObject);
            }
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetCentered(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetTopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetTopCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetBottomCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetBottomLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetBottomRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void AddBorderedBox(GameObject go, Color bgColor, Color borderColor, float borderWidth)
        {
            AddBorderedBox(go, bgColor, borderColor, borderWidth, out _, out _);
        }

        private static void AddBorderedBox(GameObject go, Color bgColor, Color borderColor, float borderWidth, out Image innerBgImg, out Image outerBorderImg)
        {
            outerBorderImg = go.GetComponent<Image>();
            if (outerBorderImg == null) outerBorderImg = go.AddComponent<Image>();
            outerBorderImg.color = borderColor;

            var innerGo = CreateUI("InnerBg", go.transform);
            Stretch(innerGo.GetComponent<RectTransform>(), borderWidth);
            innerBgImg = innerGo.AddComponent<Image>();
            innerBgImg.color = bgColor;
            innerBgImg.raycastTarget = false;
        }

        private static GameObject CreateTerminalButton(
            Transform parent,
            string name,
            TMP_FontAsset font,
            string label,
            float fontSize,
            Color bgCol,
            Color borderCol,
            Color textCol)
        {
            var go = CreateUI(name, parent);
            AddBorderedBox(go, bgCol, borderCol, 2f, out var bgImg, out var borderImg);
            bgImg.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            var txtGo = CreateUI("Label", go.transform);
            Stretch(txtGo.GetComponent<RectTransform>(), 4f);
            var tmp = CreateText(txtGo, font, label, fontSize, textCol, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            return go;
        }

        private static TextMeshProUGUI CreateText(GameObject go, TMP_FontAsset font, string text, float size, Color col, TextAlignmentOptions align)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = col;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetField(Object target, string fieldName, Object val)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(fieldName);
            if (p != null)
            {
                p.objectReferenceValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, string val)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(fieldName);
            if (p != null)
            {
                p.stringValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, bool val)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(fieldName);
            if (p != null)
            {
                p.boolValue = val;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetList<T>(Object target, string fieldName, List<T> list) where T : class
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var f = target.GetType().GetField(fieldName, flags);
            if (f != null)
            {
                f.SetValue(target, list);
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
