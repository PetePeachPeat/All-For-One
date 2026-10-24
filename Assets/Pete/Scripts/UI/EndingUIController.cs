using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Pete.Data;
using Pete.Systems;

namespace Pete.UI
{
    public class EndingUIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameNightConfigSO config;

        [Header("Ending Canvas Panel")]
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Ending Presentation")]
        [SerializeField] private Image endingArtworkImage;
        [SerializeField] private TextMeshProUGUI endingTitleText;
        [SerializeField] private TextMeshProUGUI endingDescriptionText;

        [Header("Statistics Summary")]
        [SerializeField] private TextMeshProUGUI statsSummaryText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnNightCompleted += ShowEnding;
            }

            if (endingPanel != null) endingPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnNightCompleted -= ShowEnding;
            }
        }

        public void ShowEnding()
        {
            if (endingPanel != null) endingPanel.SetActive(true);

            EndingType ending = (ScoreManager.Instance != null) ? ScoreManager.Instance.EvaluateEnding() : EndingType.BadEndUnemployed;

            Sprite art = null;
            string title = "";
            string desc = "";

            if (config != null)
            {
                switch (ending)
                {
                    case EndingType.GoodEnd:
                        art = config.goodEndingArt;
                        title = config.goodEndingTitle;
                        desc = config.goodEndingDescription;
                        break;
                    case EndingType.BadEndScammed:
                        art = config.badEndingScamArt;
                        title = config.badEndingScamTitle;
                        desc = config.badEndingScamDescription;
                        break;
                    case EndingType.BadEndUnemployed:
                        art = config.badEndingUnemployedArt;
                        title = config.badEndingUnemployedTitle;
                        desc = config.badEndingUnemployedDescription;
                        break;
                }
            }

            if (endingArtworkImage != null)
            {
                endingArtworkImage.sprite = art;
                endingArtworkImage.enabled = (art != null);
            }
            if (endingTitleText != null) endingTitleText.text = title;
            if (endingDescriptionText != null) endingDescriptionText.text = desc;

            if (statsSummaryText != null && ScoreManager.Instance != null)
            {
                var sm = ScoreManager.Instance;
                int finalSanity = SanityManager.Instance != null
                    ? Mathf.RoundToInt(SanityManager.Instance.CurrentSanity)
                    : 100;

                statsSummaryText.text =
                    $"Final Sanity: {finalSanity}%\n" +
                    $"Total Score: {sm.NetScore}\n" +
                    $"Legit Jobs Accepted: {sm.LegitJobsAccepted}\n" +
                    $"Scams Rejected: {sm.ScamsRejected}\n" +
                    $"Scams Accepted: {sm.ScamsAccepted}\n" +
                    $"Missed / Ignored: {sm.MissedIgnoredEvents}\n" +
                    $"Paranormal Survived: {sm.GhostSurvivedCount} times";
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main menu");
        }
    }
}
