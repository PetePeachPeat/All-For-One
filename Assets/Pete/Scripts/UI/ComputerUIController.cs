using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Pete.Data;
using Pete.Systems;
using Pete.Interaction;

namespace Pete.UI
{
    [Serializable]
    public class CRTMessageCardRefs
    {
        public GameObject cardRoot;
        public Image cardBorderImage;
        public Image cardBgImage;
        public TextMeshProUGUI idText;
        public Image badgeBgImage;
        public TextMeshProUGUI badgeText;
        public TextMeshProUGUI fromText;
        public TextMeshProUGUI metaText;
        public TextMeshProUGUI bodyText;
        public Button openWebButton;
        public Button approveButton;
        public Button declineButton;
    }

    public class FolderHoverZoomFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform iconContainer;
        public Image cardBorder;
        public Image cardBackground;
        public TextMeshProUGUI folderLabel;

        private bool isHovered;
        private Vector3 baseScale = Vector3.one;
        private Vector2 baseAnchoredPos;
        private bool initialized;

        private void Awake()
        {
            InitIfNeeded();
        }

        private void InitIfNeeded()
        {
            if (initialized) return;
            initialized = true;
            if (iconContainer != null)
            {
                baseScale = iconContainer.localScale;
                baseAnchoredPos = iconContainer.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            InitIfNeeded();
            isHovered = false;
            if (iconContainer != null)
            {
                iconContainer.localScale = baseScale;
                iconContainer.anchoredPosition = baseAnchoredPos;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (cardBorder != null) cardBorder.color = new Color(0.53f, 0.94f, 0.67f, 1f);
            if (cardBackground != null) cardBackground.color = new Color(0.03f, 0.18f, 0.09f, 0.92f);
            if (folderLabel != null) folderLabel.color = Color.white;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (cardBorder != null) cardBorder.color = new Color(0.13f, 0.77f, 0.37f, 0.85f);
            if (cardBackground != null) cardBackground.color = new Color(0.015f, 0.10f, 0.05f, 0.82f);
            if (folderLabel != null) folderLabel.color = new Color(0.29f, 0.87f, 0.50f, 1f);
        }

        private void Update()
        {
            if (iconContainer == null) return;
            Vector3 targetScale = isHovered ? baseScale * 1.34f : baseScale;
            Vector2 targetPos = isHovered ? baseAnchoredPos + new Vector2(0f, 8f) : baseAnchoredPos;
            iconContainer.localScale = Vector3.Lerp(iconContainer.localScale, targetScale, Time.unscaledDeltaTime * 14f);
            iconContainer.anchoredPosition = Vector2.Lerp(iconContainer.anchoredPosition, targetPos, Time.unscaledDeltaTime * 14f);
        }
    }

    public class ComputerUIController : MonoBehaviour
    {
        [Header("Desktop Root Panel")]
        [SerializeField] private GameObject desktopRootPanel;
        [SerializeField] private GameObject desktopFoldersView;

        [Header("Desktop HUD")]
        [SerializeField] private TextMeshProUGUI timeDisplay12HText;
        [SerializeField] private TextMeshProUGUI nightDisplayText;
        [SerializeField] private Button exitComputerButton;
        [SerializeField] private Button replayDesktopAnimButton;
        [SerializeField] private Button crtPowerButton;
        [SerializeField] private GameObject crtPowerOffOverlay;
        [SerializeField] private RectTransform scanBeamRect;

        [Header("Desktop App Icons")]
        [SerializeField] private Button gmailIconButton;
        [SerializeField] private GameObject gmailBadgeObject;
        [SerializeField] private TextMeshProUGUI gmailUnreadCountText;

        [SerializeField] private Button websiteIconButton;
        [SerializeField] private GameObject websiteBadgeObject;
        [SerializeField] private TextMeshProUGUI websiteAlertCountText;

        [SerializeField] private Button rulesIconButton;

        [Header("GMAIL Window (3-Column Terminal View)")]
        [SerializeField] private GameObject gmailWindow;
        [SerializeField] private Button gmailCloseButton;
        [SerializeField] private Button gmailReplayAnimButton;
        [SerializeField] private Transform emailListContainer;
        [SerializeField] private GameObject emailListItemPrefab;
        [SerializeField] private List<CRTMessageCardRefs> gmailCards = new List<CRTMessageCardRefs>();

        [Header("GMAIL - Selected Email View (Legacy Compatibility)")]
        [SerializeField] private TextMeshProUGUI emailSenderText;
        [SerializeField] private TextMeshProUGUI emailSubjectText;
        [SerializeField] private TextMeshProUGUI emailBodyText;
        [SerializeField] private GameObject openWebsiteLinkButton;
        [SerializeField] private Button emailAcceptButton;
        [SerializeField] private Button emailDeclineButton;

        [Header("WEBSITE List Window (3-Column Link Selector View)")]
        [SerializeField] private GameObject websiteListWindow;
        [SerializeField] private Button websiteListCloseButton;
        [SerializeField] private Button websiteListReplayAnimButton;
        [SerializeField] private List<CRTMessageCardRefs> websiteCards = new List<CRTMessageCardRefs>();

        [Header("WEBSITE Browser Preview Window")]
        [SerializeField] private GameObject websiteWindow;
        [SerializeField] private Button websiteCloseButton;
        [SerializeField] private Button websiteBackToListButton;
        [SerializeField] private TextMeshProUGUI websiteUrlText;
        [SerializeField] private TextMeshProUGUI websiteSenderNoteText;
        [SerializeField] private TextMeshProUGUI websiteSecurityBadgeText;
        [SerializeField] private Image websiteSecurityBadgeBg;
        [SerializeField] private TextMeshProUGUI websiteTitleText;
        [SerializeField] private TextMeshProUGUI websiteDescText;
        [SerializeField] private List<TextMeshProUGUI> websiteFormFieldTexts = new List<TextMeshProUGUI>();
        [SerializeField] private List<Button> websiteTabButtons = new List<Button>();
        [SerializeField] private Image websitePreviewImage;
        [SerializeField] private Button websiteAcceptButton;
        [SerializeField] private Button websiteDeclineButton;

        [Header("RULES Window")]
        [SerializeField] private GameObject rulesWindow;
        [SerializeField] private Button rulesCloseButton;
        [SerializeField] private Button rulesAcknowledgeButton;
        [SerializeField] private TextMeshProUGUI rulesContentText;

        [Header("Breach & Shatter FX")]
        [SerializeField] private RectTransform monitorFrameRect;
        [SerializeField] private CanvasGroup breachRedOverlay;
        [SerializeField] private CanvasGroup breachCrackOverlay;

        // Runtime states
        private EmailDataSO currentlySelectedEmail;
        private EmailDataSO currentlySelectedWebsiteEmail;
        private int currentWebBrowserIdx = 0;
        private bool isCrtPoweredOn = true;

        private struct TerminalMailItem
        {
            public string idLabel;
            public string badgeDefault;
            public string fromAddress;
            public string metaLabel;
            public string body;
            public bool isScam;
            public bool judged;
            public bool judgedCorrect;
            public string judgedStatus;
        }

        private struct TerminalWebItem
        {
            public string idLabel;
            public string tabLabel;
            public string badgeDefault;
            public string fromAddress;
            public string shortUrl;
            public string fullUrl;
            public string senderNote;
            public string cardBody;
            public bool isSecureHttps;
            public bool isScam;
            public string webTitle;
            public string webDesc;
            public string[] webFields;
            public bool judged;
            public bool judgedCorrect;
            public string judgedStatus;
        }

        private TerminalMailItem[] gmailData;
        private TerminalWebItem[] webData;

        private void Awake()
        {
            InitTerminalData();
        }

        private void InitTerminalData()
        {
            gmailData = new TerminalMailItem[]
            {
                new TerminalMailItem
                {
                    idLabel = "ข้อความ 01",
                    badgeDefault = "งานออนไลน์",
                    fromAddress = "vip-job@fast-money.net",
                    metaLabel = "โอนค่ามัดจำ 1,500฿",
                    body = "รับคนช่วยคอนเฟิร์มออเดอร์ที่บ้าน รายได้วันละ 5,000฿ แต่ต้องโอนค่าเปิดรหัส VIP ก่อน 1,500฿",
                    isScam = true
                },
                new TerminalMailItem
                {
                    idLabel = "ข้อความ 02",
                    badgeDefault = "นัดสัมภาษณ์",
                    fromAddress = "hr@sec-corp.co.th",
                    metaLabel = "Contract_2026.pdf",
                    body = "บริษัท SEC-CORP ตอบรับท่านเข้าทำงานเจ้าหน้าที่กะดึก เงินเดือน 28,000฿ ไม่มีค่าสมัครใดๆ",
                    isScam = false
                },
                new TerminalMailItem
                {
                    idLabel = "ข้อความ 03",
                    badgeDefault = "ด่วนที่สุด",
                    fromAddress = "admin@dark-payroll.xyz",
                    metaLabel = "Salary_Slip.pdf.exe",
                    body = "จ้างเปิดบัญชีรับโอนเงินเดือนละ 30,000฿ ส่งสมุดบัญชีและรหัส ATM มาให้ทางบริษัททันที",
                    isScam = true
                }
            };

            webData = new TerminalWebItem[]
            {
                new TerminalWebItem
                {
                    idLabel = "ข้อความเว็บ 01",
                    tabLabel = "เว็บไซต์ 01",
                    badgeDefault = "คืนค่าไฟ",
                    fromAddress = "claim@mea-refund.net",
                    shortUrl = "http://mea-free-refund.net",
                    fullUrl = "http://mea-free-refund.net/claim-pin",
                    senderNote = "งานรับเงินคืนค่าไฟด่วน (แนบลิงก์)",
                    cardBody = "สิทธิ์รับเงินคืนค่าไฟ 4,500฿ กดลิงก์แนบเพื่อกรอกเลขบัญชีและรหัส PIN รับเงินทันที",
                    isSecureHttps = false,
                    isScam = true,
                    webTitle = "[!] ระบบรับเงินคืนค่าไฟด่วน (MEA REFUND)",
                    webDesc = "กรุณากรอกข้อมูลเพื่อรับเงินคืน 4,500 บาท เข้าบัญชีทันที:",
                    webFields = new[] { "เบอร์โทรศัพท์", "เลขบัญชีธนาคาร", "รหัส PIN ATM 6 หลัก (บังคับกรอก)" }
                },
                new TerminalWebItem
                {
                    idLabel = "ข้อความเว็บ 02",
                    tabLabel = "เว็บไซต์ 02",
                    badgeDefault = "งานบริษัท",
                    fromAddress = "hr@sec-corp.co.th",
                    shortUrl = "https://sec-corp.co.th",
                    fullUrl = "https://www.sec-corp.co.th/onboarding",
                    senderNote = "ลงทะเบียนพนักงานใหม่ SEC-CORP (แนบลิงก์)",
                    cardBody = "เว็บลงทะเบียนพนักงานใหม่ SEC-CORP กดลิงก์เพื่อกรอกชื่อและเลือกกะทำงาน (ไม่มีค่าสมัคร)",
                    isSecureHttps = true,
                    isScam = false,
                    webTitle = "[HTTPS] ระบบยืนยันตัวตนพนักงานใหม่ บจก. SEC-CORP",
                    webDesc = "กรอกชื่อและเลือกกะเวลาทำงาน (เงินเดือน 28,000฿ ไม่มีค่าธรรมเนียมใดๆ):",
                    webFields = new[] { "ชื่อ-นามสกุลพนักงาน", "เบอร์โทรศัพท์ติดต่อ", "กะเวลาทำงาน (Night Shift 00:00 - 06:00)" }
                },
                new TerminalWebItem
                {
                    idLabel = "ข้อความเว็บ 03",
                    tabLabel = "เว็บไซต์ 03",
                    badgeDefault = "หุ้น AI",
                    fromAddress = "vip@ai-trade888.xyz",
                    shortUrl = "http://rich-ai-trade888.xyz",
                    fullUrl = "http://rich-ai-trade888.xyz/deposit",
                    senderNote = "เว็บลงทุนหุ้น AI ปันผล 300% (แนบลิงก์)",
                    cardBody = "เว็บเทรดหุ้น AI กำไร 300% กดลิงก์เพื่อฝากเงิน 1,000฿ และกรอกรหัสผ่านแอปธนาคาร",
                    isSecureHttps = false,
                    isScam = true,
                    webTitle = "[!] AI SUPER TRADE // ฝาก 1,000฿ ถอนได้ 10,000฿",
                    webDesc = "โอนเงินเปิดพอร์ตขั้นต่ำ 1,000 บาท พร้อมกรอกรหัสผ่าน Mobile Banking:",
                    webFields = new[] { "ไอดีผู้ใช้งาน", "รหัสผ่านแอปธนาคาร 6 หลัก", "จำนวนเงินโอนเปิดพอร์ต (ขั้นต่ำ 1,000฿)" }
                }
            };
        }

        private void Start()
        {
            if (exitComputerButton != null)
                exitComputerButton.onClick.AddListener(ExitComputer);
            if (replayDesktopAnimButton != null)
                replayDesktopAnimButton.onClick.AddListener(PlayScanBeamSweep);
            if (crtPowerButton != null)
                crtPowerButton.onClick.AddListener(ToggleCrtPower);

            if (gmailIconButton != null)
                gmailIconButton.onClick.AddListener(OpenGmailWindow);
            if (gmailCloseButton != null)
                gmailCloseButton.onClick.AddListener(ShowDesktopFolders);
            if (gmailReplayAnimButton != null)
                gmailReplayAnimButton.onClick.AddListener(PlayScanBeamSweep);

            if (websiteIconButton != null)
                websiteIconButton.onClick.AddListener(OpenWebsiteWindow);
            if (websiteListCloseButton != null)
                websiteListCloseButton.onClick.AddListener(ShowDesktopFolders);
            if (websiteListReplayAnimButton != null)
                websiteListReplayAnimButton.onClick.AddListener(PlayScanBeamSweep);

            if (websiteCloseButton != null)
                websiteCloseButton.onClick.AddListener(ShowDesktopFolders);
            if (websiteBackToListButton != null)
                websiteBackToListButton.onClick.AddListener(OpenWebsiteWindow);

            if (rulesIconButton != null)
                rulesIconButton.onClick.AddListener(OpenRulesWindow);
            if (rulesCloseButton != null)
                rulesCloseButton.onClick.AddListener(ShowDesktopFolders);
            if (rulesAcknowledgeButton != null)
                rulesAcknowledgeButton.onClick.AddListener(ShowDesktopFolders);

            if (emailAcceptButton != null)
                emailAcceptButton.onClick.AddListener(OnAcceptCurrentEmail);
            if (emailDeclineButton != null)
                emailDeclineButton.onClick.AddListener(OnDeclineCurrentEmail);

            if (websiteAcceptButton != null)
                websiteAcceptButton.onClick.AddListener(() => JudgeWebsiteCard(currentWebBrowserIdx, true));
            if (websiteDeclineButton != null)
                websiteDeclineButton.onClick.AddListener(() => JudgeWebsiteCard(currentWebBrowserIdx, false));

            if (openWebsiteLinkButton != null)
            {
                var btn = openWebsiteLinkButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnOpenWebsiteFromEmail);
            }

            // Wire 3 Gmail Cards
            for (int i = 0; i < gmailCards.Count; i++)
            {
                int idx = i;
                var card = gmailCards[i];
                if (card.approveButton != null)
                    card.approveButton.onClick.AddListener(() => JudgeGmailCard(idx, true));
                if (card.declineButton != null)
                    card.declineButton.onClick.AddListener(() => JudgeGmailCard(idx, false));
            }

            // Wire 3 Website List Cards
            for (int i = 0; i < websiteCards.Count; i++)
            {
                int idx = i;
                var card = websiteCards[i];
                if (card.openWebButton != null)
                    card.openWebButton.onClick.AddListener(() => OpenWebsiteBrowserDetail(idx));
                if (card.approveButton != null)
                    card.approveButton.onClick.AddListener(() => JudgeWebsiteCard(idx, true));
                if (card.declineButton != null)
                    card.declineButton.onClick.AddListener(() => JudgeWebsiteCard(idx, false));
            }

            // Wire Website Browser Tabs
            for (int i = 0; i < websiteTabButtons.Count; i++)
            {
                int idx = i;
                if (websiteTabButtons[i] != null)
                    websiteTabButtons[i].onClick.AddListener(() => RenderWebsiteBrowserDetail(idx));
            }

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged += UpdateTimeDisplay;
                UpdateTimeDisplay(GameTimeManager.Instance.CurrentHour, GameTimeManager.Instance.CurrentMinute);
            }

            if (EmailManager.Instance != null)
            {
                EmailManager.Instance.OnEmailReceived += OnNewEmailReceived;
                EmailManager.Instance.OnEmailStatusUpdated += RefreshBadgesAndList;
            }

            RefreshAllCardsUI();
            ShowDesktopFolders();

            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted += HandleInspectStarted;
                CameraController.Instance.OnInspectEnded += HandleInspectEnded;
            }

