using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        public GameObject activeContentRoot;
        public GameObject emptySlotRoot;
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

    public class ComputerUIController : MonoBehaviour
    {
        [Header("Desktop Root Panel")]
        [SerializeField] private GameObject desktopRootPanel;
        [SerializeField] private GameObject desktopFoldersView;

        [Header("Desktop HUD")]
        [SerializeField] private TextMeshProUGUI timeDisplay12HText;
        [SerializeField] private TextMeshProUGUI nightDisplayText;
        [SerializeField] private Button exitComputerButton;
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
        [SerializeField] private List<CRTMessageCardRefs> gmailCards = new List<CRTMessageCardRefs>();

        [Header("WEBSITE List Window (3-Column Link Selector View)")]
        [SerializeField] private GameObject websiteListWindow;
        [SerializeField] private Button websiteListCloseButton;
        [SerializeField] private List<CRTMessageCardRefs> websiteCards = new List<CRTMessageCardRefs>();

        [Header("WEBSITE Browser Preview Window")]
        [SerializeField] private GameObject websiteWindow;
        [SerializeField] private Button websiteCloseButton;
        [SerializeField] private Button websiteBackToListButton;
        [SerializeField] private TextMeshProUGUI websiteUrlText;
        [SerializeField] private TextMeshProUGUI websiteSenderNoteText;
        [SerializeField] private TextMeshProUGUI websiteTitleText;
        [SerializeField] private TextMeshProUGUI websiteDescText;
        [SerializeField] private List<TextMeshProUGUI> websiteFormFieldTexts = new List<TextMeshProUGUI>();
        [SerializeField] private List<Button> websiteTabButtons = new List<Button>();
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

        [Header("Random Task Spawner Settings")]
        [SerializeField] private float initialSpawnDelayMin = 3.5f;
        [SerializeField] private float initialSpawnDelayMax = 6.0f;
        [SerializeField] private float nextSpawnIntervalMin = 10.0f;
        [SerializeField] private float nextSpawnIntervalMax = 17.0f;

        private int currentWebBrowserIdx = 0;
        private bool isCrtPoweredOn = true;
        private float nextRandomTaskTimer = 4.5f;
        [Header("Audio")]
        [SerializeField] private AudioSource pingAudioSource;
        [SerializeField] private AudioClip terminalPingClip;

        private struct TerminalMailItem
        {
            public bool hasItem;
            public string idLabel;
            public string badgeDefault;
            public string fromAddress;
            public string metaLabel;
            public string body;
            public bool isScam;
        }

        private struct TerminalWebItem
        {
            public bool hasItem;
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
        }

        private TerminalMailItem[] gmailSlots;
        private TerminalWebItem[] webSlots;

        private List<TerminalMailItem> mailPool = new List<TerminalMailItem>();
        private List<TerminalWebItem> webPool = new List<TerminalWebItem>();
        private int lastMailPoolIdx = -1;
        private int lastWebPoolIdx = -1;

        private void Awake()
        {
            InitPoolsAndEmptySlots();
            EnsurePingAudio();
        }

        private void EnsurePingAudio()
        {
            pingAudioSource = GetComponent<AudioSource>();
            if (pingAudioSource == null)
            {
                pingAudioSource = gameObject.AddComponent<AudioSource>();
                pingAudioSource.playOnAwake = false;
                pingAudioSource.loop = false;
            }
            if (terminalPingClip == null)
            {
                terminalPingClip = GenerateTerminalPingClip();
            }
        }

        private static AudioClip GenerateTerminalPingClip()
        {
            int sampleRate = 22050;
            float duration = 0.24f;
            int total = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.11f) ? 880f : 1174.66f;
                float env = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.22f * env;
            }
            var clip = AudioClip.Create("ProceduralTerminalPing", total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayTerminalNotificationPing()
        {
            if (pingAudioSource != null && terminalPingClip != null)
            {
                float sfxVol = PlayerPrefs.GetInt("MM_VolSFX", PlayerPrefs.GetInt("MM_VolSfx", 85)) / 100f;
                pingAudioSource.PlayOneShot(terminalPingClip, Mathf.Clamp01(0.75f * sfxVol));
            }
        }

        private void InitPoolsAndEmptySlots()
        {
            // All 3 Gmail slots and 3 Website slots start EMPTY!
            gmailSlots = new TerminalMailItem[3];
            webSlots = new TerminalWebItem[3];

            mailPool = new List<TerminalMailItem>
            {
                new TerminalMailItem
                {
                    badgeDefault = "งานออนไลน์",
                    fromAddress = "vip-job@fast-money.net",
                    metaLabel = "โอนค่ามัดจำ 1,500฿",
                    body = "รับคนช่วยคอนเฟิร์มออเดอร์ที่บ้าน รายได้วันละ 5,000฿ แต่ต้องโอนค่าเปิดรหัส VIP ก่อน 1,500฿",
                    isScam = true
                },
                new TerminalMailItem
                {
                    badgeDefault = "นัดสัมภาษณ์",
                    fromAddress = "hr@sec-corp.co.th",
                    metaLabel = "Contract_2026.pdf",
                    body = "บริษัท SEC-CORP ตอบรับท่านเข้าทำงานเจ้าหน้าที่กะดึก เงินเดือน 28,000฿ ไม่มีค่าสมัครใดๆ",
                    isScam = false
                },
                new TerminalMailItem
                {
                    badgeDefault = "ด่วนที่สุด",
                    fromAddress = "admin@dark-payroll.xyz",
                    metaLabel = "Salary_Slip.pdf.exe",
                    body = "จ้างเปิดบัญชีรับโอนเงินเดือนละ 30,000฿ ส่งสมุดบัญชีและรหัส ATM มาให้ทางบริษัททันที",
                    isScam = true
                },
                new TerminalMailItem
                {
                    badgeDefault = "ฝ่ายบุคคล",
                    fromAddress = "recruit@nova-logistics.co.th",
                    metaLabel = "Shift_Schedule.pdf",
                    body = "ยืนยันรับท่านเข้าทำงานพนักงานตรวจเช็กสต็อกกะดึก เงินเดือน 24,500฿ มีประกันสังคมครบ ไม่มีค่าธรรมเนียม",
                    isScam = false
                },
                new TerminalMailItem
                {
                    badgeDefault = "งานแพ็กของ",
                    fromAddress = "home-pack99@easy-cash.org",
                    metaLabel = "โอนค่าประกันอุปกรณ์ 990฿",
                    body = "รับงานแพ็กสบู่ส่งออกที่บ้าน รายได้ดี แต่ต้องโอนค่ามัดจำอุปกรณ์ล่วงหน้า 990฿ เข้าบัญชีส่วนบุคคลก่อนจัดส่ง",
                    isScam = true
                },
                new TerminalMailItem
                {
                    badgeDefault = "รับสมัครงาน",
                    fromAddress = "careers@datalink-center.co.th",
                    metaLabel = "Offer_Letter.pdf",
                    body = "บริษัท DATA-LINK ตอบรับท่านในตำแหน่งพนักงานคีย์ข้อมูลกะดึก มีอุปกรณ์ให้ยืมฟรีโดยไม่เก็บเงินมัดจำ",
                    isScam = false
                },
                new TerminalMailItem
                {
                    badgeDefault = "แจ้งเตือนบัญชี",
                    fromAddress = "security@bank-verify-alert.xyz",
                    metaLabel = "Urgent_Unlock.scr.exe",
                    body = "บัญชีรับเงินเดือนของท่านถูกล็อกชั่วคราว กรุณาเปิดไฟล์แนบ .exe และโอนเงิน 2,000฿ เพื่อยืนยันตัวตนทันที",
                    isScam = true
                },
                new TerminalMailItem
                {
                    badgeDefault = "งานพาร์ทไทม์",
                    fromAddress = "hr@metro-archive.co.th",
                    metaLabel = "Document_Check_Job.pdf",
                    body = "รับเจ้าหน้าที่ตรวจสอบเอกสารดิจิทัลกะดึก จ่ายเงินโอนเข้าบัญชีโดยตรงทุกสัปดาห์ ไม่มีการเรียกเก็บค่าสมัคร",
                    isScam = false
                }
            };

            webPool = new List<TerminalWebItem>
            {
                new TerminalWebItem
                {
                    badgeDefault = "คืนค่าไฟ",
                    fromAddress = "claim@mea-refund.net",
                    shortUrl = "http://mea-free-refund.net",
                    fullUrl = "http://mea-free-refund.net/claim-pin",
                    senderNote = "งานรับเงินคืนค่าไฟด่วน (แนบลิงก์)",
                    cardBody = "สิทธิ์รับเงินคืนค่าไฟ 4,500฿ กดลิงก์แนบเพื่อกรอกเลขบัญชีและรหัส PIN รับเงินทันที",
                    isSecureHttps = false,
                    isScam = true,
                    webTitle = "ระบบรับเงินคืนค่าไฟด่วน (MEA REFUND)",
                    webDesc = "กรุณากรอกข้อมูลเพื่อรับเงินคืน 4,500 บาท เข้าบัญชีทันที:",
                    webFields = new[] { "เบอร์โทรศัพท์", "เลขบัญชีธนาคาร", "รหัส PIN ATM 6 หลัก (บังคับกรอก)" }
                },
                new TerminalWebItem
                {
                    badgeDefault = "งานบริษัท",
                    fromAddress = "hr@sec-corp.co.th",
                    shortUrl = "https://sec-corp.co.th",
                    fullUrl = "https://www.sec-corp.co.th/onboarding",
                    senderNote = "ลงทะเบียนพนักงานใหม่ SEC-CORP (แนบลิงก์)",
                    cardBody = "เว็บลงทะเบียนพนักงานใหม่ SEC-CORP กดลิงก์เพื่อกรอกชื่อและเลือกกะทำงาน (ไม่มีค่าสมัคร)",
                    isSecureHttps = true,
                    isScam = false,
                    webTitle = "ระบบยืนยันตัวตนพนักงานใหม่ บจก. SEC-CORP",
                    webDesc = "กรอกชื่อและเลือกกะเวลาทำงาน (เงินเดือน 28,000฿ ไม่มีค่าธรรมเนียมใดๆ):",
                    webFields = new[] { "ชื่อ-นามสกุลพนักงาน", "เบอร์โทรศัพท์ติดต่อ", "กะเวลาทำงาน (Night Shift 00:00 - 06:00)" }
                },
                new TerminalWebItem
                {
                    badgeDefault = "หุ้น AI",
                    fromAddress = "vip@ai-trade888.xyz",
                    shortUrl = "http://rich-ai-trade888.xyz",
                    fullUrl = "http://rich-ai-trade888.xyz/deposit",
                    senderNote = "เว็บลงทุนหุ้น AI ปันผล 300% (แนบลิงก์)",
                    cardBody = "เว็บเทรดหุ้น AI กำไร 300% กดลิงก์เพื่อฝากเงิน 1,000฿ และกรอกรหัสผ่านแอปธนาคาร",
                    isSecureHttps = false,
                    isScam = true,
                    webTitle = "AI SUPER TRADE // ฝาก 1,000฿ ถอนได้ 10,000฿",
                    webDesc = "โอนเงินเปิดพอร์ตขั้นต่ำ 1,000 บาท พร้อมกรอกรหัสผ่าน Mobile Banking:",
                    webFields = new[] { "ไอดีผู้ใช้งาน", "รหัสผ่านแอปธนาคาร 6 หลัก", "จำนวนเงินโอนเปิดพอร์ต (ขั้นต่ำ 1,000฿)" }
                },
                new TerminalWebItem
                {
                    badgeDefault = "คลังสินค้า",
                    fromAddress = "hr@nova-logistics.co.th",
                    shortUrl = "https://nova-logistics.co.th",
                    fullUrl = "https://portal.nova-logistics.co.th/staff-checkin",
                    senderNote = "แบบฟอร์มรายงานตัวพนักงานคลังสินค้า (แนบลิงก์)",
                    cardBody = "กรุณากรอกรหัสพนักงานและเลือกสาขาที่สะดวกเข้ากะดึก (ไม่ต้องกรอกข้อมูลบัตรเครดิตหรือรหัสผ่านธนาคาร)",
                    isSecureHttps = true,
                    isScam = false,
                    webTitle = "NOVA LOGISTICS // ระบบลงทะเบียนพนักงานเข้ากะดึก",
                    webDesc = "ยืนยันตารางเริ่มงานและเลือกจุดรับชุดยูนิฟอร์มฟรี:",
                    webFields = new[] { "ชื่อ-นามสกุลจริง", "สาขาคลังสินค้าที่เลือก", "ขนาดชุดยูนิฟอร์ม (S / M / L / XL)" }
                },
                new TerminalWebItem
                {
                    badgeDefault = "อนุมัติไว",
                    fromAddress = "loan@fast-vip-money.xyz",
                    shortUrl = "http://fast-vip-money.xyz",
                    fullUrl = "http://fast-vip-money.xyz/unlock-wallet",
                    senderNote = "เว็บเบิกเงินเดือนล่วงหน้าด่วน (แนบลิงก์)",
                    cardBody = "กดลิงก์เพื่อรับวงเงินทำงานล่วงหน้า 20,000฿ โดยมีเงื่อนไขต้องโอนค่าปลดล็อกระบบ 1,800฿ ก่อน",
                    isSecureHttps = false,
                    isScam = true,
                    webTitle = "FAST VIP WALLET // เบิกเงินล่วงหน้า 20,000฿",
                    webDesc = "กรอกข้อมูลบัตรเดบิตและโอนเงินค่าถอนรหัสปลดล็อก 1,800 บาท:",
                    webFields = new[] { "เลขหน้าบัตร ATM 16 หลัก", "รหัส CVV หลังบัตร 3 หลัก", "รหัส OTP ที่ได้รับทาง SMS" }
                },
                new TerminalWebItem
                {
                    badgeDefault = "คีย์ข้อมูล",
                    fromAddress = "onboarding@datalink-center.co.th",
                    shortUrl = "https://datalink-center.co.th",
                    fullUrl = "https://www.datalink-center.co.th/remote-work",
                    senderNote = "ระบบรับงานคีย์ข้อมูลกะดึก DATA-LINK (แนบลิงก์)",
                    cardBody = "เข้าสู่ระบบพอร์ทัลพนักงานของ DATA-LINK เพื่อยืนยันอีเมลรับส่งงาน (ไม่มีค่าใช้จ่ายแรกเข้า)",
                    isSecureHttps = true,
                    isScam = false,
                    webTitle = "DATA-LINK CENTER // พอร์ทัลรับงานคีย์ข้อมูลออนไลน์",
                    webDesc = "ลงทะเบียนอีเมลสำหรับรับไฟล์งานเอกสารประจำกะดึก:",
                    webFields = new[] { "ชื่อ-นามสกุลผู้ปฏิบัติงาน", "อีเมลสำหรับรับไฟล์งาน", "หมายเลขโทรศัพท์ติดต่อกลับ" }
                }
            };

            nextRandomTaskTimer = UnityEngine.Random.Range(initialSpawnDelayMin, initialSpawnDelayMax);
        }

        private void Start()
        {
            if (exitComputerButton != null)
                exitComputerButton.onClick.AddListener(ExitComputer);
            if (crtPowerButton != null)
                crtPowerButton.onClick.AddListener(ToggleCrtPower);

            if (gmailIconButton != null)
                gmailIconButton.onClick.AddListener(OpenGmailWindow);
            if (gmailCloseButton != null)
                gmailCloseButton.onClick.AddListener(ShowDesktopFolders);

            if (websiteIconButton != null)
                websiteIconButton.onClick.AddListener(OpenWebsiteWindow);
            if (websiteListCloseButton != null)
                websiteListCloseButton.onClick.AddListener(ShowDesktopFolders);

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

            if (websiteAcceptButton != null)
                websiteAcceptButton.onClick.AddListener(() => JudgeWebsiteCard(currentWebBrowserIdx, true));
            if (websiteDeclineButton != null)
                websiteDeclineButton.onClick.AddListener(() => JudgeWebsiteCard(currentWebBrowserIdx, false));

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

            RefreshAllCardsUI();
            ShowDesktopFolders();

            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted += HandleInspectStarted;
                CameraController.Instance.OnInspectEnded += HandleInspectEnded;
            }

            if (desktopRootPanel != null) desktopRootPanel.SetActive(false);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (GameTimeManager.Instance != null && !GameTimeManager.Instance.IsNightActive) return;

            nextRandomTaskTimer -= Time.deltaTime;
            if (nextRandomTaskTimer <= 0f)
            {
                SpawnNextRandomNightTask();
                nextRandomTaskTimer = UnityEngine.Random.Range(nextSpawnIntervalMin, nextSpawnIntervalMax);
            }
        }

        private void SpawnNextRandomNightTask()
        {
            if (gmailSlots == null || webSlots == null) InitPoolsAndEmptySlots();

            int emptyGmailSlot = FindEmptyGmailSlot();
            int emptyWebSlot = FindEmptyWebSlot();
            bool canPhoneRing = PhoneManager.Instance != null &&
                                PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Idle &&
                                (GhostManager.Instance == null || !GhostManager.Instance.IsCountdownActive);

            var candidates = new List<int>(3);
            if (emptyGmailSlot >= 0) candidates.Add(0); // 0 = Gmail
            if (emptyWebSlot >= 0) candidates.Add(1);   // 1 = Website
            if (canPhoneRing) candidates.Add(2);        // 2 = Phone Call

            if (candidates.Count == 0) return;

            int chosenType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (chosenType == 0)
            {
                SpawnRandomGmailIntoSlot(emptyGmailSlot);
            }
            else if (chosenType == 1)
            {
                SpawnRandomWebIntoSlot(emptyWebSlot);
            }
            else if (chosenType == 2 && PhoneManager.Instance != null)
            {
                PhoneManager.Instance.TryTriggerRandomCall();
            }
        }

        private int FindEmptyGmailSlot()
        {
            for (int i = 0; i < gmailSlots.Length; i++)
            {
                if (!gmailSlots[i].hasItem) return i;
            }
            return -1;
        }

        private int FindEmptyWebSlot()
        {
            for (int i = 0; i < webSlots.Length; i++)
            {
                if (!webSlots[i].hasItem) return i;
            }
            return -1;
        }

        private void SpawnRandomGmailIntoSlot(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= gmailSlots.Length || mailPool.Count == 0) return;

            int poolIdx = UnityEngine.Random.Range(0, mailPool.Count);
            if (mailPool.Count > 1 && poolIdx == lastMailPoolIdx)
                poolIdx = (poolIdx + 1) % mailPool.Count;
            lastMailPoolIdx = poolIdx;

            var item = mailPool[poolIdx];
            item.hasItem = true;
            item.idLabel = $"ข้อความ 0{slotIdx + 1}";
            gmailSlots[slotIdx] = item;

            PlayTerminalNotificationPing();
            RefreshAllCardsUI();
        }

        private void SpawnRandomWebIntoSlot(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= webSlots.Length || webPool.Count == 0) return;

            int poolIdx = UnityEngine.Random.Range(0, webPool.Count);
            if (webPool.Count > 1 && poolIdx == lastWebPoolIdx)
                poolIdx = (poolIdx + 1) % webPool.Count;
            lastWebPoolIdx = poolIdx;

            var item = webPool[poolIdx];
            item.hasItem = true;
            item.idLabel = $"ข้อความเว็บ 0{slotIdx + 1}";
            item.tabLabel = $"เว็บไซต์ 0{slotIdx + 1}";
            webSlots[slotIdx] = item;

            PlayTerminalNotificationPing();
            RefreshAllCardsUI();

            if (websiteWindow != null && websiteWindow.activeSelf)
            {
                RenderWebsiteBrowserDetail(currentWebBrowserIdx);
            }
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
                timeDisplay12HText.text = $"{GameTimeManager.Instance.GetDisplayTime24H()} ({GameTimeManager.Instance.GetDisplayHour12H()})";
            }
            if (nightDisplayText != null)
            {
                nightDisplayText.text = "NIGHT 1";
            }
        }

        public void RefreshBadgesAndList()
        {
            RefreshAllCardsUI();
        }

        private void RefreshAllCardsUI()
        {
            if (gmailSlots == null || webSlots == null) InitPoolsAndEmptySlots();

            int gmailActiveCount = 0;
            for (int i = 0; i < gmailSlots.Length; i++)
            {
                if (gmailSlots[i].hasItem) gmailActiveCount++;
                if (i < gmailCards.Count)
                {
                    BindGmailCardUI(gmailCards[i], gmailSlots[i]);
                }
            }

            int webActiveCount = 0;
            for (int i = 0; i < webSlots.Length; i++)
            {
                if (webSlots[i].hasItem) webActiveCount++;
                if (i < websiteCards.Count)
                {
                    BindWebCardUI(websiteCards[i], webSlots[i]);
                }
            }

            if (gmailBadgeObject != null) gmailBadgeObject.SetActive(gmailActiveCount > 0);
            if (gmailUnreadCountText != null) gmailUnreadCountText.text = gmailActiveCount.ToString();

            if (websiteBadgeObject != null) websiteBadgeObject.SetActive(webActiveCount > 0);
            if (websiteAlertCountText != null) websiteAlertCountText.text = webActiveCount.ToString();
        }

        private void BindGmailCardUI(CRTMessageCardRefs ui, TerminalMailItem data)
        {
            if (ui == null) return;

            if (!data.hasItem)
            {
                if (ui.activeContentRoot != null) ui.activeContentRoot.SetActive(false);
                if (ui.emptySlotRoot != null) ui.emptySlotRoot.SetActive(true);
                if (ui.cardBorderImage != null) ui.cardBorderImage.color = new Color(0.08f, 0.24f, 0.14f, 0.45f);
                return;
            }

            if (ui.activeContentRoot != null) ui.activeContentRoot.SetActive(true);
            if (ui.emptySlotRoot != null) ui.emptySlotRoot.SetActive(false);
            if (ui.cardBorderImage != null) ui.cardBorderImage.color = new Color(0.18f, 0.65f, 0.34f, 1f);

            if (ui.idText != null) ui.idText.text = data.idLabel;
            if (ui.fromText != null) ui.fromText.text = $"<color=#22c55e>จาก:</color> {data.fromAddress}";
            if (ui.metaText != null) ui.metaText.text = $"<color=#22c55e>ข้อมูล:</color> {data.metaLabel}";
            if (ui.bodyText != null) ui.bodyText.text = data.body;

            if (ui.badgeText != null)
            {
                ui.badgeText.text = data.badgeDefault;
                ui.badgeText.color = new Color(0.29f, 0.87f, 0.50f, 1f);
            }
            if (ui.badgeBgImage != null)
            {
                ui.badgeBgImage.color = new Color(0.08f, 0.35f, 0.18f, 1f);
            }
            if (ui.approveButton != null) ui.approveButton.interactable = true;
            if (ui.declineButton != null) ui.declineButton.interactable = true;
        }

        private void BindWebCardUI(CRTMessageCardRefs ui, TerminalWebItem data)
        {
            if (ui == null) return;

            if (!data.hasItem)
            {
                if (ui.activeContentRoot != null) ui.activeContentRoot.SetActive(false);
                if (ui.emptySlotRoot != null) ui.emptySlotRoot.SetActive(true);
                if (ui.cardBorderImage != null) ui.cardBorderImage.color = new Color(0.08f, 0.24f, 0.14f, 0.45f);
                return;
            }

            if (ui.activeContentRoot != null) ui.activeContentRoot.SetActive(true);
            if (ui.emptySlotRoot != null) ui.emptySlotRoot.SetActive(false);
            if (ui.cardBorderImage != null) ui.cardBorderImage.color = new Color(0.18f, 0.65f, 0.34f, 1f);

            bool hiUrl = PlayerPrefs.GetInt("MM_UrlHighlight", 0) == 1;
            string urlCol = hiUrl ? (data.isSecureHttps ? "#4ade80" : "#facc15") : "#ffffff";

            if (ui.idText != null) ui.idText.text = data.idLabel;
            if (ui.fromText != null) ui.fromText.text = $"<color=#22c55e>จาก:</color> {data.fromAddress}";
            if (ui.metaText != null) ui.metaText.text = $"<color=#22c55e>ลิงก์:</color> <color={urlCol}>{data.shortUrl}</color>";
            if (ui.bodyText != null) ui.bodyText.text = data.cardBody;

            if (ui.badgeText != null)
            {
                ui.badgeText.text = data.badgeDefault;
                ui.badgeText.color = new Color(0.29f, 0.87f, 0.50f, 1f);
            }
            if (ui.badgeBgImage != null)
            {
                ui.badgeBgImage.color = new Color(0.08f, 0.35f, 0.18f, 1f);
            }
            if (ui.approveButton != null) ui.approveButton.interactable = true;
            if (ui.declineButton != null) ui.declineButton.interactable = true;
        }

        private void JudgeGmailCard(int idx, bool approve)
        {
            if (idx < 0 || idx >= gmailSlots.Length || !gmailSlots[idx].hasItem) return;

            bool isScam = gmailSlots[idx].isScam;
            bool isCorrect = (isScam && !approve) || (!isScam && approve);

            // Clear the slot immediately so it becomes an Empty Slot!
            gmailSlots[idx].hasItem = false;

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
            if (idx < 0 || idx >= webSlots.Length || !webSlots[idx].hasItem) return;

            bool isScam = webSlots[idx].isScam;
            bool isCorrect = (isScam && !approve) || (!isScam && approve);

            // Clear the slot immediately so it becomes an Empty Slot!
            webSlots[idx].hasItem = false;

            if (ScoreManager.Instance != null)
            {
                if (approve) ScoreManager.Instance.RecordJobAccepted(isScam);
                else ScoreManager.Instance.RecordJobDeclined(isScam);
            }

            if (!isCorrect)
                TriggerBreachVisuals();

            RefreshAllCardsUI();

            // If we judged the website from inside the browser preview window, return to the 3-column list view
            if (websiteWindow != null && websiteWindow.activeSelf)
            {
                OpenWebsiteWindow();
            }
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
        }

        public void OpenWebsiteBrowserDetail(int idx)
        {
            if (webSlots == null || idx < 0 || idx >= webSlots.Length || !webSlots[idx].hasItem)
                return;

            CloseAllSubWindows();
            if (desktopFoldersView != null) desktopFoldersView.SetActive(false);
            if (websiteWindow != null) websiteWindow.SetActive(true);
            RenderWebsiteBrowserDetail(idx);
            PlayScanBeamSweep();
        }

        private void RenderWebsiteBrowserDetail(int idx)
        {
            if (webSlots == null) InitPoolsAndEmptySlots();
            if (idx < 0 || idx >= webSlots.Length || !webSlots[idx].hasItem)
            {
                OpenWebsiteWindow();
                return;
            }

            currentWebBrowserIdx = idx;
            var w = webSlots[currentWebBrowserIdx];

            bool hiUrl = PlayerPrefs.GetInt("MM_UrlHighlight", 0) == 1;
            if (websiteUrlText != null)
            {
                websiteUrlText.text = w.fullUrl;
                websiteUrlText.color = hiUrl
                    ? (w.isSecureHttps ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.98f, 0.85f, 0.15f, 1f))
                    : Color.white;
            }
            if (websiteSenderNoteText != null) websiteSenderNoteText.text = w.senderNote;
            if (websiteTitleText != null) websiteTitleText.text = w.webTitle;
            if (websiteDescText != null) websiteDescText.text = w.webDesc;

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
                bool slotHasItem = (i < webSlots.Length && webSlots[i].hasItem);
                websiteTabButtons[i].gameObject.SetActive(slotHasItem);
                if (!slotHasItem) continue;

                var img = websiteTabButtons[i].GetComponent<Image>();
                var txt = websiteTabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                bool sel = (i == currentWebBrowserIdx);
                if (img != null)
                    img.color = sel ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.015f, 0.08f, 0.04f, 1f);
                if (txt != null)
                {
                    txt.text = "► " + webSlots[i].tabLabel;
                    txt.color = sel ? new Color(0.01f, 0.04f, 0.02f, 1f) : new Color(0.29f, 0.87f, 0.50f, 1f);
                }
            }

            if (websiteAcceptButton != null) websiteAcceptButton.interactable = true;
            if (websiteDeclineButton != null) websiteDeclineButton.interactable = true;
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

        private void CloseAllSubWindows()
        {
            if (gmailWindow != null) gmailWindow.SetActive(false);
            if (websiteListWindow != null) websiteListWindow.SetActive(false);
            if (websiteWindow != null) websiteWindow.SetActive(false);
            if (rulesWindow != null) rulesWindow.SetActive(false);
        }
    }
}
