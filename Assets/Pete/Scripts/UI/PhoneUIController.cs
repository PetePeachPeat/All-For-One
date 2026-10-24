using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pete.Data;
using Pete.Systems;
using Pete.Interaction;

namespace Pete.UI
{
    public class PhoneUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject phoneMainPanel;
        [SerializeField] private GameObject idleStandbyPanel;
        [SerializeField] private GameObject ringingPanel;
        [SerializeField] private GameObject inCallPanel;
        [SerializeField] private GameObject decisionPanel;

        [Header("Horror Mode Roots")]
        [SerializeField] private GameObject normalBackdropLayer;
        [SerializeField] private GameObject horrorRedBackdropLayer;
        [SerializeField] private GameObject ghostEyeballRoot;
        [SerializeField] private GameObject ghostScreamRoot;
        [SerializeField] private Image phoneScreenBgImage;
        [SerializeField] private Image phoneFrameBezelImage;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI idleClockText;
        [SerializeField] private TextMeshProUGUI idleSubClockText;
        [SerializeField] private TextMeshProUGUI ringingClockText;
        [SerializeField] private TextMeshProUGUI callerNameText;
        [SerializeField] private TextMeshProUGUI callerNumberText;
        [SerializeField] private TextMeshProUGUI inCallCallerNameText;
        [SerializeField] private TextMeshProUGUI inCallCallerNumberText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI carrierHeaderText;
        [SerializeField] private TextMeshProUGUI bottomStatusBadgeText;
        [SerializeField] private Image bottomStatusBadgeBorder;
        [SerializeField] private TextMeshProUGUI ghostEyeballCountdownText;
        [SerializeField] private TextMeshProUGUI ghostScreamCountdownText;

        [Header("Buttons")]
        [SerializeField] private Button answerButton;
        [SerializeField] private Button hangupRingingButton;
        [SerializeField] private Button nextDialogueButton;
        [SerializeField] private Button acceptOfferButton;
        [SerializeField] private Button declineOfferButton;
        [SerializeField] private Button exitPhoneButton;

        [Header("Animated Horror Elements")]
        [SerializeField] private List<RectTransform> eyeballPupils = new List<RectTransform>();
        [SerializeField] private List<RectTransform> screamFloodTexts = new List<RectTransform>();
        [SerializeField] private RectTransform phoneFrameShakeRect;

        private enum ActivePhoneVisualMode
        {
            IdleStandby,
            NormalIncoming,
            NormalInCall,
            GhostEyeball,
            GhostScream
        }

        private ActivePhoneVisualMode visualMode = ActivePhoneVisualMode.IdleStandby;
        private Vector2[] pupilBasePositions;
        private Vector2[] screamBasePositions;
        private Vector2 phoneFrameBasePos;

