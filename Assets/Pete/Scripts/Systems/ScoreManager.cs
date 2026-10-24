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
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnScamAccepted();
            }
            else
            {
                LegitJobsAccepted++;
                NetScore += 1; // รับงานจริง ได้ 1 แต้ม
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnCorrectJobDecision();
            }

            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordJobDeclined(bool isScam)
        {
            if (isScam)
            {
                ScamsRejected++;
                NetScore += 1; // ปฏิเสธมิจฉาชีพถูกต้อง ได้ 1 แต้ม
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnCorrectJobDecision();
            }
            else
            {
                NetScore -= 1; // ปฏิเสธงานจริง เสีย 1 แต้ม
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnJobMissedOrDeclined();
            }

            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordIgnoredEvent()
        {
            MissedIgnoredEvents++;
            NetScore -= 1; // ละเลย/ไม่รับสาย เสีย 1 แต้ม
            if (SanityManager.Instance != null)
                SanityManager.Instance.OnJobMissedOrDeclined();

            OnScoreChanged?.Invoke(NetScore);
        }

        public void RecordGhostResult(bool survived)
        {
            if (survived)
            {
                GhostSurvivedCount++;
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnGhostSurvived();
            }
            else
            {
                GhostFailedCount++;
                if (SanityManager.Instance != null)
                    SanityManager.Instance.OnGhostFailed();
            }
        }

        public EndingType EvaluateEnding()
        {
            // 1. หาก Sanity เหลือ 0 ในระหว่างคืน -> จบเกมทันที เข้าสู่ฉากจบโดนหลอกเอาเงินและข้อมูล (Bad End 1)
            if (SanityManager.Instance != null &&
                (SanityManager.Instance.IsSanityDepleted || SanityManager.Instance.CurrentSanity <= 0.01f))
            {
                return EndingType.BadEndScammed;
            }

            int minGoodScore = (config != null) ? config.minScoreForGoodEnd : 3;

            // 2. รอดจนถึง 06:00 AM โดย Sanity > 0 และทำคะแนนงานที่ถูกได้ตามเกณฑ์ -> ได้งานทำ (Good End)
            if (NetScore >= minGoodScore && LegitJobsAccepted > 0)
            {
                return EndingType.GoodEnd;
            }

            // 3. รอดถึงเช้าแต่ผลงานไม่ถึงเกณฑ์ -> คนไร้งาน หางานไม่ได้ (Bad End 2)
            return EndingType.BadEndUnemployed;
        }
    }
}
