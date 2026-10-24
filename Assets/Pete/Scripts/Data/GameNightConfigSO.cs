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

        [Header("Sanity Settings (0 - 100%)")]
        [Tooltip("ค่า Sanity สูงสุดตอนเริ่มคืน")]
        public float maxSanity = 100f;
        [Tooltip("Sanity ที่ลดลงเมื่อสวดมนต์ไม่ทันหรือเลือกบทสวดผิด (-30)")]
        public float sanityPenaltyGhostFail = 30f;
        [Tooltip("Sanity ที่ลดลงเมื่อกดรับงานมิจฉาชีพ เมล/เว็บ/โทรศัพท์ (-20)")]
        public float sanityPenaltyScamAccepted = 20f;
        [Tooltip("Sanity ที่ลดลงเมื่อปฏิเสธงานจริง หรือปล่อยสายโทรศัพท์ตัด (-10)")]
        public float sanityPenaltyJobMissedOrDeclined = 10f;
        [Tooltip("Sanity ที่ฟื้นฟูเมื่อรับงานจริง หรือปฏิเสธมิจฉาชีพถูก (+10)")]
        public float sanityRewardCorrectAction = 10f;
        [Tooltip("Sanity ที่ฟื้นฟูเมื่อสวดมนต์ไล่ผีถูกและทันเวลา (+15)")]
        public float sanityRewardGhostSurvived = 15f;
        [Tooltip("อัตราการฟื้นฟู Sanity ต่อวินาทีขณะกด Spacebar ค้างหลับตา (~7/s)")]
        public float sanityCloseEyesRestorePerSecond = 7f;

        [Header("Ending Score Thresholds")]
        [Tooltip("คะแนนขั้นต่ำเพื่อให้ได้ Good End")]
        public int minScoreForGoodEnd = 3;
        [Tooltip("จำนวนครั้งที่รับงานมิจฉาชีพ แล้วจะตัดเข้าสู่ Bad End 1 (โดนหลอกหมดตัว)")]
        public int maxScamAcceptedForBadEnd = 2;

        [Header("Good Ending Data")]
        public Sprite goodEndingArt;
        public string goodEndingTitle = "Dawn of Hope (Good Ending)";
        [TextArea(3, 6)]
        public string goodEndingDescription = "Congratulations! You survived the grueling night, filtered out scammers, kept your sanity intact, and successfully landed a stable job with a legitimate company.";

        [Header("Bad Ending 1: Scammed / Sanity Depleted")]
        public Sprite badEndingScamArt;
        public string badEndingScamTitle = "Penniless & Drained (Bad Ending 1)";
        [TextArea(3, 6)]
        public string badEndingScamDescription = "Your sanity shattered under the pressure of the night. Scammers drained your entire bank account and stole all of your personal data...";

        [Header("Bad Ending 2: Unemployed")]
        public Sprite badEndingUnemployedArt;
        public string badEndingUnemployedTitle = "Still Unemployed (Bad Ending 2)";
        [TextArea(3, 6)]
        public string badEndingUnemployedDescription = "Paralyzed by fear and hesitation, you failed to secure enough legitimate job offers. Dawn has arrived, yet you remain unemployed...";
    }
}
