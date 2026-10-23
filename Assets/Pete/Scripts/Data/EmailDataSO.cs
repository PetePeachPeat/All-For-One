using UnityEngine;

namespace Pete.Data
{
    [CreateAssetMenu(fileName = "NewEmail", menuName = "Pete/Data/Email")]
    public class EmailDataSO : ScriptableObject
    {
        [Header("Email Metadata")]
        public string emailId;
        public string senderName;
        public string senderEmail;
        public string subject;

        [Header("Content")]
        [TextArea(3, 8)]
        public string bodyText;

        [Header("URL / Website")]
        public bool hasUrl;
        public string urlDisplay;
        public Sprite websitePreviewImage;

        [Header("Validation")]
        public bool isScam;

        [Header("Schedule (00:00 - 06:00)")]
        [Range(0, 6)]
        public int targetHour;
        [Range(0, 59)]
        public int targetMinute;
    }
}
