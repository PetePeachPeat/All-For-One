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

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI callerNameText;
        [SerializeField] private TextMeshProUGUI callerNumberText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("Buttons")]
        [SerializeField] private Button answerButton;
        [SerializeField] private Button nextDialogueButton;
        [SerializeField] private Button acceptOfferButton;
        [SerializeField] private Button declineOfferButton;
        [SerializeField] private Button exitPhoneButton;

        private void Start()
        {
            if (answerButton != null)
                answerButton.onClick.AddListener(OnAnswerClicked);
            if (nextDialogueButton != null)
                nextDialogueButton.onClick.AddListener(OnNextDialogueClicked);
            if (acceptOfferButton != null)
                acceptOfferButton.onClick.AddListener(OnAcceptOfferClicked);
            if (declineOfferButton != null)
                declineOfferButton.onClick.AddListener(OnDeclineOfferClicked);
            if (exitPhoneButton != null)
                exitPhoneButton.onClick.AddListener(ExitPhone);

            if (PhoneManager.Instance != null)
            {
                PhoneManager.Instance.OnPhoneRingingStarted += OnRingingStarted;
                PhoneManager.Instance.OnPhoneCallAnswered += OnCallAnswered;
                PhoneManager.Instance.OnDialogueAdvanced += OnDialogueUpdated;
                PhoneManager.Instance.OnPhoneCallEnded += OnCallEnded;
            }

            ResetUI();

            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted += HandleInspectStarted;
                CameraController.Instance.OnInspectEnded += HandleInspectEnded;
            }

            if (phoneMainPanel != null) phoneMainPanel.SetActive(false);
        }

        private void HandleInspectStarted(InspectTargetType type)
        {
            if (phoneMainPanel != null)
            {
                phoneMainPanel.SetActive(type == InspectTargetType.Phone);
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

        public void ExitPhone()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitInspect();
            }
        }

        private void OnRingingStarted(PhoneCallDataSO call)
        {
            if (ringingPanel != null) ringingPanel.SetActive(true);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);

            if (callerNameText != null) callerNameText.text = call.callerName;
            if (callerNumberText != null) callerNumberText.text = call.callerNumber;
        }

        private void OnCallAnswered(PhoneCallDataSO call)
        {
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(true);
            if (decisionPanel != null) decisionPanel.SetActive(false);

            if (callerNameText != null) callerNameText.text = call.callerName;
            if (callerNumberText != null) callerNumberText.text = call.callerNumber;
        }

        private void OnDialogueUpdated(string line, bool isLastLine)
        {
            if (dialogueText != null) dialogueText.text = line;

            // ถ้าเป็นประโยคสุดท้าย และไม่ใช่สายผี ให้เปิดปุ่มตัดสินใจ
            if (isLastLine && PhoneManager.Instance != null && !PhoneManager.Instance.IsGhostCall(PhoneManager.Instance.CurrentCall.callType))
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
            ResetUI();
        }

        private void ResetUI()
        {
            if (ringingPanel != null) ringingPanel.SetActive(false);
            if (inCallPanel != null) inCallPanel.SetActive(false);
            if (decisionPanel != null) decisionPanel.SetActive(false);
        }

        private void OnAnswerClicked()
        {
            if (PhoneManager.Instance != null)
                PhoneManager.Instance.AnswerCall();
        }

        private void OnNextDialogueClicked()
        {
            if (PhoneManager.Instance != null)
                PhoneManager.Instance.NextDialogue();
        }

        private void OnAcceptOfferClicked()
        {
            if (PhoneManager.Instance != null)
                PhoneManager.Instance.AcceptCallOffer();
        }

        private void OnDeclineOfferClicked()
        {
            if (PhoneManager.Instance != null)
                PhoneManager.Instance.DeclineCallOffer();
        }
    }
}
