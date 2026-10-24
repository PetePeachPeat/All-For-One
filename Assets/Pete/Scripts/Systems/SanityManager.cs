using System;
using UnityEngine;
using Pete.Data;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pete.Systems
{
    public class SanityManager : MonoBehaviour
    {
        public static SanityManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameNightConfigSO config;

        [Header("Audio (Breathing while closing eyes)")]
        [SerializeField] private AudioSource breathingAudioSource;
        [SerializeField] private AudioClip breathingLoopClip;

        [Header("Audio (Tension, Panic & Collapse)")]
        [SerializeField] private AudioSource tensionAudioSource;
        [SerializeField] private AudioClip lowSanityHeartbeatClip;
        [SerializeField] private AudioClip lowSanityPanicBreathingClip;
        [SerializeField] private AudioClip sanityZeroCollapseClip;

        public float MaxSanity { get; private set; } = 100f;
        public float CurrentSanity { get; private set; } = 100f;
        public float SanityNormalized => MaxSanity > 0f ? Mathf.Clamp01(CurrentSanity / MaxSanity) : 0f;

        /// <summary>
        /// ระดับความมืดขอบจอ 0..10 (เพิ่มขึ้นทุกๆ 10% ที่ Sanity ลดลง)
        /// </summary>
        public int DarknessStep => Mathf.Clamp(10 - Mathf.FloorToInt((CurrentSanity / Mathf.Max(1f, MaxSanity)) * 10f), 0, 10);

        public bool IsClosingEyes { get; private set; } = false;
        public bool IsSanityDepleted { get; private set; } = false;

        // Events: (currentSanity, maxSanity)
        public event Action<float, float> OnSanityChanged;
        // Events: (delta, reasonLabel)
        public event Action<float, string> OnSanityDeltaEvent;
        // Events: (isClosingEyes)
        public event Action<bool> OnEyesClosedChanged;
        public event Action OnSanityDepleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            Initialize();
        }

        public void Initialize()
        {
            MaxSanity = (config != null && config.maxSanity > 0f) ? config.maxSanity : 100f;
            CurrentSanity = MaxSanity;
            IsClosingEyes = false;
            IsSanityDepleted = false;

            EnsureBreathingAudioReady();
            OnSanityChanged?.Invoke(CurrentSanity, MaxSanity);
        }

        private void EnsureBreathingAudioReady()
        {
            if (breathingAudioSource == null)
            {
                breathingAudioSource = gameObject.AddComponent<AudioSource>();
                breathingAudioSource.playOnAwake = false;
                breathingAudioSource.loop = true;
                breathingAudioSource.spatialBlend = 0f;
                breathingAudioSource.volume = 0.35f;
            }

            if (breathingLoopClip == null)
            {
                breathingLoopClip = GenerateProceduralBreathingClip();
            }

            breathingAudioSource.clip = breathingLoopClip;
            breathingAudioSource.loop = true;

            if (tensionAudioSource == null)
            {
                tensionAudioSource = gameObject.AddComponent<AudioSource>();
                tensionAudioSource.playOnAwake = false;
                tensionAudioSource.loop = true;
                tensionAudioSource.spatialBlend = 0f;
                tensionAudioSource.volume = 0.4f;
            }
        }

        private static AudioClip GenerateProceduralBreathingClip()
        {
            int sampleRate = 22050;
            float duration = 3.6f; // 1.8s inhale, 1.8s exhale
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            var rng = new System.Random(101);
            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float cyclePos = (t % duration) / duration; // 0..1
                // Smooth inhale (0..0.45) and exhale (0.50..0.95) envelope
                float env = 0f;
                if (cyclePos < 0.45f)
                    env = Mathf.Sin((cyclePos / 0.45f) * Mathf.PI);
                else if (cyclePos >= 0.50f && cyclePos < 0.95f)
                    env = Mathf.Sin(((cyclePos - 0.50f) / 0.45f) * Mathf.PI) * 0.85f;

                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Low-pass filter for warm soft breath sound
                filterState = Mathf.Lerp(filterState, white, 0.045f);
                samples[i] = filterState * env * 0.38f;
            }

            AudioClip clip = AudioClip.Create("ProceduralSlowBreathing", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Update()
        {
            if (IsSanityDepleted) return;
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsNightEnded)
            {
                if (IsClosingEyes) SetEyesClosed(false);
                return;
            }

            bool holdingSpace = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                holdingSpace = true;
#else
            if (Input.GetKey(KeyCode.Space))
                holdingSpace = true;
#endif

            if (holdingSpace != IsClosingEyes)
            {
                SetEyesClosed(holdingSpace);
            }

            if (IsClosingEyes)
            {
                float restoreRate = (config != null) ? config.sanityCloseEyesRestorePerSecond : 7f;
                if (CurrentSanity < MaxSanity)
                {
                    ModifySanity(restoreRate * Time.deltaTime, null);
                }
            }

            UpdateTensionAudio();
        }

        private void UpdateTensionAudio()
        {
            if (tensionAudioSource == null || IsSanityDepleted)
            {
                if (tensionAudioSource != null && tensionAudioSource.isPlaying)
                    tensionAudioSource.Stop();
                return;
            }

            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsNightEnded)
            {
                if (tensionAudioSource.isPlaying) tensionAudioSource.Stop();
                return;
            }

            float sfxVol = PlayerPrefs.GetInt("MM_VolSfx", 85) / 100f;

            // When closing eyes, fade out tension audio so user hears calm breathing
            if (IsClosingEyes)
            {
                if (tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.volume = Mathf.MoveTowards(tensionAudioSource.volume, 0f, Time.deltaTime * 2f);
                    if (tensionAudioSource.volume <= 0.01f) tensionAudioSource.Stop();
                }
                return;
            }

            // Severe low sanity (<= 20%): Panic heavy breathing or fast heartbeat
            if (CurrentSanity <= 20f && lowSanityPanicBreathingClip != null)
            {
                if (tensionAudioSource.clip != lowSanityPanicBreathingClip || !tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.clip = lowSanityPanicBreathingClip;
                    tensionAudioSource.loop = true;
                    tensionAudioSource.Play();
                }
                tensionAudioSource.volume = Mathf.Clamp01(0.6f * sfxVol);
            }
            // Warning low sanity (<= 35%): Low sanity heartbeat
            else if (CurrentSanity <= 35f && lowSanityHeartbeatClip != null)
            {
                if (tensionAudioSource.clip != lowSanityHeartbeatClip || !tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.clip = lowSanityHeartbeatClip;
                    tensionAudioSource.loop = true;
                    tensionAudioSource.Play();
                }
                float ratio = 1f - (CurrentSanity / 35f);
                tensionAudioSource.volume = Mathf.Clamp01(Mathf.Lerp(0.3f, 0.7f, ratio) * sfxVol);
            }
            else
            {
                if (tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.volume = Mathf.MoveTowards(tensionAudioSource.volume, 0f, Time.deltaTime * 2f);
                    if (tensionAudioSource.volume <= 0.01f)
                    {
                        tensionAudioSource.Stop();
                    }
                }
            }
        }

        private void SetEyesClosed(bool closed)
        {
            if (IsClosingEyes == closed) return;
            IsClosingEyes = closed;

            if (breathingAudioSource != null)
            {
                float sfxVol = PlayerPrefs.GetInt("MM_VolSfx", 85) / 100f;
                breathingAudioSource.volume = 0.42f * sfxVol;
                if (closed)
                {
                    if (!breathingAudioSource.isPlaying)
                        breathingAudioSource.Play();
                }
                else
                {
                    if (breathingAudioSource.isPlaying)
                        breathingAudioSource.Stop();
                }
            }

            OnEyesClosedChanged?.Invoke(IsClosingEyes);
        }

        public void OnGhostFailed()
        {
            float pen = (config != null) ? config.sanityPenaltyGhostFail : 30f;
            ModifySanity(-pen, $"-{pen:0} SANITY (GHOST CURSE)");
        }

        public void OnGhostSurvived()
        {
            float rew = (config != null) ? config.sanityRewardGhostSurvived : 15f;
            ModifySanity(rew, $"+{rew:0} SANITY (SHRINE WARD)");
        }

        public void OnScamAccepted()
        {
            float pen = (config != null) ? config.sanityPenaltyScamAccepted : 20f;
            ModifySanity(-pen, $"-{pen:0} SANITY (SCAM BREACH)");
        }

        public void OnJobMissedOrDeclined()
        {
            float pen = (config != null) ? config.sanityPenaltyJobMissedOrDeclined : 10f;
            ModifySanity(-pen, $"-{pen:0} SANITY (MISSED JOB)");
        }

        public void OnCorrectJobDecision()
        {
            float rew = (config != null) ? config.sanityRewardCorrectAction : 10f;
            ModifySanity(rew, $"+{rew:0} SANITY (GOOD DECISION)");
        }

        public void ModifySanity(float delta, string eventLabel)
        {
            if (IsSanityDepleted) return;

            float prev = CurrentSanity;
            CurrentSanity = Mathf.Clamp(CurrentSanity + delta, 0f, MaxSanity);

            if (!string.IsNullOrEmpty(eventLabel) && Mathf.Abs(delta) >= 0.5f)
            {
                OnSanityDeltaEvent?.Invoke(delta, eventLabel);
            }

            if (Mathf.Abs(CurrentSanity - prev) > 0.0001f || CurrentSanity <= 0f)
            {
                OnSanityChanged?.Invoke(CurrentSanity, MaxSanity);
            }

            if (CurrentSanity <= 0f && !IsSanityDepleted)
            {
                TriggerSanityZeroBreakdown();
            }
        }

        private void TriggerSanityZeroBreakdown()
        {
            CurrentSanity = 0f;
            IsSanityDepleted = true;
            if (IsClosingEyes)
                SetEyesClosed(false);

            if (tensionAudioSource != null && tensionAudioSource.isPlaying)
                tensionAudioSource.Stop();

            if (sanityZeroCollapseClip != null)
            {
                float sfxVol = PlayerPrefs.GetInt("MM_VolSfx", 85) / 100f;
                if (tensionAudioSource != null)
                {
                    tensionAudioSource.loop = false;
                    tensionAudioSource.volume = sfxVol;
                    tensionAudioSource.PlayOneShot(sanityZeroCollapseClip, sfxVol);
                }
            }

            OnSanityChanged?.Invoke(0f, MaxSanity);
            OnSanityDepleted?.Invoke();

            // จบเกมทันที เข้าสู่ฉากจบที่ 2 (โดนหลอกดูดเงินและข้อมูลจนหมดตัว)
            if (GameTimeManager.Instance != null && !GameTimeManager.Instance.IsNightEnded)
            {
                GameTimeManager.Instance.ForceEndNight();
            }
        }
    }
}
