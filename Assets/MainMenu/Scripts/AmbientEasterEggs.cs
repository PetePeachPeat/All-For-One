using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class AmbientEasterEggs : MonoBehaviour
    {
        [SerializeField] private RectTransform eyeFlashRect;
        [SerializeField] private Image eyeFlashImage;
        [SerializeField] private RectTransform wordFlashRect;
        [SerializeField] private TMP_Text wordFlashText;

        private float _eyeTimer;
        private float _wordTimer;
        private float _phoneIdleTimer;
        private bool _phoneRangOnce;

        private static readonly string[] GhostWords = { "HELP", "HAHAHA", "???", "EVIL", "DIE" };

        private void Awake()
        {
            if (eyeFlashImage != null) eyeFlashImage.gameObject.SetActive(false);
            if (wordFlashText != null) wordFlashText.gameObject.SetActive(false);
            ResetTimers();
        }

        public void NotifyUserInput()
        {
            _phoneIdleTimer = 0f;
            _phoneRangOnce = false;
        }

        private void ResetTimers()
        {
            _eyeTimer = Random.Range(14f, 26f);
            _wordTimer = Random.Range(22f, 38f);
        }

        private void Update()
        {
            bool photoSafe = PlayerPrefs.GetInt("MM_PhotoSafe", 0) == 1;

            _phoneIdleTimer += Time.unscaledDeltaTime;
            if (!_phoneRangOnce && _phoneIdleTimer >= 60f)
            {
                _phoneRangOnce = true;
                if (MenuAudioManager.Instance != null)
                    MenuAudioManager.Instance.Play("phone_ring_far", 0.01f);
            }

            if (photoSafe) return;

            _eyeTimer -= Time.unscaledDeltaTime;
            if (_eyeTimer <= 0f)
            {
                _eyeTimer = Random.Range(22f, 40f);
                StartCoroutine(FlashEyeRoutine());
            }

            _wordTimer -= Time.unscaledDeltaTime;
            if (_wordTimer <= 0f)
            {
                _wordTimer = Random.Range(32f, 55f);
                StartCoroutine(FlashWordRoutine());
            }
        }

        private IEnumerator FlashEyeRoutine()
        {
            if (eyeFlashRect == null || eyeFlashImage == null) yield break;
            eyeFlashRect.anchoredPosition = new Vector2(Random.Range(160f, 520f), Random.Range(-220f, 220f));
            eyeFlashImage.color = new Color(0.95f, 0.16f, 0.16f, 0.18f);
            eyeFlashImage.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.14f);
            eyeFlashImage.gameObject.SetActive(false);
        }

        private IEnumerator FlashWordRoutine()
        {
            if (wordFlashRect == null || wordFlashText == null) yield break;
            wordFlashText.text = GhostWords[Random.Range(0, GhostWords.Length)];
            wordFlashRect.anchoredPosition = new Vector2(Random.Range(-180f, 460f), Random.Range(-260f, 260f));
            wordFlashRect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-11f, 11f));
            wordFlashText.color = new Color(0.92f, 0.12f, 0.12f, 0.22f);
            wordFlashText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.09f);
            wordFlashText.gameObject.SetActive(false);
        }
    }
}
