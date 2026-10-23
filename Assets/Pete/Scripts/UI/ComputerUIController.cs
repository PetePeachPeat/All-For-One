using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pete.Data;
using Pete.Systems;
using Pete.Interaction;

namespace Pete.UI
{
    public class ComputerUIController : MonoBehaviour
    {
        [Header("Desktop Root Panel")]
        [SerializeField] private GameObject desktopRootPanel;

        [Header("Desktop HUD")]
        [SerializeField] private TextMeshProUGUI timeDisplay12HText;
        [SerializeField] private TextMeshProUGUI nightDisplayText;
        [SerializeField] private Button exitComputerButton;

        [Header("Desktop App Icons")]
        [SerializeField] private Button gmailIconButton;
        [SerializeField] private GameObject gmailBadgeObject;
        [SerializeField] private TextMeshProUGUI gmailUnreadCountText;

        [SerializeField] private Button websiteIconButton;
        [SerializeField] private GameObject websiteBadgeObject;
        [SerializeField] private TextMeshProUGUI websiteAlertCountText;

        [SerializeField] private Button rulesIconButton;

        [Header("GMAIL Window")]
        [SerializeField] private GameObject gmailWindow;
        [SerializeField] private Button gmailCloseButton;
        [SerializeField] private Transform emailListContainer;
        [SerializeField] private GameObject emailListItemPrefab;

        [Header("GMAIL - Selected Email View")]
        [SerializeField] private TextMeshProUGUI emailSenderText;
        [SerializeField] private TextMeshProUGUI emailSubjectText;
        [SerializeField] private TextMeshProUGUI emailBodyText;
        [SerializeField] private GameObject openWebsiteLinkButton;
        [SerializeField] private Button emailAcceptButton;
        [SerializeField] private Button emailDeclineButton;

        [Header("WEBSITE Window")]
        [SerializeField] private GameObject websiteWindow;
        [SerializeField] private Button websiteCloseButton;
        [SerializeField] private TextMeshProUGUI websiteUrlText;
        [SerializeField] private Image websitePreviewImage;
        [SerializeField] private Button websiteAcceptButton;
        [SerializeField] private Button websiteDeclineButton;

        [Header("RULES Window")]
        [SerializeField] private GameObject rulesWindow;
        [SerializeField] private Button rulesCloseButton;
        [SerializeField] private TextMeshProUGUI rulesContentText;

        // Runtime states
        private EmailDataSO currentlySelectedEmail;
        private EmailDataSO currentlySelectedWebsiteEmail;

        private void Start()
        {
            // ผูกปุ่มหลัก
            if (exitComputerButton != null)
                exitComputerButton.onClick.AddListener(ExitComputer);

            if (gmailIconButton != null)
                gmailIconButton.onClick.AddListener(OpenGmailWindow);
            if (gmailCloseButton != null)
                gmailCloseButton.onClick.AddListener(CloseGmailWindow);

            if (websiteIconButton != null)
                websiteIconButton.onClick.AddListener(OpenWebsiteWindow);
            if (websiteCloseButton != null)
                websiteCloseButton.onClick.AddListener(CloseWebsiteWindow);

            if (rulesIconButton != null)
                rulesIconButton.onClick.AddListener(OpenRulesWindow);
            if (rulesCloseButton != null)
                rulesCloseButton.onClick.AddListener(CloseRulesWindow);

            if (emailAcceptButton != null)
                emailAcceptButton.onClick.AddListener(OnAcceptCurrentEmail);
            if (emailDeclineButton != null)
                emailDeclineButton.onClick.AddListener(OnDeclineCurrentEmail);

            if (websiteAcceptButton != null)
                websiteAcceptButton.onClick.AddListener(OnAcceptCurrentWebsite);
            if (websiteDeclineButton != null)
                websiteDeclineButton.onClick.AddListener(OnDeclineCurrentWebsite);

            if (openWebsiteLinkButton != null)
            {
                var btn = openWebsiteLinkButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnOpenWebsiteFromEmail);
            }

            // ผูกระบบเวลาและอีเมล
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged += UpdateTimeDisplay;
                UpdateTimeDisplay(GameTimeManager.Instance.CurrentHour, GameTimeManager.Instance.CurrentMinute);
            }

            if (EmailManager.Instance != null)
            {
                EmailManager.Instance.OnEmailReceived += OnNewEmailReceived;
                EmailManager.Instance.OnEmailStatusUpdated += RefreshBadgesAndList;
                RefreshBadgesAndList();
            }

