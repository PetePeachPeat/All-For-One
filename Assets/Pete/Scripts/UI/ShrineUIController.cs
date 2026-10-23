using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pete.Data;
using Pete.Systems;

namespace Pete.UI
{
    public class ShrineUIController : MonoBehaviour
    {
        [Header("Countdown HUD")]
        [SerializeField] private GameObject countdownHudPanel;
        [SerializeField] private Slider countdownProgressBar;
        [SerializeField] private TextMeshProUGUI countdownSecondsText;

        [Header("Chant Selection Popup")]
        [SerializeField] private GameObject chantPopupPanel;

        [Header("Chant Button A")]
        [SerializeField] private Button chantButtonA;
        [SerializeField] private TextMeshProUGUI chantNameTextA;
        [SerializeField] private TextMeshProUGUI chantDescTextA;

        [Header("Chant Button B")]
        [SerializeField] private Button chantButtonB;
        [SerializeField] private TextMeshProUGUI chantNameTextB;
        [SerializeField] private TextMeshProUGUI chantDescTextB;

        private List<GhostChantSO> activeChants;

        private void Start()
        {
            if (chantButtonA != null)
                chantButtonA.onClick.AddListener(() => OnChantSelected(0));
            if (chantButtonB != null)
                chantButtonB.onClick.AddListener(() => OnChantSelected(1));

            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostCountdownStarted += OnCountdownStarted;
                GhostManager.Instance.OnCountdownTick += OnCountdownTick;
                GhostManager.Instance.OnChantSelectionOpened += OnChantSelectionOpened;
                GhostManager.Instance.OnGhostAverted += OnGhostResolved;
                GhostManager.Instance.OnGhostDebuffApplied += (t, d) => OnGhostResolved();
            }

            HideAll();
        }

        private void OnDestroy()
        {
            if (GhostManager.Instance != null)
            {
                GhostManager.Instance.OnGhostCountdownStarted -= OnCountdownStarted;
                GhostManager.Instance.OnCountdownTick -= OnCountdownTick;
                GhostManager.Instance.OnChantSelectionOpened -= OnChantSelectionOpened;
                GhostManager.Instance.OnGhostAverted -= OnGhostResolved;
            }
        }

        private void OnCountdownStarted(float totalTime)
        {
            if (countdownHudPanel != null) countdownHudPanel.SetActive(true);
            if (countdownProgressBar != null) countdownProgressBar.value = 1f;
            if (countdownSecondsText != null) countdownSecondsText.text = $"{Mathf.CeilToInt(totalTime)}s";
        }

        private void OnCountdownTick(float remaining, float total)
        {
            if (countdownProgressBar != null && total > 0f)
            {
                countdownProgressBar.value = Mathf.Clamp01(remaining / total);
            }
            if (countdownSecondsText != null)
            {
                countdownSecondsText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, remaining))}s";
            }
        }

        private void OnChantSelectionOpened(List<GhostChantSO> chants)
        {
            activeChants = chants;
            if (chantPopupPanel != null) chantPopupPanel.SetActive(true);

            if (chants != null && chants.Count >= 2)
            {
                // บทสวดที่ 1
                if (chantNameTextA != null) chantNameTextA.text = chants[0].chantName;
                if (chantDescTextA != null) chantDescTextA.text = chants[0].chantDescription;

                // บทสวดที่ 2
                if (chantNameTextB != null) chantNameTextB.text = chants[1].chantName;
                if (chantDescTextB != null) chantDescTextB.text = chants[1].chantDescription;
            }
        }

        private void OnChantSelected(int index)
        {
            if (activeChants != null && index >= 0 && index < activeChants.Count)
            {
                if (GhostManager.Instance != null)
                {
                    GhostManager.Instance.SelectChant(activeChants[index]);
                }
            }

            if (chantPopupPanel != null) chantPopupPanel.SetActive(false);
        }

        private void OnGhostResolved()
        {
            HideAll();
        }

        private void HideAll()
        {
            if (countdownHudPanel != null) countdownHudPanel.SetActive(false);
            if (chantPopupPanel != null) chantPopupPanel.SetActive(false);
        }
    }
}
