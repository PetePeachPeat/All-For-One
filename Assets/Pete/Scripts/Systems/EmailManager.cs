using System;
using System.Collections.Generic;
using UnityEngine;
using Pete.Data;

namespace Pete.Systems
{
    public class EmailManager : MonoBehaviour
    {
        public static EmailManager Instance { get; private set; }

        [Header("Email Database")]
        [SerializeField] private List<EmailDataSO> scheduledEmails = new List<EmailDataSO>();

        [Header("Audio")]
        [SerializeField] private AudioSource notificationAudioSource;
        [SerializeField] private AudioClip emailPingClip;

        // Runtime storage
        public List<EmailDataSO> DeliveredEmails { get; private set; } = new List<EmailDataSO>();
        public HashSet<string> ReadEmailIds { get; private set; } = new HashSet<string>();
        public HashSet<string> ProcessedEmailIds { get; private set; } = new HashSet<string>();

        private HashSet<string> deliveredIds = new HashSet<string>();

        public event Action<EmailDataSO> OnEmailReceived;
        public event Action OnEmailStatusUpdated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged += CheckScheduledEmails;
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeChanged -= CheckScheduledEmails;
            }
        }

        private void CheckScheduledEmails(int hour, int minute)
        {
            for (int i = 0; i < scheduledEmails.Count; i++)
            {
                var email = scheduledEmails[i];
                if (email == null) continue;

                if (!deliveredIds.Contains(email.emailId) &&
                    email.targetHour == hour && email.targetMinute == minute)
                {
                    DeliverEmail(email);
                }
            }
        }

        public void DeliverEmail(EmailDataSO email)
        {
            if (deliveredIds.Contains(email.emailId)) return;

            deliveredIds.Add(email.emailId);
            DeliveredEmails.Add(email);

            // เล่นเสียงแจ้งเตือน
            if (notificationAudioSource != null && emailPingClip != null)
            {
                float sfxVol = PlayerPrefs.GetInt("MM_VolSfx", 85) / 100f;
                notificationAudioSource.PlayOneShot(emailPingClip, Mathf.Clamp01(0.85f * sfxVol));
            }

            OnEmailReceived?.Invoke(email);
            OnEmailStatusUpdated?.Invoke();
        }

        public void MarkAsRead(EmailDataSO email)
        {
            if (email == null) return;
            if (ReadEmailIds.Add(email.emailId))
            {
                OnEmailStatusUpdated?.Invoke();
            }
        }

        public void AcceptEmail(EmailDataSO email)
        {
            if (email == null || ProcessedEmailIds.Contains(email.emailId)) return;

            ProcessedEmailIds.Add(email.emailId);
            MarkAsRead(email);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordJobAccepted(email.isScam);
            }

            OnEmailStatusUpdated?.Invoke();
        }

        public void DeclineEmail(EmailDataSO email)
        {
            if (email == null || ProcessedEmailIds.Contains(email.emailId)) return;

            ProcessedEmailIds.Add(email.emailId);
            MarkAsRead(email);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecordJobDeclined(email.isScam);
            }

            OnEmailStatusUpdated?.Invoke();
        }

        public int GetUnreadCount(bool forWebsitesOnly)
        {
            int count = 0;
            foreach (var email in DeliveredEmails)
            {
                if (ProcessedEmailIds.Contains(email.emailId)) continue;
                if (ReadEmailIds.Contains(email.emailId)) continue;

                if (forWebsitesOnly && email.hasUrl) count++;
                else if (!forWebsitesOnly && !email.hasUrl) count++;
            }
            return count;
        }

        public List<EmailDataSO> GetEmails(bool forWebsitesOnly)
        {
            var list = new List<EmailDataSO>();
            foreach (var email in DeliveredEmails)
            {
                if (forWebsitesOnly && email.hasUrl) list.Add(email);
                else if (!forWebsitesOnly && !email.hasUrl) list.Add(email);
            }
            return list;
        }

        public void SetScheduledEmails(List<EmailDataSO> emails)
        {
            scheduledEmails = emails;
        }
    }
}