            if (desktopRootPanel != null) desktopRootPanel.SetActive(false);
        }

        private void HandleInspectStarted(InspectTargetType type)
        {
            if (desktopRootPanel != null)
            {
                bool active = (type == InspectTargetType.Computer);
                desktopRootPanel.SetActive(active);
                if (active)
                {
                    ShowDesktopFolders();
                }
            }
        }

        private void HandleInspectEnded()
        {
            if (desktopRootPanel != null)
            {
                desktopRootPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted -= HandleInspectStarted;
                CameraController.Instance.OnInspectEnded -= HandleInspectEnded;
            }
            if (GameTimeManager.Instance != null)
                GameTimeManager.Instance.OnTimeChanged -= UpdateTimeDisplay;

            if (EmailManager.Instance != null)
            {
                EmailManager.Instance.OnEmailReceived -= OnNewEmailReceived;
                EmailManager.Instance.OnEmailStatusUpdated -= RefreshBadgesAndList;
            }
        }

        public void ExitComputer()
        {
            ShowDesktopFolders();
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitInspect();
            }
        }

        private void ToggleCrtPower()
        {
            isCrtPoweredOn = !isCrtPoweredOn;
            if (crtPowerOffOverlay != null)
                crtPowerOffOverlay.SetActive(!isCrtPoweredOn);
            if (isCrtPoweredOn)
                PlayScanBeamSweep();
        }

        private void PlayScanBeamSweep()
        {
            if (scanBeamRect != null)
            {
                if (!Application.isPlaying)
                {
                    scanBeamRect.gameObject.SetActive(false);
                    return;
                }
                if (gameObject.activeInHierarchy)
                {
                    StopCoroutine(nameof(ScanBeamRoutine));
                    StartCoroutine(nameof(ScanBeamRoutine));
                }
            }
        }

        private IEnumerator ScanBeamRoutine()
        {
            scanBeamRect.gameObject.SetActive(true);
            float dur = 0.52f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                scanBeamRect.anchorMin = new Vector2(0f, 1f - p);
                scanBeamRect.anchorMax = new Vector2(1f, 1f - p);
                scanBeamRect.anchoredPosition = Vector2.zero;
                yield return null;
            }
            scanBeamRect.gameObject.SetActive(false);
        }

        private void UpdateTimeDisplay(int hour, int minute)
        {
            if (timeDisplay12HText != null && GameTimeManager.Instance != null)
            {
                timeDisplay12HText.text = GameTimeManager.Instance.GetDisplayHour12H();
            }
            if (nightDisplayText != null)
            {
                nightDisplayText.text = "NIGHT 1";
            }
        }

        private void OnNewEmailReceived(EmailDataSO email)
        {
            RefreshBadgesAndList();
        }

        public void RefreshBadgesAndList()
        {
            RefreshAllCardsUI();
        }

        private void RefreshAllCardsUI()
        {
            if (gmailData == null || webData == null) InitTerminalData();

            int gmailRemaining = 0;
            for (int i = 0; i < gmailData.Length; i++)
            {
                if (!gmailData[i].judged) gmailRemaining++;
                if (i < gmailCards.Count)
                {
                    BindGmailCardUI(gmailCards[i], gmailData[i]);
                }
            }

            int webRemaining = 0;
            for (int i = 0; i < webData.Length; i++)
            {
                if (!webData[i].judged) webRemaining++;
                if (i < websiteCards.Count)
                {
                    BindWebCardUI(websiteCards[i], webData[i]);
                }
            }

            if (gmailBadgeObject != null) gmailBadgeObject.SetActive(gmailRemaining > 0);
            if (gmailUnreadCountText != null) gmailUnreadCountText.text = gmailRemaining.ToString();

            if (websiteBadgeObject != null) websiteBadgeObject.SetActive(webRemaining > 0);
            if (websiteAlertCountText != null) websiteAlertCountText.text = webRemaining.ToString();
        }

        private void BindGmailCardUI(CRTMessageCardRefs ui, TerminalMailItem data)
        {
            if (ui == null) return;
            if (ui.idText != null) ui.idText.text = data.idLabel;
            if (ui.fromText != null) ui.fromText.text = $"<color=#22c55e>จาก:</color> {data.fromAddress}";
            if (ui.metaText != null) ui.metaText.text = $"<color=#22c55e>ข้อมูล:</color> {data.metaLabel}";
            if (ui.bodyText != null) ui.bodyText.text = data.body;

            if (ui.badgeText != null)
            {
                ui.badgeText.text = data.judged ? data.judgedStatus : data.badgeDefault;
                ui.badgeText.color = data.judged
                    ? (data.judgedCorrect ? new Color(0.01f, 0.04f, 0.02f, 1f) : Color.white)
                    : new Color(0.29f, 0.87f, 0.50f, 1f);
            }
            if (ui.badgeBgImage != null)
            {
                ui.badgeBgImage.color = data.judged
                    ? (data.judgedCorrect ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.94f, 0.27f, 0.27f, 1f))
                    : new Color(0.08f, 0.35f, 0.18f, 1f);
            }
            if (ui.approveButton != null) ui.approveButton.interactable = !data.judged;
            if (ui.declineButton != null) ui.declineButton.interactable = !data.judged;
        }

        private void BindWebCardUI(CRTMessageCardRefs ui, TerminalWebItem data)
        {
            if (ui == null) return;
            if (ui.idText != null) ui.idText.text = data.idLabel;
            if (ui.fromText != null) ui.fromText.text = $"<color=#22c55e>จาก:</color> {data.fromAddress}";
            if (ui.metaText != null) ui.metaText.text = $"<color=#22c55e>ลิงก์:</color> <color=#ffffff>{data.shortUrl}</color>";
            if (ui.bodyText != null) ui.bodyText.text = data.cardBody;

            if (ui.badgeText != null)
            {
                ui.badgeText.text = data.judged ? data.judgedStatus : data.badgeDefault;
                ui.badgeText.color = data.judged
                    ? (data.judgedCorrect ? new Color(0.01f, 0.04f, 0.02f, 1f) : Color.white)
                    : new Color(0.29f, 0.87f, 0.50f, 1f);
            }
            if (ui.badgeBgImage != null)
            {
                ui.badgeBgImage.color = data.judged
                    ? (data.judgedCorrect ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.94f, 0.27f, 0.27f, 1f))
                    : new Color(0.08f, 0.35f, 0.18f, 1f);
            }
            if (ui.approveButton != null) ui.approveButton.interactable = !data.judged;
            if (ui.declineButton != null) ui.declineButton.interactable = !data.judged;
        }

        private void JudgeGmailCard(int idx, bool approve)
        {
            if (idx < 0 || idx >= gmailData.Length || gmailData[idx].judged) return;

            bool isScam = gmailData[idx].isScam;
            bool isCorrect = (isScam && !approve) || (!isScam && approve);

            gmailData[idx].judged = true;
            gmailData[idx].judgedCorrect = isCorrect;
            gmailData[idx].judgedStatus = isCorrect ? (approve ? "[+] APPROVED" : "[X] DECLINED") : "[!] พลาด!";

            if (ScoreManager.Instance != null)
            {
                if (approve) ScoreManager.Instance.RecordJobAccepted(isScam);
                else ScoreManager.Instance.RecordJobDeclined(isScam);
            }

            if (!isCorrect)
                TriggerBreachVisuals();

            RefreshAllCardsUI();
        }

        private void JudgeWebsiteCard(int idx, bool approve)
        {
            if (idx < 0 || idx >= webData.Length || webData[idx].judged) return;

            bool isScam = webData[idx].isScam;
            bool isCorrect = (isScam && !approve) || (!isScam && approve);

            webData[idx].judged = true;
            webData[idx].judgedCorrect = isCorrect;
            webData[idx].judgedStatus = isCorrect ? (approve ? "[+] APPROVED" : "[X] DECLINED") : "[!] พลาด!";

            if (ScoreManager.Instance != null)
            {
                if (approve) ScoreManager.Instance.RecordJobAccepted(isScam);
                else ScoreManager.Instance.RecordJobDeclined(isScam);
            }

            if (!isCorrect)
                TriggerBreachVisuals();

            RefreshAllCardsUI();
            if (websiteWindow != null && websiteWindow.activeSelf)
                RenderWebsiteBrowserDetail(currentWebBrowserIdx);
        }

        private void TriggerBreachVisuals()
        {
            if (!gameObject.activeInHierarchy) return;
            StopCoroutine(nameof(BreachRoutine));
            StartCoroutine(nameof(BreachRoutine));
        }

        private IEnumerator BreachRoutine()
        {
            if (breachRedOverlay != null) breachRedOverlay.alpha = 0.85f;
            if (breachCrackOverlay != null) breachCrackOverlay.alpha = 1f;

            Vector2 basePos = monitorFrameRect != null ? monitorFrameRect.anchoredPosition : Vector2.zero;
            float shakeDur = 0.45f;
            float t = 0f;
            while (t < shakeDur)
            {
                t += Time.unscaledDeltaTime;
                float decay = 1f - (t / shakeDur);
                if (monitorFrameRect != null)
                {
                    float ox = Mathf.Sin(t * 75f) * 14f * decay;
                    float oy = Mathf.Cos(t * 62f) * 9f * decay;
                    monitorFrameRect.anchoredPosition = basePos + new Vector2(ox, oy);
                }
                yield return null;
            }
            if (monitorFrameRect != null) monitorFrameRect.anchoredPosition = basePos;

            yield return new WaitForSecondsRealtime(0.45f);

            float fadeDur = 1.1f;
            t = 0f;
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / fadeDur);
                if (breachRedOverlay != null) breachRedOverlay.alpha = a * 0.85f;
                if (breachCrackOverlay != null) breachCrackOverlay.alpha = a;
                yield return null;
            }
            if (breachRedOverlay != null) breachRedOverlay.alpha = 0f;
            if (breachCrackOverlay != null) breachCrackOverlay.alpha = 0f;
        }

        // ==================== NON-OVERLAPPING SCREEN NAVIGATION ====================
        public void ShowDesktopFolders()
        {
            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(true);
            RefreshAllCardsUI();
            PlayScanBeamSweep();
        }

        public void OpenGmailWindow()
        {
            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(false);
            if (gmailWindow != null) gmailWindow.SetActive(true);
            RefreshAllCardsUI();
            PlayScanBeamSweep();
        }

        public void CloseGmailWindow()
        {
            ShowDesktopFolders();
        }

        public void OpenWebsiteWindow()
        {
            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(false);
            if (websiteListWindow != null)
            {
                websiteListWindow.SetActive(true);
                RefreshAllCardsUI();
                PlayScanBeamSweep();
            }
            else if (websiteWindow != null)
            {
                websiteWindow.SetActive(true);
                RenderWebsiteBrowserDetail(0);
            }
        }

        public void OpenWebsiteBrowserDetail(int idx)
        {
            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(false);
            if (websiteWindow != null) websiteWindow.SetActive(true);
            RenderWebsiteBrowserDetail(idx);
            PlayScanBeamSweep();
        }

        private void RenderWebsiteBrowserDetail(int idx)
        {
            if (webData == null) InitTerminalData();
            currentWebBrowserIdx = Mathf.Clamp(idx, 0, webData.Length - 1);
            var w = webData[currentWebBrowserIdx];

            if (websiteUrlText != null) websiteUrlText.text = w.fullUrl;
            if (websiteSenderNoteText != null) websiteSenderNoteText.text = w.senderNote;
            if (websiteTitleText != null) websiteTitleText.text = w.webTitle;
            if (websiteDescText != null) websiteDescText.text = w.webDesc;

            if (websiteSecurityBadgeText != null)
            {
                websiteSecurityBadgeText.text = w.isSecureHttps ? "[HTTPS] ปลอดภัย" : "[!] HTTP ไม่ปลอดภัย";
                websiteSecurityBadgeText.color = w.isSecureHttps
                    ? new Color(0.53f, 0.94f, 0.67f, 1f)
                    : new Color(0.98f, 0.80f, 0.20f, 1f);
            }
            if (websiteSecurityBadgeBg != null)
            {
                websiteSecurityBadgeBg.color = w.isSecureHttps
                    ? new Color(0.08f, 0.33f, 0.18f, 1f)
                    : new Color(0.28f, 0.14f, 0.04f, 1f);
            }

            for (int i = 0; i < websiteFormFieldTexts.Count; i++)
            {
                if (websiteFormFieldTexts[i] == null) continue;
                if (w.webFields != null && i < w.webFields.Length)
                {
                    websiteFormFieldTexts[i].transform.parent.gameObject.SetActive(true);
                    websiteFormFieldTexts[i].text = "• " + w.webFields[i];
                }
                else
                {
                    websiteFormFieldTexts[i].transform.parent.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < websiteTabButtons.Count; i++)
            {
                if (websiteTabButtons[i] == null) continue;
                var img = websiteTabButtons[i].GetComponent<Image>();
                var txt = websiteTabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                bool sel = (i == currentWebBrowserIdx);
                if (img != null)
                    img.color = sel ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.015f, 0.08f, 0.04f, 1f);
                if (txt != null && i < webData.Length)
                {
                    string statusSuffix = webData[i].judged ? (" • " + webData[i].judgedStatus) : "";
                    txt.text = "► " + webData[i].tabLabel + statusSuffix;
                    txt.color = sel ? new Color(0.01f, 0.04f, 0.02f, 1f) : new Color(0.29f, 0.87f, 0.50f, 1f);
                }
            }

            if (websiteAcceptButton != null) websiteAcceptButton.interactable = !w.judged;
            if (websiteDeclineButton != null) websiteDeclineButton.interactable = !w.judged;
        }

        public void OpenWebsiteDirectly(EmailDataSO email)
        {
            currentlySelectedWebsiteEmail = email;
            OpenWebsiteBrowserDetail(0);
        }

        public void CloseWebsiteWindow()
        {
            ShowDesktopFolders();
        }

        public void OpenRulesWindow()
        {
            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(false);
            if (rulesWindow != null) rulesWindow.SetActive(true);
            PlayScanBeamSweep();
        }

        public void CloseRulesWindow()
        {
            ShowDesktopFolders();
        }

        public void SelectEmail(EmailDataSO email)
        {
            currentlySelectedEmail = email;
            if (email == null) return;
            if (EmailManager.Instance != null) EmailManager.Instance.MarkAsRead(email);
            if (emailSenderText != null) emailSenderText.text = $"From: {email.senderName} <{email.senderEmail}>";
            if (emailSubjectText != null) emailSubjectText.text = email.subject;
            if (emailBodyText != null) emailBodyText.text = email.bodyText;
        }

        private void OnAcceptCurrentEmail()
        {
            if (currentlySelectedEmail != null && EmailManager.Instance != null)
                EmailManager.Instance.AcceptEmail(currentlySelectedEmail);
        }

        private void OnDeclineCurrentEmail()
        {
            if (currentlySelectedEmail != null && EmailManager.Instance != null)
                EmailManager.Instance.DeclineEmail(currentlySelectedEmail);
        }

        private void OnOpenWebsiteFromEmail()
        {
            OpenWebsiteBrowserDetail(0);
        }

        private void CloseAllSubWindows()
        {
            if (gmailWindow != null) gmailWindow.SetActive(false);
            if (websiteListWindow != null) websiteListWindow.SetActive(false);
            if (websiteWindow != null) websiteWindow.SetActive(false);
            if (rulesWindow != null) rulesWindow.SetActive(false);
        }
    }
}
