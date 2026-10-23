using UnityEngine;

namespace Pete.Data
{
    [CreateAssetMenu(fileName = "NewPhoneCall", menuName = "Pete/Data/Phone Call")]
    public class PhoneCallDataSO : ScriptableObject
    {
        [Header("Caller Info")]
        public string callId;
        public string callerName = "Unknown Caller";
        public string callerNumber = "08X-XXX-XXXX";
        public CallType callType = CallType.JobOffer;

        [Header("Conversation")]
        [TextArea(2, 5)]
        public string[] dialogueLines;
        public AudioClip voiceClip;

        [Header("Validation")]
        [Tooltip("เป็นมิจฉาชีพหรือไม่")]
        public bool isScam;

        [Header("Schedule (00:00 - 06:00)")]
        [Range(0, 6)]
        public int targetHour;
        [Range(0, 59)]
        public int targetMinute;
    }
}
