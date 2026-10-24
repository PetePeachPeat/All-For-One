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
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureProceduralGhostAudio();
        }

        private void EnsureProceduralGhostAudio()
        {
            int sr = 22050;
            if (heartbeatLoopClip == null)
            {
                int len = Mathf.RoundToInt(sr * 0.85f);
                float[] data = new float[len];
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / sr;
                    float thump1 = (t < 0.14f) ? Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 22f) : 0f;
                    float thump2 = (t >= 0.22f && t < 0.38f) ? Mathf.Sin(2f * Mathf.PI * 46f * (t - 0.22f)) * Mathf.Exp(-(t - 0.22f) * 25f) * 0.8f : 0f;
                    data[i] = Mathf.Clamp((thump1 + thump2) * 0.75f, -1f, 1f);
                }
                heartbeatLoopClip = AudioClip.Create("ProceduralHeartbeat", len, 1, sr, false);
                heartbeatLoopClip.SetData(data, 0);
            }

            if (holyReliefClip == null)
            {
                int len = Mathf.RoundToInt(sr * 1.1f);
                float[] data = new float[len];
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / sr;
                    float env = Mathf.Exp(-t * 3.2f);
                    float c1 = Mathf.Sin(2f * Mathf.PI * 528f * t);
                    float c2 = Mathf.Sin(2f * Mathf.PI * 792f * t) * 0.5f;
                    data[i] = (c1 + c2) * env * 0.35f;
                }
                holyReliefClip = AudioClip.Create("ProceduralHolyRelief", len, 1, sr, false);
                holyReliefClip.SetData(data, 0);
            }

            if (jumpscareRoarClip == null)
            {
                int len = Mathf.RoundToInt(sr * 0.9f);
                float[] data = new float[len];
                var rng = new System.Random(666);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / sr;
                    float env = Mathf.Exp(-t * 2.8f);
                    float shriek = Mathf.Sin(2f * Mathf.PI * (290f + Mathf.Sin(t * 45f) * 60f) * t);
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.5f;
                    data[i] = Mathf.Clamp((shriek + noise) * env * 0.55f, -1f, 1f);
                }
                jumpscareRoarClip = AudioClip.Create("ProceduralJumpscareRoar", len, 1, sr, false);
                jumpscareRoarClip.SetData(data, 0);
            }
        }

        private void Update()
        {
            if (IsCountdownActive)
            {
                RemainingCountdown -= Time.deltaTime;
                OnCountdownTick?.Invoke(RemainingCountdown, TotalCountdown);

                if (RemainingCountdown <= 0f)
                {
                    TriggerFail();
                }
            }

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

            if (ghostAudioSource != null && heartbeatLoopClip != null)
            {
                float ghostVol = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
                ghostAudioSource.volume = Mathf.Clamp01(0.85f * ghostVol);
                ghostAudioSource.clip = heartbeatLoopClip;
                ghostAudioSource.loop = true;
                ghostAudioSource.Play();
            }

            OnGhostCountdownStarted?.Invoke(TotalCountdown);
        }

        public void OpenChantSelection()
        {
            if (!IsCountdownActive) return;

            OnChantSelectionOpened?.Invoke(availableChants);
        }

        public void SelectChant(GhostChantSO chant)
        {
            if (!IsCountdownActive || chant == null) return;

            if (chant.countersGhostType == CurrentGhostType)
            {
                TriggerSuccess();
            }
            else
            {
                TriggerFail();
            }
        }

        private void TriggerSuccess()
        {
            IsCountdownActive = false;
            StopHeartbeat();

            if (ghostAudioSource != null && holyReliefClip != null)
            {
                float ghostVol = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
                ghostAudioSource.PlayOneShot(holyReliefClip, ghostVol);
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

            if (ghostAudioSource != null && jumpscareRoarClip != null)
            {
                float ghostVol = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
                bool jumpCap = PlayerPrefs.GetInt("MM_JumpCap", 0) == 1;
                if (jumpCap) ghostVol = Mathf.Min(ghostVol, 0.45f);
                ghostAudioSource.PlayOneShot(jumpscareRoarClip, ghostVol);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordGhostResult(false);
            }

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