        private void Start()
        {
            if (answerButton != null)
                answerButton.onClick.AddListener(OnAnswerClicked);
            if (hangupRingingButton != null)
                hangupRingingButton.onClick.AddListener(OnDeclineOfferClicked);
            if (nextDialogueButton != null)
                nextDialogueButton.onClick.AddListener(OnNextDialogueClicked);
            if (acceptOfferButton != null)
                acceptOfferButton.onClick.AddListener(OnAcceptOfferClicked);
            if (declineOfferButton != null)
                declineOfferButton.onClick.AddListener(OnDeclineOfferClicked);
            if (exitPhoneButton != null)
                exitPhoneButton.onClick.AddListener(ExitPhone);

            CacheAnimationBasePositions();

            if (PhoneManager.Instance != null)
            {
                PhoneManager.Instance.OnPhoneRingingStarted += OnRingingStarted;
                PhoneManager.Instance.OnPhoneCallAnswered += OnCallAnswered;
                PhoneManager.Instance.OnDialogueAdvanced += OnDialogueUpdated;
                PhoneManager.Instance.OnPhoneCallEnded += OnCallEnded;
            }

            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostAverted += OnGhostResolved;
                GhostManager.Instance.OnGhostDebuffApplied += OnGhostDebuffResolved;
            }

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged += UpdatePhoneClock;
                UpdatePhoneClock(GameTimeManager.Instance.CurrentHour, GameTimeManager.Instance.CurrentMinute);
            }

            ShowIdleStandbyScreen();

            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted += HandleInspectStarted;
                CameraController.Instance.OnInspectEnded += HandleInspectEnded;
            }

            if (phoneMainPanel != null) phoneMainPanel.SetActive(false);
        }

        private void CacheAnimationBasePositions()
        {
            if (phoneFrameShakeRect != null)
                phoneFrameBasePos = phoneFrameShakeRect.anchoredPosition;

            pupilBasePositions = new Vector2[eyeballPupils.Count];
            for (int i = 0; i < eyeballPupils.Count; i++)
            {
                if (eyeballPupils[i] != null)
                    pupilBasePositions[i] = eyeballPupils[i].anchoredPosition;
            }

            screamBasePositions = new Vector2[screamFloodTexts.Count];
            for (int i = 0; i < screamFloodTexts.Count; i++)
            {
                if (screamFloodTexts[i] != null)
                    screamBasePositions[i] = screamFloodTexts[i].anchoredPosition;
            }
        }

        private void HandleInspectStarted(InspectTargetType type)
        {
            if (phoneMainPanel != null)
            {
                bool active = (type == InspectTargetType.Phone);
                phoneMainPanel.SetActive(active);
                if (active)
                {
                    if (GameTimeManager.Instance != null)
                        UpdatePhoneClock(GameTimeManager.Instance.CurrentHour, GameTimeManager.Instance.CurrentMinute);

                    bool ghostRunning = GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive;
                    if (!ghostRunning && (PhoneManager.Instance == null || PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Idle))
                    {
                        ShowIdleStandbyScreen();
                    }
                }
            }
        }

        private void HandleInspectEnded()
        {
            if (phoneMainPanel != null)
            {
                phoneMainPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted -= HandleInspectStarted;
                CameraController.Instance.OnInspectEnded -= HandleInspectEnded;
            }
            if (PhoneManager.Instance != null)
            {
                PhoneManager.Instance.OnPhoneRingingStarted -= OnRingingStarted;
                PhoneManager.Instance.OnPhoneCallAnswered -= OnCallAnswered;
                PhoneManager.Instance.OnDialogueAdvanced -= OnDialogueUpdated;
                PhoneManager.Instance.OnPhoneCallEnded -= OnCallEnded;
            }
            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostAverted -= OnGhostResolved;
                GhostManager.Instance.OnGhostDebuffApplied -= OnGhostDebuffResolved;
            }
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged -= UpdatePhoneClock;
            }
        }

        private void UpdatePhoneClock(int hour, int minute)
        {
            string h12 = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetDisplayHour12H() : "12 AM";
            string t24 = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetDisplayTime24H() : $"{hour:00}:{minute:00}";

            if (idleClockText != null)
                idleClockText.text = t24;
            if (idleSubClockText != null)
                idleSubClockText.text = $"{h12}  •  NIGHT 1";
            if (ringingClockText != null)
                ringingClockText.text = $"{t24} ({h12})";
        }

        private void Update()
        {
            if (phoneMainPanel == null || !phoneMainPanel.activeInHierarchy) return;

            float t = Time.unscaledTime;

            // Update countdown if in ghost mode
            if (visualMode == ActivePhoneVisualMode.GhostEyeball || visualMode == ActivePhoneVisualMode.GhostScream)
            {
                if (GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive)
                {
                    float rem = GhostManager.Instance.RemainingCountdown;
                    string timerStr = $"[!] สายผีตัด — รีบไปศาลเจ้าใน: {rem:0.0}s";
                    if (ghostEyeballCountdownText != null) ghostEyeballCountdownText.text = timerStr;
                    if (ghostScreamCountdownText != null) ghostScreamCountdownText.text = timerStr;
                }
                else
                {
                    ShowIdleStandbyScreen();
                    return;
                }
            }

            // Animate roving pupils in GhostEyeball mode
            if (visualMode == ActivePhoneVisualMode.GhostEyeball && pupilBasePositions != null)
            {
                for (int i = 0; i < eyeballPupils.Count; i++)
                {
                    var rt = eyeballPupils[i];
                    if (rt == null) continue;
                    float speed = 2.4f + (i % 4) * 0.65f;
                    float phase = i * 1.37f;
                    float maxOffsetX = (i == 0) ? 26f : 12f;
                    float maxOffsetY = (i == 0) ? 10f : 5f;
                    float ox = Mathf.Sin(t * speed + phase) * maxOffsetX;
                    float oy = Mathf.Cos(t * (speed * 0.8f) + phase) * maxOffsetY;
                    rt.anchoredPosition = pupilBasePositions[i] + new Vector2(ox, oy);
                }
            }

            // Animate chaotic shaking words in GhostScream mode
            if (visualMode == ActivePhoneVisualMode.GhostScream && screamBasePositions != null)
            {
                for (int i = 0; i < screamFloodTexts.Count; i++)
                {
                    var rt = screamFloodTexts[i];
                    if (rt == null) continue;
                    float jx = Mathf.Sin(t * 48f + i * 2.1f) * 5.5f;
                    float jy = Mathf.Cos(t * 54f + i * 1.7f) * 4.5f;
                    rt.anchoredPosition = screamBasePositions[i] + new Vector2(jx, jy);
                }

                if (phoneFrameShakeRect != null)
                {
                    float sx = Mathf.Sin(t * 65f) * 3.5f;
                    float sy = Mathf.Cos(t * 58f) * 2.5f;
                    phoneFrameShakeRect.anchoredPosition = phoneFrameBasePos + new Vector2(sx, sy);
                }
            }
            else if (phoneFrameShakeRect != null)
            {
                phoneFrameShakeRect.anchoredPosition = phoneFrameBasePos;
            }
        }

        public void ExitPhone()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitInspect();
            }
        }

        public void ShowIdleStandbyScreen()
        {
            visualMode = ActivePhoneVisualMode.IdleStandby;
            ApplyThemeColors(false);

            if (phoneScreenBgImage != null)
                phoneScreenBgImage.color = new Color(0.008f, 0.012f, 0.018f, 1f);

            if (idleStandbyPanel != null) idleStandbyPanel.SetActive(true);
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (GameTimeManager.Instance != null)
                UpdatePhoneClock(GameTimeManager.Instance.CurrentHour, GameTimeManager.Instance.CurrentMinute);

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "● โทรศัพท์สแตนด์บาย (ไม่มีสายเรียกเข้า)";
                bottomStatusBadgeText.color = new Color(0.52f, 0.68f, 0.74f, 0.9f);
            }
        }

        public void TriggerGhostEyeballMode()
        {
            visualMode = ActivePhoneVisualMode.GhostEyeball;
            ApplyThemeColors(true);

            if (idleStandbyPanel != null) idleStandbyPanel.SetActive(false);
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(true);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "● ผีพูดขู่! รีบกด A/D หันไปสวดมนต์ที่ศาลเจ้า!";
                bottomStatusBadgeText.color = new Color(0.98f, 0.35f, 0.35f, 1f);
            }

            if (GhostManager.Instance != null && Application.isPlaying && !GhostManager.Instance.IsCountdownActive)
            {
                GhostManager.Instance.StartGhostCountdown(GhostType.Threat);
            }
        }

        public void TriggerGhostScreamMode()
        {
            visualMode = ActivePhoneVisualMode.GhostScream;
            ApplyThemeColors(true);

            if (idleStandbyPanel != null) idleStandbyPanel.SetActive(false);
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(true);

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "[!] ผีกรี๊ด! รีบกด A/D หันไปสวดมนต์ที่ศาลเจ้า!";
                bottomStatusBadgeText.color = new Color(0.98f, 0.28f, 0.28f, 1f);
            }

            if (GhostManager.Instance != null && Application.isPlaying && !GhostManager.Instance.IsCountdownActive)
            {
                GhostManager.Instance.StartGhostCountdown(GhostType.Scream);
            }
        }

        private void ApplyThemeColors(bool isHorrorRed)
        {
            if (normalBackdropLayer != null) normalBackdropLayer.SetActive(!isHorrorRed);
            if (horrorRedBackdropLayer != null) horrorRedBackdropLayer.SetActive(isHorrorRed);

            if (phoneScreenBgImage != null)
            {
                phoneScreenBgImage.color = isHorrorRed
                    ? new Color(0.58f, 0.04f, 0.04f, 1f)
                    : new Color(0.015f, 0.035f, 0.05f, 1f);
            }
            if (phoneFrameBezelImage != null)
            {
                phoneFrameBezelImage.color = isHorrorRed
                    ? new Color(0.28f, 0.04f, 0.04f, 1f)
                    : new Color(0.09f, 0.13f, 0.16f, 1f);
            }
            if (carrierHeaderText != null)
            {
                carrierHeaderText.text = isHorrorRed
                    ? "SEC-NET   • GHOST LINE   5G |||"
                    : "SEC-NET                  5G |||";
                carrierHeaderText.color = isHorrorRed
                    ? new Color(0.95f, 0.65f, 0.65f, 0.9f)
                    : new Color(0.58f, 0.72f, 0.78f, 0.9f);
            }
        }

        // ==================== PHONE MANAGER EVENT HANDLERS ====================
        private void OnRingingStarted(PhoneCallDataSO call)
        {
            if (call == null) return;
            visualMode = ActivePhoneVisualMode.NormalIncoming;
            ApplyThemeColors(false);

            if (idleStandbyPanel != null) idleStandbyPanel.SetActive(false);
            if (ringingPanel != null) ringingPanel.SetActive(true);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (callerNameText != null) callerNameText.text = $"\"{call.callerName}\"";
            if (callerNumberText != null) callerNumberText.text = call.callerNumber;
            if (inCallCallerNameText != null) inCallCallerNameText.text = $"\"{call.callerName}\"";
            if (inCallCallerNumberText != null) inCallCallerNumberText.text = call.callerNumber;

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "► สายเรียกเข้า! กดปุ่มสีเขียวเพื่อรับสาย";
                bottomStatusBadgeText.color = new Color(0.45f, 0.92f, 0.98f, 1f);
            }
        }

        private void OnCallAnswered(PhoneCallDataSO call)
        {
            if (call == null) return;

            if (call.callType == CallType.GhostThreat)
            {
                TriggerGhostEyeballMode();
                return;
            }
            if (call.callType == CallType.GhostScream)
            {
                TriggerGhostScreamMode();
                return;
            }

            visualMode = ActivePhoneVisualMode.NormalInCall;
            ApplyThemeColors(false);

            if (idleStandbyPanel != null) idleStandbyPanel.SetActive(false);
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(true);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (inCallCallerNameText != null) inCallCallerNameText.text = $"\"{call.callerName}\"";
            if (inCallCallerNumberText != null) inCallCallerNumberText.text = call.callerNumber;
        }

        private void OnDialogueUpdated(string line, bool isLastLine)
        {
            if (dialogueText != null) dialogueText.text = line;

            if (isLastLine && PhoneManager.Instance != null && PhoneManager.Instance.CurrentCall != null &&
                !PhoneManager.Instance.IsGhostCall(PhoneManager.Instance.CurrentCall.callType))
            {
                if (decisionPanel != null) decisionPanel.SetActive(true);
                if (nextDialogueButton != null) nextDialogueButton.gameObject.SetActive(false);
                if (bottomStatusBadgeText != null)
                {
                    bottomStatusBadgeText.text = "► เลือก APPROVE หรือ DECLINE สายนี้";
                    bottomStatusBadgeText.color = new Color(0.45f, 0.92f, 0.98f, 1f);
                }
            }
            else
            {
                if (decisionPanel != null) decisionPanel.SetActive(false);
                if (nextDialogueButton != null) nextDialogueButton.gameObject.SetActive(!isLastLine);
                if (bottomStatusBadgeText != null)
                {
                    bottomStatusBadgeText.text = "● กำลังสนทนา... กดฟังประโยคถัดไป";
                    bottomStatusBadgeText.color = new Color(0.45f, 0.92f, 0.98f, 1f);
                }
            }
        }

        private void OnCallEnded()
        {
            bool ghostRunning = GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive;
            if (!ghostRunning && visualMode != ActivePhoneVisualMode.GhostEyeball && visualMode != ActivePhoneVisualMode.GhostScream)
            {
                ShowIdleStandbyScreen();
            }
        }

        private void OnGhostResolved()
        {
            ShowIdleStandbyScreen();
        }

        private void OnGhostDebuffResolved(GhostType type, float duration)
        {
            ShowIdleStandbyScreen();
        }

        private void OnAnswerClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Ringing)
            {
                PhoneManager.Instance.AnswerCall();
            }
        }

        private void OnNextDialogueClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall)
            {
                PhoneManager.Instance.NextDialogue();
            }
        }

        private void OnAcceptOfferClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall)
            {
                PhoneManager.Instance.AcceptCallOffer();
                ShowIdleStandbyScreen();
            }
        }

        private void OnDeclineOfferClicked()
        {
            if (PhoneManager.Instance != null &&
                (PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall ||
                 PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Ringing))
            {
                var currentCall = PhoneManager.Instance.CurrentCall;
                bool isGhost = currentCall != null && PhoneManager.Instance.IsGhostCall(currentCall.callType);
                CallType ghostCallType = currentCall != null ? currentCall.callType : CallType.JobOffer;

                PhoneManager.Instance.DeclineCallOffer();

                if (isGhost)
                {
                    if (ghostCallType == CallType.GhostThreat) TriggerGhostEyeballMode();
                    else TriggerGhostScreamMode();
                }
                else
                {
                    ShowIdleStandbyScreen();
                }
            }
        }
    }
}
