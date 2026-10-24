using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    public class ScrambleText : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private float duration = 0.22f;
        [SerializeField] private string charset = "0123456789ABCDEF#?@%█▒░";

        private Coroutine _routine;
        private readonly StringBuilder _sb = new StringBuilder(64);

        public void DecodeTo(string finalString)
        {
            if (targetText == null) targetText = GetComponent<TMP_Text>();
            if (targetText == null) return;

            if (_routine != null) StopCoroutine(_routine);
            if (!gameObject.activeInHierarchy)
            {
                targetText.text = finalString;
                return;
            }
            _routine = StartCoroutine(DecodeRoutine(finalString ?? ""));
        }

        private IEnumerator DecodeRoutine(string finalStr)
        {
            float elapsed = 0f;
            int len = finalStr.Length;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                int locked = Mathf.FloorToInt(p * len);

                _sb.Clear();
                if (locked > 0) _sb.Append(finalStr, 0, locked);
                int scrambleCount = Mathf.Min(6, len - locked);
                for (int i = 0; i < scrambleCount; i++)
                {
                    char c = finalStr[locked + i];
                    if (c == ' ') _sb.Append(' ');
                    else _sb.Append(charset[Random.Range(0, charset.Length)]);
                }
                targetText.text = _sb.ToString();
                yield return new WaitForSecondsRealtime(0.025f);
            }
            targetText.text = finalStr;
        }
    }
}
