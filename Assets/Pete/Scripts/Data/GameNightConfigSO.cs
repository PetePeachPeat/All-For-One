using UnityEngine;

namespace Pete.Data
{
    [CreateAssetMenu(fileName = "GameNightConfig", menuName = "Pete/Data/Game Night Config")]
    public class GameNightConfigSO : ScriptableObject
    {
        [Header("Time Settings")]
        [Tooltip("จำนวนวินาทีจริงต่อ 1 ชั่วโมงในเกม (เช่น 60 วินาที = 1 ชั่วโมง)")]
        public float realSecondsPerHour = 60f;
        public int startHour = 0;
        public int endHour = 6;

        [Header("Ghost & Debuff Settings")]
        [Tooltip("เวลานับถอยหลังในการสวดมนต์หน้าศาลเจ้า")]
        public float ghostCountdownSeconds = 7f;
        [Tooltip("ระยะเวลาที่ติดสถานะดีบัฟ (วินาที)")]
        public float debuffDurationSeconds = 35f;

        [Header("Ending Score Thresholds")]
        [Tooltip("คะแนนขั้นต่ำเพื่อให้ได้ Good End")]
        public int minScoreForGoodEnd = 3;
        [Tooltip("จำนวนครั้งที่รับงานมิจฉาชีพ แล้วจะตัดเข้าสู่ Bad End 1 (โดนหลอกหมดตัว)")]
        public int maxScamAcceptedForBadEnd = 2;

        [Header("Good Ending Data")]
        public Sprite goodEndingArt;
        public string goodEndingTitle = "Dawn of Hope (Good Ending)";
        [TextArea(3, 6)]
        public string goodEndingDescription = "Congratulations! You survived the grueling night, filtered out scammers, and successfully landed a stable job with a legitimate company.";

        [Header("Bad Ending 1: Scammed")]
        public Sprite badEndingScamArt;
        public string badEndingScamTitle = "Penniless (Bad Ending 1)";
        [TextArea(3, 6)]
        public string badEndingScamDescription = "You fell for deceptive job offers. Your entire savings have been drained by scammers, and your personal data was sold off...";

        [Header("Bad Ending 2: Unemployed")]
        public Sprite badEndingUnemployedArt;
        public string badEndingUnemployedTitle = "Still Unemployed (Bad Ending 2)";
        [TextArea(3, 6)]
        public string badEndingUnemployedDescription = "Paralyzed by paranoia, you declined and ignored every offer. The night is over, yet you remain trapped in this room without a job...";
    }
}
