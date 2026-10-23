using UnityEngine;
using Pete.Data;
using Pete.Systems;

namespace Pete.Core
{
    public class NightGameManager : MonoBehaviour
    {
        public static NightGameManager Instance { get; private set; }

        [Header("Global Night Configuration")]
        [SerializeField] private GameNightConfigSO nightConfig;

        [Header("System References")]
        [SerializeField] private GameTimeManager timeManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private EmailManager emailManager;
        [SerializeField] private PhoneManager phoneManager;
        [SerializeField] private GhostManager ghostManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            // ค้นหาระบบอัตโนมัติหากไม่ได้ลากใส่ใน Inspector
            if (timeManager == null) timeManager = GetComponentInChildren<GameTimeManager>();
            if (scoreManager == null) scoreManager = GetComponentInChildren<ScoreManager>();
            if (emailManager == null) emailManager = GetComponentInChildren<EmailManager>();
            if (phoneManager == null) phoneManager = GetComponentInChildren<PhoneManager>();
            if (ghostManager == null) ghostManager = GetComponentInChildren<GhostManager>();
        }

        public GameNightConfigSO NightConfig => nightConfig;
        public GameTimeManager TimeManager => timeManager;
        public ScoreManager ScoreManager => scoreManager;
        public EmailManager EmailManager => emailManager;
        public PhoneManager PhoneManager => phoneManager;
        public GhostManager GhostManager => ghostManager;
    }
}
