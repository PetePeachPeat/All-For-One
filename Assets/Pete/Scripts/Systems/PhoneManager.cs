using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pete.Data;

namespace Pete.Systems
{
    public class PhoneManager : MonoBehaviour
    {
        public static PhoneManager Instance { get; private set; }

        public enum PhoneState
        {
            Idle,
            Ringing,
            InCall
        }

        [Header("Call Schedule")]
        [SerializeField] private List<PhoneCallDataSO> scheduledCalls = new List<PhoneCallDataSO>();

        [Header("Audio")]
        [SerializeField] private AudioSource phoneAudioSource;
        [SerializeField] private AudioClip ringtoneClip;
        [SerializeField] private AudioClip hangupClip;

        [Header("Settings")]
        [Tooltip("ระยะเวลาที่ปล่อยให้โทรศัพท์ดังก่อนสายตัด (วินาที)")]
        [SerializeField] private float ringTimeoutSeconds = 15f;

        // Runtime states
        public PhoneState CurrentState { get; private set; } = PhoneState.Idle;
        public PhoneCallDataSO CurrentCall { get; private set; }
        public int CurrentDialogueIndex { get; private set; } = 0;

        private float ringTimer = 0f;
        private HashSet<string> deliveredCallIds = new HashSet<string>();

        public event Action<PhoneCallDataSO> OnPhoneRingingStarted;
        public event Action<PhoneCallDataSO> OnPhoneCallAnswered;
        public event Action<string, bool> OnDialogueAdvanced; // text, isLast
        public event Action OnPhoneCallEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged += CheckScheduledCalls;
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged -= CheckScheduledCalls;
            }
        }

        private void Update()
        {
            if (CurrentState == PhoneState.Ringing)
            {
                ringTimer += Time.deltaTime;
                if (ringTimer >= ringTimeoutSeconds)
                {
                    // สายตัดเพราะไม่ได้รับ (Missed Call)
                    MissedCall();
                }
            }
        }

        private void CheckScheduledCalls(int hour, int minute)
        {
            // ถ้าผีกำลังนับถอยหลัง จะไม่ให้สายใหม่เข้า
            if (GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive)
                return;

            if (CurrentState != PhoneState.Idle) return;

            for (int i = 0; i < scheduledCalls.Count; i++)
            {
                var call = scheduledCalls[i];
                if (call == null) continue;

                if (!deliveredCallIds.Contains(call.callId) &&
                    call.targetHour == hour && call.targetMinute == minute)
                {
                    StartRinging(call);
                    break;
                }
            }
        }

        public void StartRinging(PhoneCallDataSO call)
        {
            if (CurrentState != PhoneState.Idle) return;

            CurrentCall = call;
            CurrentState = PhoneState.Ringing;
            ringTimer = 0f;
            deliveredCallIds.Add(call.callId);

            if (phoneAudioSource != null && ringtoneClip != null)
            {
                phoneAudioSource.clip = ringtoneClip;
                phoneAudioSource.loop = true;
                phoneAudioSource.Play();
            }

            OnPhoneRingingStarted?.Invoke(call);
        }

        /// <summary>
        /// ผู้เล่นคลิกโทรศัพท์เพื่อรับสาย
        /// </summary>
        public void AnswerCall()
        {
            if (CurrentState != PhoneState.Ringing || CurrentCall == null) return;

            CurrentState = PhoneState.InCall;
            CurrentDialogueIndex = 0;

            if (phoneAudioSource != null)
            {
                phoneAudioSource.Stop();
                phoneAudioSource.loop = false;
            }

            OnPhoneCallAnswered?.Invoke(CurrentCall);

            // ถ้ามีเสียงพากย์ ให้เล่น
            if (CurrentCall.voiceClip != null && phoneAudioSource != null)
            {
                phoneAudioSource.PlayOneShot(CurrentCall.voiceClip);
            }

            // ถ้าเป็นสายผี ให้เริ่มบทพูดและตัดสายอัตโนมัติ
            if (IsGhostCall(CurrentCall.callType))
            {
                StartCoroutine(RoutineGhostCallSequence(CurrentCall));
            }
            else
            {
                ShowCurrentDialogue();
            }
        }

        private IEnumerator RoutineGhostCallSequence(PhoneCallDataSO call)
        {
            // แสดงประโยคของผี
            if (call.dialogueLines != null && call.dialogueLines.Length > 0)
            {
                for (int i = 0; i < call.dialogueLines.Length; i++)
                {
                    OnDialogueAdvanced?.Invoke(call.dialogueLines[i], i == call.dialogueLines.Length - 1);
                    yield return new WaitForSeconds(3.5f);
                }
            }
            else
            {
                yield return new WaitForSeconds(3.0f);
            }

            // วางสายอัตโนมัติ
            GhostType ghostType = (call.callType == CallType.GhostThreat) ? GhostType.Threat : GhostType.Scream;
            EndCall(false);

            // เริ่มนับถอยหลังของผีทันที
            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.StartGhostCountdown(ghostType);
            }
        }

        public void NextDialogue()
        {
            if (CurrentState != PhoneState.InCall || CurrentCall == null) return;

            if (CurrentCall.dialogueLines == null || CurrentDialogueIndex >= CurrentCall.dialogueLines.Length - 1)
            {
                // จบประโยคแล้ว รอการกดรับ/ปฏิเสธ
                return;
            }

            CurrentDialogueIndex++;
            ShowCurrentDialogue();
        }

        private void ShowCurrentDialogue()
        {
            if (CurrentCall == null || CurrentCall.dialogueLines == null || CurrentCall.dialogueLines.Length == 0)
            {
                OnDialogueAdvanced?.Invoke("...", true);
                return;
            }

            bool isLast = CurrentDialogueIndex >= CurrentCall.dialogueLines.Length - 1;
            OnDialogueAdvanced?.Invoke(CurrentCall.dialogueLines[CurrentDialogueIndex], isLast);
        }

        public void AcceptCallOffer()
        {
            if (CurrentState != PhoneState.InCall || CurrentCall == null) return;

            if (!IsGhostCall(CurrentCall.callType))
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RecordJobAccepted(CurrentCall.isScam);
                }
            }

            EndCall(true);
        }

        public void DeclineCallOffer()
        {
            if (CurrentState != PhoneState.InCall || CurrentCall == null) return;

            if (!IsGhostCall(CurrentCall.callType))
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RecordJobDeclined(CurrentCall.isScam);
                }
            }

            EndCall(true);
        }

        private void MissedCall()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordIgnoredEvent();
            }
            EndCall(false);
        }

        private void EndCall(bool playHangupSound)
        {
            if (phoneAudioSource != null)
            {
                phoneAudioSource.Stop();
                if (playHangupSound && hangupClip != null)
                {
                    phoneAudioSource.PlayOneShot(hangupClip);
                }
            }

            CurrentState = PhoneState.Idle;
            CurrentCall = null;
            OnPhoneCallEnded?.Invoke();
        }

        public bool IsGhostCall(CallType type)
        {
            return type == CallType.GhostThreat || type == CallType.GhostScream;
        }

        public void SetScheduledCalls(List<PhoneCallDataSO> calls)
        {
            scheduledCalls = calls;
        }
    }
}
