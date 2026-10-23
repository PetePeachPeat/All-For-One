using System;
using UnityEngine;
using Pete.Data;

namespace Pete.Systems
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameNightConfigSO config;

        // Statistics
        public int NetScore { get; private set; } = 0;
        public int LegitJobsAccepted { get; private set; } = 0;
        public int ScamsAccepted { get; private set; } = 0;
        public int ScamsRejected { get; private set; } = 0;
        public int MissedIgnoredEvents { get; private set; } = 0;
        public int GhostSurvivedCount { get; private set; } = 0;
        public int GhostFailedCount { get; private set; } = 0;

        public event Action<int> OnScoreChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        public void RecordJobAccepted(bool isScam)
        {
            if (isScam)
            {
                ScamsAccepted++;
                NetScore -= 2; // หลงเชื่อกลโกง เสีย 2 แต้ม
            }
            else
            {
                LegitJobsAccepted++;
                NetScore += 1; // รับงานจริง ได้ 1 แต้ม
            }

            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordJobDeclined(bool isScam)
        {
            if (isScam)
            {
                ScamsRejected++;
                NetScore += 1; // ปฏิเสธมิจฉาชีพถูกต้อง ได้ 1 แต้ม
            }
            else
            {
                NetScore -= 1; // ปฏิเสธงานจริง เสีย 1 แต้ม
            }

            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordIgnoredEvent()
        {
            MissedIgnoredEvents++;
            NetScore -= 1; // ละเลย/ไม่รับสาย เสีย 1 แต้ม
            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordGhostResult(bool survived)
        {
            if (survived) GhostSurvivedCount++;
            else GhostFailedCount++;
        }

        public EndingType EvaluateEnding()
        {
            int maxScamThreshold = (config != null) ? config.maxScamAcceptedForBadEnd : 2;
            int minGoodScore = (config != null) ? config.minScoreForGoodEnd : 3;

            // 1. ถ้าหลงเชื่อรับงานมิจฉาชีพสะสมถึงเกณฑ์ -> โดนหลอกหมดตัว (Bad End 1)
            if (ScamsAccepted >= maxScamThreshold)
            {
                return EndingType.BadEndScammed;
            }

            // 2. ถ้าคะแนนสุทธิถึงเกณฑ์ และมีงานจริงที่รับไว้อย่างน้อย 1 งาน -> ได้งานทำ (Good End)
            if (NetScore >= minGoodScore && LegitJobsAccepted > 0)
            {
                return EndingType.GoodEnd;
            }

            // 3. ปฏิเสธ/เมินจนไม่มีงานทำ หรือคะแนนไม่พอ -> ตกงาน (Bad End 2)
            return EndingType.BadEndUnemployed;
        }
    }
}
