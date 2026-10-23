using System;
using System.Collections.Generic;
using UnityEngine;
using Pete.Data;

namespace Pete.Systems
{
    public class GhostManager : MonoBehaviour
    {
        public static GhostManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameNightConfigSO config;
        [SerializeField] private List<GhostChantSO> availableChants = new List<GhostChantSO>();

        [Header("Audio")]
        [SerializeField] private AudioSource ghostAudioSource;
        [SerializeField] private AudioClip heartbeatLoopClip;
        [SerializeField] private AudioClip jumpscareRoarClip;
        [SerializeField] private AudioClip holyReliefClip;

        // Runtime states
        public bool IsCountdownActive { get; private set; } = false;
        public GhostType CurrentGhostType { get; private set; } = GhostType.None;
        public float RemainingCountdown { get; private set; } = 0f;
        public float TotalCountdown { get; private set; } = 7f;

        // Debuff states
        public GhostType ActiveDebuffType { get; private set; } = GhostType.None;
        public bool IsDebuffActive => ActiveDebuffType != GhostType.None;
        private float debuffTimer = 0f;

        public event Action<float> OnGhostCountdownStarted; // totalTime
        public event Action<float, float> OnCountdownTick;   // remaining, total
        public event Action<List<GhostChantSO>> OnChantSelectionOpened;
        public event Action OnGhostAverted;
        public event Action<GhostType, float> OnGhostDebuffApplied; // type, duration
        public event Action OnGhostDebuffEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Update()
        {
            // อัปเดตนับถอยหลังของผี
            if (IsCountdownActive)
            {
                RemainingCountdown -= Time.deltaTime;
                OnCountdownTick?.Invoke(RemainingCountdown, TotalCountdown);

                if (RemainingCountdown <= 0f)
                {
                    // หมดเวลา สวดไม่ทัน โดนหลอก!
                    TriggerFail();
                }
            }

            // อัปเดตเวลานับถอยหลังของดีบัฟ
            if (IsDebuffActive)
            {
                debuffTimer -= Time.deltaTime;
                if (debuffTimer <= 0f)
                {
                    EndDebuff();
                }
            }
        }

        public void StartGhostCountdown(GhostType ghostType)
        {
            CurrentGhostType = ghostType;
            TotalCountdown = (config != null) ? config.ghostCountdownSeconds : 7f;
            RemainingCountdown = TotalCountdown;
            IsCountdownActive = true;

            // เริ่มเล่นเสียงหัวใจเต้น
            if (ghostAudioSource != null && heartbeatLoopClip != null)
            {
                ghostAudioSource.clip = heartbeatLoopClip;
                ghostAudioSource.loop = true;
                ghostAudioSource.Play();
            }

            OnGhostCountdownStarted?.Invoke(TotalCountdown);
        }

        /// <summary>
        /// เมื่อผู้เล่นคลิกที่ศาลเจ้า จะเปิดเมนูบทสวดมนต์
        /// </summary>
        public void OpenChantSelection()
        {
            if (!IsCountdownActive) return;

            OnChantSelectionOpened?.Invoke(availableChants);
        }

        /// <summary>
        /// ผู้เล่นคลิกเลือกบทสวดมนต์
        /// </summary>
        public void SelectChant(GhostChantSO chant)
        {
            if (!IsCountdownActive || chant == null) return;

            if (chant.countersGhostType == CurrentGhostType)
            {
                // สวดถูกประเภท! รอดปลอดภัย
                TriggerSuccess();
            }
            else
            {
                // สวดผิดบท! โดนหลอกทันที
                TriggerFail();
            }
        }

        private void TriggerSuccess()
        {
            IsCountdownActive = false;
            StopHeartbeat();

            if (ghostAudioSource != null && holyReliefClip != null)
            {
                ghostAudioSource.PlayOneShot(holyReliefClip);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordGhostResult(true);
            }

            OnGhostAverted?.Invoke();
        }

        private void TriggerFail()
        {
            IsCountdownActive = false;
            StopHeartbeat();

            // เล่นเสียง Jump-scare คำราม
            if (ghostAudioSource != null && jumpscareRoarClip != null)
            {
                ghostAudioSource.PlayOneShot(jumpscareRoarClip);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordGhostResult(false);
            }

            // ติดดีบัฟตามประเภทของผี
            float duration = (config != null) ? config.debuffDurationSeconds : 35f;
            ApplyDebuff(CurrentGhostType, duration);
        }

        private void ApplyDebuff(GhostType type, float duration)
        {
            ActiveDebuffType = type;
            debuffTimer = duration;

            OnGhostDebuffApplied?.Invoke(type, duration);
        }

        private void EndDebuff()
        {
            ActiveDebuffType = GhostType.None;
            debuffTimer = 0f;
            OnGhostDebuffEnded?.Invoke();
        }

        private void StopHeartbeat()
        {
            if (ghostAudioSource != null)
            {
                ghostAudioSource.Stop();
                ghostAudioSource.loop = false;
            }
        }

        public void SetAvailableChants(List<GhostChantSO> chants)
        {
            availableChants = chants;
        }
    }
}
