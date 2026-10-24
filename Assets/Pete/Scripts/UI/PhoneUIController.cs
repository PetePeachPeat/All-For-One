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
    public class PhoneUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject phoneMainPanel;
        [SerializeField] private GameObject ringingPanel;
        [SerializeField] private GameObject inCallPanel;
        [SerializeField] private GameObject decisionPanel;

        [Header("Horror Mode Roots (Screenshots 3 & 4)")]
        [SerializeField] private GameObject normalBackdropLayer;
        [SerializeField] private GameObject horrorRedBackdropLayer;
        [SerializeField] private GameObject ghostEyeballRoot;
        [SerializeField] private GameObject ghostScreamRoot;
        [SerializeField] private Image phoneScreenBgImage;
        [SerializeField] private Image phoneFrameBezelImage;

        [Header("Text Displays")]
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

        [Header("Top Mode Switcher & Replay Buttons (Screenshots 2, 3, 4)")]
        [SerializeField] private Button modeIncomingButton;
        [SerializeField] private Button modeFakeCaseButton;
        [SerializeField] private Button modeRealCaseButton;
        [SerializeField] private Button modeGhostEyeballButton;
        [SerializeField] private Button modeGhostScreamButton;
        [SerializeField] private Button replayEyeballJumpscareButton;
        [SerializeField] private Button replayScreamJumpscareButton;

        [Header("Animated Horror Elements")]
        [SerializeField] private List<RectTransform> eyeballPupils = new List<RectTransform>();
        [SerializeField] private List<RectTransform> screamFloodTexts = new List<RectTransform>();
        [SerializeField] private RectTransform phoneFrameShakeRect;

        private enum ActivePhoneVisualMode
        {
            NormalIncoming,
            NormalInCall,
            GhostEyeball,
            GhostScream
        }

        private ActivePhoneVisualMode visualMode = ActivePhoneVisualMode.NormalIncoming;
        private bool isDemoFakeCase = true;
        private int demoDialogueIdx = 0;
        private float localGhostCountdown = 7.0f;
        private bool localCountdownRunning = false;
        private Vector2[] pupilBasePositions;
        private Vector2[] screamBasePositions;
        private Vector2 phoneFrameBasePos;

        private readonly string[] fakeDialogueLines =
        {
            "\"สวัสดีครับ ติดต่อจาก สภ.เมือง ฝ่ายสืบสวนคดีฟอกเงิน\"",
            "\"พบบัญชีของคุณพัวพันคดีสีเทา ต้องโอนเงินทั้งหมดมาตรวจสอบเดี๋ยวนี้!\"",
            "\"กดยืนยันโอนเงินประกันตัว 15,000฿ หรือจะให้ตำรวจออกหมายจับ?!\""
        };

        private readonly string[] realDialogueLines =
        {
            "\"สวัสดีค่ะ ติดต่อจากฝ่ายบุคคล บจก. SEC-CORP นะคะ\"",
            "\"ยืนยันตารางเข้ากะดึกของคุณคืนนี้ เวลา 00:00 - 06:00 น. ค่ะ\"",
            "\"ไม่มีค่าธรรมเนียมใดๆ กดยืนยันรับทราบตารางงานได้เลยค่ะ\""
        };

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

            if (modeIncomingButton != null)
                modeIncomingButton.onClick.AddListener(() => ShowDemoIncomingCall(isDemoFakeCase));
            if (modeFakeCaseButton != null)
                modeFakeCaseButton.onClick.AddListener(() => ShowDemoIncomingCall(true));
            if (modeRealCaseButton != null)
                modeRealCaseButton.onClick.AddListener(() => ShowDemoIncomingCall(false));
            if (modeGhostEyeballButton != null)
                modeGhostEyeballButton.onClick.AddListener(TriggerGhostEyeballMode);
            if (modeGhostScreamButton != null)
                modeGhostScreamButton.onClick.AddListener(TriggerGhostScreamMode);
            if (replayEyeballJumpscareButton != null)
                replayEyeballJumpscareButton.onClick.AddListener(TriggerGhostEyeballMode);
            if (replayScreamJumpscareButton != null)
                replayScreamJumpscareButton.onClick.AddListener(TriggerGhostScreamMode);

            CacheAnimationBasePositions();

            if (PhoneManager.Instance != null)
            {
                PhoneManager.Instance.OnPhoneRingingStarted += OnRingingStarted;
                PhoneManager.Instance.OnPhoneCallAnswered += OnCallAnswered;
                PhoneManager.Instance.OnDialogueAdvanced += OnDialogueUpdated;
                PhoneManager.Instance.OnPhoneCallEnded += OnCallEnded;
            }

            ShowDemoIncomingCall(true);

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
                if (active && (PhoneManager.Instance == null || PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Idle))
                {
                    if (visualMode != ActivePhoneVisualMode.GhostEyeball && visualMode != ActivePhoneVisualMode.GhostScream)
                    {
                        ShowDemoIncomingCall(isDemoFakeCase);
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
        }

        private void Update()
        {
            if (phoneMainPanel == null || !phoneMainPanel.activeInHierarchy) return;

            float t = Time.unscaledTime;

            // Update countdown if in ghost mode
            if (visualMode == ActivePhoneVisualMode.GhostEyeball || visualMode == ActivePhoneVisualMode.GhostScream)
            {
                float rem = localGhostCountdown;
                if (GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive)
                {
                    rem = GhostManager.Instance.RemainingCountdown;
                }
                else if (localCountdownRunning)
                {
                    localGhostCountdown = Mathf.Max(0f, localGhostCountdown - Time.unscaledDeltaTime);
                    rem = localGhostCountdown;
                }

                string timerStr = $"[!] สายผีตัด\nใน: {rem:0.0}s";
                if (ghostEyeballCountdownText != null) ghostEyeballCountdownText.text = timerStr;
                if (ghostScreamCountdownText != null) ghostScreamCountdownText.text = $"[!] ใน: {rem:0.0}s";
            }

            // Animate roving pupils in GhostEyeball mode (Screenshot 3)
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

            // Animate chaotic shaking words in GhostScream mode (Screenshot 4)
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

        // ==================== DEMO & SCHEDULED CALL MODES ====================
        public void ShowDemoIncomingCall(bool fakeCase)
        {
            isDemoFakeCase = fakeCase;
            visualMode = ActivePhoneVisualMode.NormalIncoming;
            localCountdownRunning = false;

            ApplyThemeColors(false);

            if (ringingPanel != null) ringingPanel.SetActive(true);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            string cNum = fakeCase ? "098-712-XXXX" : "02-910-8800";
            string cName = fakeCase ? "\"สภ.เมือง (ฝ่ายสืบสวน)\"" : "\"บจก. SEC-CORP (ฝ่ายบุคคล)\"";

            if (callerNumberText != null) callerNumberText.text = cNum;
            if (callerNameText != null) callerNameText.text = cName;
            if (inCallCallerNumberText != null) inCallCallerNumberText.text = cNum;
            if (inCallCallerNameText != null) inCallCallerNameText.text = cName;

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "► กดปุ่มสีเขียวเพื่อรับสาย";
                bottomStatusBadgeText.color = new Color(0.45f, 0.92f, 0.98f, 1f);
            }
        }

        public void TriggerGhostEyeballMode()
        {
            visualMode = ActivePhoneVisualMode.GhostEyeball;
            localGhostCountdown = 7.0f;
            localCountdownRunning = true;

            ApplyThemeColors(true);

            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(true);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "● ลูกตากำลังกลอกมองไปมา...";
                bottomStatusBadgeText.color = new Color(0.45f, 0.92f, 0.98f, 1f);
            }

            if (GhostManager.Instance != null && Application.isPlaying)
            {
                GhostManager.Instance.StartGhostCountdown(GhostType.Threat);
            }
        }

        public void TriggerGhostScreamMode()
        {
            visualMode = ActivePhoneVisualMode.GhostScream;
            localGhostCountdown = 7.0f;
            localCountdownRunning = true;

            ApplyThemeColors(true);

            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(true);

            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = "[!] ผีกรี๊ด! ตัวหนังสือทะลักเต็มจอ!";
                bottomStatusBadgeText.color = new Color(0.98f, 0.28f, 0.28f, 1f);
            }

            if (GhostManager.Instance != null && Application.isPlaying)
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
                    : new Color(0.02f, 0.05f, 0.07f, 1f);
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

            if (ringingPanel != null) ringingPanel.SetActive(true);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
            if (ghostEyeballRoot != null) ghostEyeballRoot.SetActive(false);
            if (ghostScreamRoot != null) ghostScreamRoot.SetActive(false);

            if (callerNameText != null) callerNameText.text = $"\"{call.callerName}\"";
            if (callerNumberText != null) callerNumberText.text = call.callerNumber;
            if (inCallCallerNameText != null) inCallCallerNameText.text = $"\"{call.callerName}\"";
            if (inCallCallerNumberText != null) inCallCallerNumberText.text = call.callerNumber;
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
            }
            else
            {
                if (decisionPanel != null) decisionPanel.SetActive(false);
                if (nextDialogueButton != null) nextDialogueButton.gameObject.SetActive(!isLastLine);
            }
        }

        private void OnCallEnded()
        {
            if (visualMode != ActivePhoneVisualMode.GhostEyeball && visualMode != ActivePhoneVisualMode.GhostScream)
            {
                ShowDemoIncomingCall(isDemoFakeCase);
            }
        }

        private void OnAnswerClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.Ringing)
            {
                PhoneManager.Instance.AnswerCall();
                return;
            }

            // Demo interactive flow when no scheduled PhoneManager call is ringing
            visualMode = ActivePhoneVisualMode.NormalInCall;
            demoDialogueIdx = 0;
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(true);
            UpdateDemoDialogueView();
        }

        private void UpdateDemoDialogueView()
        {
            string[] lines = isDemoFakeCase ? fakeDialogueLines : realDialogueLines;
            demoDialogueIdx = Mathf.Clamp(demoDialogueIdx, 0, lines.Length - 1);
            bool isLast = (demoDialogueIdx >= lines.Length - 1);

            if (dialogueText != null) dialogueText.text = lines[demoDialogueIdx];
            if (nextDialogueButton != null) nextDialogueButton.gameObject.SetActive(!isLast);
            if (decisionPanel != null) decisionPanel.SetActive(isLast);
            if (bottomStatusBadgeText != null)
            {
                bottomStatusBadgeText.text = isLast
                    ? "► เลือก APPROVE หรือ DECLINE สายนี้"
                    : "● กำลังสนทนา... กดฟังประโยคถัดไป";
            }
        }

        private void OnNextDialogueClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall)
            {
                PhoneManager.Instance.NextDialogue();
                return;
            }

            demoDialogueIdx++;
            UpdateDemoDialogueView();
        }

        private void OnAcceptOfferClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall)
            {
                PhoneManager.Instance.AcceptCallOffer();
                return;
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RecordJobAccepted(isDemoFakeCase);

            if (bottomStatusBadgeText != null)
            {
                bool ok = !isDemoFakeCase;
                bottomStatusBadgeText.text = ok ? "[+] รับสายงานจริงสำเร็จ!" : "[!] พลาด! คุณหลงเชื่อสายมิจฉาชีพ!";
                bottomStatusBadgeText.color = ok ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.98f, 0.28f, 0.28f, 1f);
            }
            ShowDemoIncomingCall(!isDemoFakeCase);
        }

        private void OnDeclineOfferClicked()
        {
            if (PhoneManager.Instance != null && PhoneManager.Instance.CurrentState == PhoneManager.PhoneState.InCall)
            {
                PhoneManager.Instance.DeclineCallOffer();
                return;
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RecordJobDeclined(isDemoFakeCase);

            if (bottomStatusBadgeText != null)
            {
                bool ok = isDemoFakeCase;
                bottomStatusBadgeText.text = ok ? "[+] ตัดสายมิจฉาชีพสำเร็จ!" : "[!] คุณปฏิเสธสายงานจริง!";
                bottomStatusBadgeText.color = ok ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.98f, 0.28f, 0.28f, 1f);
            }
            ShowDemoIncomingCall(!isDemoFakeCase);
        }
    }
}