            // ปิดหน้าต่างย่อยไว้ตอนเริ่ม
            CloseAllSubWindows();

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
                desktopRootPanel.SetActive(type == InspectTargetType.Computer);
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
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitInspect();
            }
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
            if (EmailManager.Instance == null) return;

            // อัปเดตแบดจ์ GMAIL
            int gmailUnread = EmailManager.Instance.GetUnreadCount(false);
            if (gmailBadgeObject != null) gmailBadgeObject.SetActive(gmailUnread > 0);
            if (gmailUnreadCountText != null) gmailUnreadCountText.text = gmailUnread.ToString();

            // อัปเดตแบดจ์ WEBSITE
            int webUnread = EmailManager.Instance.GetUnreadCount(true);
            if (websiteBadgeObject != null) websiteBadgeObject.SetActive(webUnread > 0);
            if (websiteAlertCountText != null) websiteAlertCountText.text = "!";

            // อัปเดตรายการอีเมลในหน้าต่าง GMAIL ถ้าเปิดอยู่
            if (gmailWindow != null && gmailWindow.activeSelf)
            {
                PopulateGmailList();
            }
        }

        // ==================== GMAIL WINDOW ====================
        public void OpenGmailWindow()
        {
            CloseAllSubWindows();
            if (gmailWindow != null) gmailWindow.SetActive(true);
            PopulateGmailList();
        }

        public void CloseGmailWindow()
        {
            if (gmailWindow != null) gmailWindow.SetActive(false);
        }

        private void PopulateGmailList()
        {
            if (EmailManager.Instance == null || emailListContainer == null) return;

            // เคลียร์รายการเดิม (ถ้ามี prefab ให้ทีม UI ใช้งาน)
            var emails = EmailManager.Instance.GetEmails(false);
            if (emails.Count > 0 && currentlySelectedEmail == null)
            {
                SelectEmail(emails[emails.Count - 1]);
            }
            else if (currentlySelectedEmail != null)
            {
                SelectEmail(currentlySelectedEmail);
            }
        }

        public void SelectEmail(EmailDataSO email)
        {
            currentlySelectedEmail = email;
            if (email == null) return;

            if (EmailManager.Instance != null)
                EmailManager.Instance.MarkAsRead(email);

            if (emailSenderText != null) emailSenderText.text = $"From: {email.senderName} <{email.senderEmail}>";
            if (emailSubjectText != null) emailSubjectText.text = email.subject;
            if (emailBodyText != null) emailBodyText.text = email.bodyText;

            if (openWebsiteLinkButton != null)
                openWebsiteLinkButton.SetActive(email.hasUrl);

            bool isProcessed = EmailManager.Instance != null && EmailManager.Instance.ProcessedEmailIds.Contains(email.emailId);
            if (emailAcceptButton != null) emailAcceptButton.interactable = !isProcessed;
            if (emailDeclineButton != null) emailDeclineButton.interactable = !isProcessed;
        }

        private void OnAcceptCurrentEmail()
        {
            if (currentlySelectedEmail != null && EmailManager.Instance != null)
            {
                EmailManager.Instance.AcceptEmail(currentlySelectedEmail);
                SelectEmail(currentlySelectedEmail);
            }
        }

        private void OnDeclineCurrentEmail()
        {
            if (currentlySelectedEmail != null && EmailManager.Instance != null)
            {
                EmailManager.Instance.DeclineEmail(currentlySelectedEmail);
                SelectEmail(currentlySelectedEmail);
            }
        }

        private void OnOpenWebsiteFromEmail()
        {
            if (currentlySelectedEmail != null && currentlySelectedEmail.hasUrl)
            {
                OpenWebsiteDirectly(currentlySelectedEmail);
            }
        }

        // ==================== WEBSITE WINDOW ====================
        public void OpenWebsiteWindow()
        {
            CloseAllSubWindows();
            if (websiteWindow != null) websiteWindow.SetActive(true);

            // โหลดเว็บล่าสุดที่ส่งมา
            if (EmailManager.Instance != null)
            {
                var webEmails = EmailManager.Instance.GetEmails(true);
                if (webEmails.Count > 0)
                {
                    OpenWebsiteDirectly(webEmails[webEmails.Count - 1]);
                }
            }
        }

        public void OpenWebsiteDirectly(EmailDataSO email)
        {
            currentlySelectedWebsiteEmail = email;
            CloseAllSubWindows();
            if (websiteWindow != null) websiteWindow.SetActive(true);

            if (email == null) return;

            if (EmailManager.Instance != null)
                EmailManager.Instance.MarkAsRead(email);

            if (websiteUrlText != null) websiteUrlText.text = email.urlDisplay;
            if (websitePreviewImage != null)
            {
                websitePreviewImage.sprite = email.websitePreviewImage;
                websitePreviewImage.enabled = (email.websitePreviewImage != null);
            }

            bool isProcessed = EmailManager.Instance != null && EmailManager.Instance.ProcessedEmailIds.Contains(email.emailId);
            if (websiteAcceptButton != null) websiteAcceptButton.interactable = !isProcessed;
            if (websiteDeclineButton != null) websiteDeclineButton.interactable = !isProcessed;
        }

        public void CloseWebsiteWindow()
        {
            if (websiteWindow != null) websiteWindow.SetActive(false);
        }

        private void OnAcceptCurrentWebsite()
        {
            if (currentlySelectedWebsiteEmail != null && EmailManager.Instance != null)
            {
                EmailManager.Instance.AcceptEmail(currentlySelectedWebsiteEmail);
                OpenWebsiteDirectly(currentlySelectedWebsiteEmail);
            }
        }

        private void OnDeclineCurrentWebsite()
        {
            if (currentlySelectedWebsiteEmail != null && EmailManager.Instance != null)
            {
                EmailManager.Instance.DeclineEmail(currentlySelectedWebsiteEmail);
                OpenWebsiteDirectly(currentlySelectedWebsiteEmail);
            }
        }

        // ==================== RULES WINDOW ====================
        public void OpenRulesWindow()
        {
            CloseAllSubWindows();
            if (rulesWindow != null) rulesWindow.SetActive(true);
        }

        public void CloseRulesWindow()
        {
            if (rulesWindow != null) rulesWindow.SetActive(false);
        }

        private void CloseAllSubWindows()
        {
            if (gmailWindow != null) gmailWindow.SetActive(false);
            if (websiteWindow != null) websiteWindow.SetActive(false);
            if (rulesWindow != null) rulesWindow.SetActive(false);
        }
    }
}
