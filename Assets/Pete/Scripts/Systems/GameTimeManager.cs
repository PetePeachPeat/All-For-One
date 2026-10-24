using System;
using UnityEngine;
using Pete.Data;

namespace Pete.Systems
{
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameNightConfigSO config;

        // Current in-game clock
        public int CurrentHour { get; private set; } = 0;
        public int CurrentMinute { get; private set; } = 0;
        public bool IsNightEnded { get; private set; } = false;
        public bool IsNightActive => !IsNightEnded;

        private float minuteTimer = 0f;
        private float secondsPerMinute = 1f;

        public event Action<int, int> OnTimeChanged; // hour, minute
        public event Action<int> OnHourChanged;       // hour
        public event Action OnNightCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            Initialize();
        }

        public void Initialize()
        {
            if (config != null)
            {
                CurrentHour = config.startHour;
                secondsPerMinute = Mathf.Max(0.01f, config.realSecondsPerHour / 60f);
            }
            else
            {
                CurrentHour = 0;
                secondsPerMinute = 1f;
            }

            CurrentMinute = 0;
            minuteTimer = 0f;
            IsNightEnded = false;
        }

        private void Update()
        {
            if (IsNightEnded) return;

            minuteTimer += Time.deltaTime;
            if (minuteTimer >= secondsPerMinute)
            {
                minuteTimer -= secondsPerMinute;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            CurrentMinute++;
            if (CurrentMinute >= 60)
            {
                CurrentMinute = 0;
                CurrentHour++;

                OnHourChanged?.Invoke(CurrentHour);

                int endH = (config != null) ? config.endHour : 6;
                if (CurrentHour >= endH)
                {
                    EndNight();
                    return;
                }
            }

            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        public void ForceEndNight()
        {
            if (IsNightEnded) return;
            EndNight();
        }

        private void EndNight()
        {
            IsNightEnded = true;
            OnNightCompleted?.Invoke();
        }

        public string GetTimeString()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }

        public string GetDisplayTime24H()
        {
            return GetTimeString();
        }

        public string GetDisplayHour12H()
        {
            int h = CurrentHour == 0 ? 12 : CurrentHour;
            return $"{h} AM";
        }
    }
}
