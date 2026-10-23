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
                // 1 ชั่วโมงในเกม = config.realSecondsPerHour วินาทีจริง
                // ดังนั้น 1 นาทีในเกม = config.realSecondsPerHour / 60 วินาทีจริง
                secondsPerMinute = Mathf.Max(0.01f, config.realSecondsPerHour / 60f);
            }
            else
            {
                CurrentHour = 0;
                secondsPerMinute = 1f; // ค่าเริ่มต้น 60 วิ ต่อชั่วโมง
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

        private void EndNight()
        {
            IsNightEnded = true;
            OnNightCompleted?.Invoke();
        }

        public string GetTimeString()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }

        public string GetDisplayHour12H()
        {
            int h = CurrentHour == 0 ? 12 : CurrentHour;
            return $"{h} AM";
        }
    }
}
