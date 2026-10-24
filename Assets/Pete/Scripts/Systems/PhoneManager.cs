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
        [SerializeField] private float ringTimeoutSeconds = 18f;

        // Runtime states
        public PhoneState CurrentState { get; private set; } = PhoneState.Idle;
        public PhoneCallDataSO CurrentCall { get; private set; }
        public int CurrentDialogueIndex { get; private set; } = 0;

        private float ringTimer = 0f;
        private HashSet<string> deliveredCallIds = new HashSet<string>();
        private List<PhoneCallDataSO> runtimeRandomPool = new List<PhoneCallDataSO>();
        private int lastRandomCallIndex = -1;

        public event Action<PhoneCallDataSO> OnPhoneRingingStarted;
        public event Action<PhoneCallDataSO> OnPhoneCallAnswered;
        public event Action<string, bool> OnDialogueAdvanced; // text, isLast
        public event Action OnPhoneCallEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            if (ringtoneClip == null)
                ringtoneClip = GenerateProceduralRingtoneClip();

            BuildRuntimeRandomCallPool();
        }

        private void BuildRuntimeRandomCallPool()
        {
            runtimeRandomPool.Clear();

            // 1. Legitimate Job Calls
            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_legit_01", "บจก. SEC-CORP (ฝ่ายบุคคล)", "02-910-8800", CallType.JobOffer, false,
                new[]
                {
                    "\"สวัสดีค่ะ ติดต่อจากฝ่ายบุคคล บจก. SEC-CORP นะคะ\"",
                    "\"คุณผ่านการคัดเลือกเข้าทำงานกะดึก เงินเดือน 28,000 บาทค่ะ\"",
                    "\"ไม่มีค่าใช้จ่ายหรือค่ามัดจำใดๆ กดยืนยันรับข้อเสนองานได้เลยค่ะ\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_legit_02", "คลังสินค้า NOVA LOGISTICS", "02-441-2900", CallType.JobOffer, false,
                new[]
                {
                    "\"สวัสดีครับ โทรจากฝ่ายจัดซื้อและคลังสินค้า NOVA LOGISTICS ครับ\"",
                    "\"รับตำแหน่งเจ้าหน้าที่ตรวจเช็กสต็อกกะดึก มีประกันสังคมและค่ากะให้ครบครับ\"",
                    "\"ไม่ต้องโอนเงินสำรองใดๆ ทั้งสิ้น สนใจกดยืนยันเริ่มงานพรุ่งนี้ไหมครับ?\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_legit_03", "DATA-LINK CENTER (HR)", "02-772-1055", CallType.JobOffer, false,
                new[]
                {
                    "\"สวัสดีค่ะ โทรแจ้งผลสัมภาษณ์ตำแหน่งพนักงานคีย์ข้อมูลจาก DATA-LINK ค่ะ\"",
                    "\"บริษัทจัดส่งอุปกรณ์คอมพิวเตอร์ให้ที่บ้านโดยไม่คิดค่าประกันอุปกรณ์ค่ะ\"",
                    "\"หากตกลงรับงาน กด APPROVE เพื่อลงชื่อในสัญญาจ้างได้เลยค่ะ\""
                }));

            // 2. Scam Calls
            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_scam_01", "สภ.เมือง (ฝ่ายสืบสวน)", "098-712-4491", CallType.Scam, true,
                new[]
                {
                    "\"สวัสดีครับ ติดต่อจาก สภ.เมือง ฝ่ายสืบสวนคดีฟอกเงิน\"",
                    "\"พบบัญชีของคุณพัวพันคดีสีเทา ต้องโอนเงินทั้งหมดมาตรวจสอบเดี๋ยวนี้!\"",
                    "\"กดยืนยันโอนเงินประกันตัว 15,000฿ หรือจะให้ตำรวจออกหมายจับ?!\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_scam_02", "ฝ่ายบริหารงาน VIP ออนไลน์", "065-889-1120", CallType.Scam, true,
                new[]
                {
                    "\"สวัสดีค่ะ งานกดรับออเดอร์สินค้าแบรนด์เนม รายได้วันละ 4,500 บาทค่ะ\"",
                    "\"แต่คุณต้องโอนค่าเปิดรหัสสมาชิก VIP 1,200 บาทเข้ามาที่บัญชีส่วนบุคคลก่อนนะคะ\"",
                    "\"กด APPROVE เพื่อโอนเงินเปิดรหัสและรับโบนัสทันทีเลยไหมคะ?\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_scam_03", "กรมศุลกากร (แจ้งพัสดุตกค้าง)", "082-331-9004", CallType.Scam, true,
                new[]
                {
                    "\"แจ้งเตือนด่วน! คุณมีพัสดุผิดกฎหมายถูกอายัดไว้ที่ด่านศุลกากร\"",
                    "\"หากไม่ต้องการถูกดำเนินคดี ต้องชำระค่าธรรมเนียมถอนอายัด 8,000 บาททันที\"",
                    "\"กดยืนยันเพื่อผูกบัญชีธนาคารและรหัส OTP ของคุณเดี๋ยวนี้!\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_scam_04", "ศูนย์คัดกรองงานต่างประเทศ", "091-004-7788", CallType.Scam, true,
                new[]
                {
                    "\"ดีใจด้วยครับ! คุณได้รับคัดเลือกไปทำงานแพ็กผลไม้ต่างประเทศ รายได้เดือนละแสน\"",
                    "\"เพียงโอนค่ายื่นวีซ่าด่วน 5,500 บาทเข้าบัญชีหัวหน้าสายงานภายในคืนนี้\"",
                    "\"กดตกลงเพื่อโอนเงินจองสิทธิ์ด่วนเลยครับ!\""
                }));

            // 3. Ghost Calls (Threat / Eyeball & Scream)
            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_ghost_threat_01", "หมายเลขไม่ระบุชื่อ", "000-000-0000", CallType.GhostThreat, false,
                new[]
                {
                    "\"...เห็นนะ... กำลังจ้องมองแกอยู่จากข้างหลัง...\""
                }));

            runtimeRandomPool.Add(CreateRuntimeCall(
                "rnd_ghost_scream_01", "สายมรณะ", "666-666-6666", CallType.GhostScream, false,
                new[]
                {
                    "\"กรี๊ดดดดดดดดดดดดดดดด!!!\""
                }));
        }

        private static PhoneCallDataSO CreateRuntimeCall(
            string id,
            string callerName,
            string callerNumber,
            CallType type,
            bool isScam,
            string[] lines)
        {
            var so = ScriptableObject.CreateInstance<PhoneCallDataSO>();
            so.callId = id;
            so.callerName = callerName;
            so.callerNumber = callerNumber;
            so.callType = type;
            so.isScam = isScam;
            so.dialogueLines = lines;
            return so;
        }

        private static AudioClip GenerateProceduralRingtoneClip()
        {
            int sampleRate = 22050;
            float duration = 2.0f; // 0.8s dual-tone ring, 1.2s pause
            int total = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                if (t < 0.8f)
                {
                    float mod = Mathf.Sin(2f * Mathf.PI * 20f * t) > 0f ? 1f : 0.25f;
                    float w1 = Mathf.Sin(2f * Mathf.PI * 440f * t);
                    float w2 = Mathf.Sin(2f * Mathf.PI * 480f * t);
                    samples[i] = (w1 + w2) * 0.22f * mod;
                }
                else
                {
                    samples[i] = 0f;
                }
            }
            var clip = AudioClip.Create("ProceduralPhoneRing", total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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
                    MissedCall();
                }
            }
        }

        private void CheckScheduledCalls(int hour, int minute)
        {
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

        /// <summary>
        /// สุ่มสายโทรเข้าใหม่ทันที (ทั้งงานจริง, มิจฉาชีพ, หรือผีโทรมา) หากโทรศัพท์ว่างอยู่
        /// </summary>
        public bool TryTriggerRandomCall()
        {
            if (CurrentState != PhoneState.Idle) return false;
            if (GhostManager.Instance != null && GhostManager.Instance.IsCountdownActive) return false;
            if (runtimeRandomPool == null || runtimeRandomPool.Count == 0) BuildRuntimeRandomCallPool();

            int idx = UnityEngine.Random.Range(0, runtimeRandomPool.Count);
            if (runtimeRandomPool.Count > 1 && idx == lastRandomCallIndex)
            {
                idx = (idx + 1) % runtimeRandomPool.Count;
            }
            lastRandomCallIndex = idx;

            var chosen = runtimeRandomPool[idx];
            StartRinging(chosen);
            return true;
        }

        public void StartRinging(PhoneCallDataSO call)
        {
            if (CurrentState != PhoneState.Idle || call == null) return;

            CurrentCall = call;
            CurrentState = PhoneState.Ringing;
            ringTimer = 0f;
            if (!string.IsNullOrEmpty(call.callId))
                deliveredCallIds.Add(call.callId);

            if (phoneAudioSource != null && ringtoneClip != null)
            {
                float ghostVol = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
                phoneAudioSource.volume = Mathf.Clamp01(0.85f * ghostVol);
                phoneAudioSource.clip = ringtoneClip;
                phoneAudioSource.loop = true;
                phoneAudioSource.Play();
            }

            OnPhoneRingingStarted?.Invoke(call);
        }

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

            if (CurrentCall.voiceClip != null && phoneAudioSource != null)
            {
                float ghostVol = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
                phoneAudioSource.PlayOneShot(CurrentCall.voiceClip, ghostVol);
            }

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
            GhostType ghostType = (call.callType == CallType.GhostThreat) ? GhostType.Threat : GhostType.Scream;

            if (GhostManager.Instance != null && !GhostManager.Instance.IsCountdownActive)
            {
                GhostManager.Instance.StartGhostCountdown(ghostType);
            }

            yield return new WaitForSeconds(0.2f);
            EndCall(false);
        }

        public void NextDialogue()
        {
            if (CurrentState != PhoneState.InCall || CurrentCall == null) return;

            if (CurrentCall.dialogueLines == null || CurrentDialogueIndex >= CurrentCall.dialogueLines.Length - 1)
            {
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
            if ((CurrentState != PhoneState.InCall && CurrentState != PhoneState.Ringing) || CurrentCall == null)
                return;

            var call = CurrentCall;
            EndCall(true);

            if (IsGhostCall(call.callType))
            {
                GhostType ghostType = (call.callType == CallType.GhostThreat) ? GhostType.Threat : GhostType.Scream;
                if (GhostManager.Instance != null && !GhostManager.Instance.IsCountdownActive)
                {
                    GhostManager.Instance.StartGhostCountdown(ghostType);
                }
            }
            else
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RecordJobDeclined(call.isScam);
                }
            }
        }

        private void MissedCall()
        {
            var call = CurrentCall;
            EndCall(false);

            if (call != null && IsGhostCall(call.callType))
            {
                GhostType ghostType = (call.callType == CallType.GhostThreat) ? GhostType.Threat : GhostType.Scream;
                if (GhostManager.Instance != null && !GhostManager.Instance.IsCountdownActive)
                {
                    GhostManager.Instance.StartGhostCountdown(ghostType);
                }
            }
            else if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordIgnoredEvent();
            }
        }

        private void EndCall(bool playHangupSound)
        {
            if (phoneAudioSource != null)
            {
                phoneAudioSource.Stop();
                phoneAudioSource.loop = false;
                if (playHangupSound && hangupClip != null)
                {
                    float sfxVol = PlayerPrefs.GetInt("MM_VolSFX", PlayerPrefs.GetInt("MM_VolSfx", 85)) / 100f;
                    phoneAudioSource.PlayOneShot(hangupClip, sfxVol);
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
