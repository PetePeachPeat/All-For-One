using System.Collections;
using TMPro;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    public class TypewriterText : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private float charDelay = 0.022f;
        [SerializeField] private string keySfx = "typewriter_key";
        [SerializeField] private string finishSfx = "";
        [SerializeField] private int sfxEveryNChars = 2;

        private Coroutine _typingRoutine;
        private string _fullString = "";

        private void Awake()
        {
            if (targetText == null) targetText = GetComponent<TMP_Text>();
        }

        public void PlayText(string text, float customDelay = -1f)
        {
            if (targetText == null) targetText = GetComponent<TMP_Text>();
            if (targetText == null) return;

            _fullString = text ?? "";
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);

            if (!gameObject.activeInHierarchy)
            {
                targetText.text = _fullString;
                targetText.maxVisibleCharacters = 99999;
                return;
            }

            float delay = customDelay > 0f ? customDelay : charDelay;
            _typingRoutine = StartCoroutine(TypeRoutine(_fullString, delay));
        }

        public void SkipToEnd()
        {
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            if (targetText != null)
            {
                targetText.text = _fullString;
                targetText.maxVisibleCharacters = 99999;
            }
        }

        public void ClearImmediate()
        {
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            if (targetText != null)
            {
                targetText.text = "";
                targetText.maxVisibleCharacters = 0;
            }
        }

        private IEnumerator TypeRoutine(string text, float delay)
        {
            targetText.text = text;
            targetText.ForceMeshUpdate();
            int total = targetText.textInfo.characterCount;
            targetText.maxVisibleCharacters = 0;

            for (int i = 1; i <= total; i++)
            {
                targetText.maxVisibleCharacters = i;
                if (!string.IsNullOrEmpty(keySfx) && (i % Mathf.Max(1, sfxEveryNChars) == 0))
                {
                    if (MenuAudioManager.Instance != null)
                        MenuAudioManager.Instance.Play(keySfx, 0.06f);
                }
                yield return new WaitForSecondsRealtime(delay);
            }

            if (!string.IsNullOrEmpty(finishSfx) && MenuAudioManager.Instance != null)
            {
                MenuAudioManager.Instance.Play(finishSfx, 0.01f);
            }
        }
    }
}
