using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class MenuClock : MonoBehaviour
    {
        [SerializeField] private Image recDotImage;
        [SerializeField] private TMP_Text recLabelText;
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text secOsTagText;
        [SerializeField] private bool useSystemTime = false;

        private float _recTimer;
        private float _cursorTimer;
        private float _shiftSimSeconds;
        private bool _cursorVisible = true;

        private static readonly Color NormalClockColor = new Color(0.91f, 0.90f, 0.88f, 0.88f);
        private static readonly Color CurseRedColor = new Color(0.90f, 0.10f, 0.10f, 1.0f);

        private void Update()
        {
            // Blinking red REC dot
            _recTimer += Time.unscaledDeltaTime;
            if (recDotImage != null)
            {
                float pulse = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(_recTimer * 4.2f));
                Color c = recDotImage.color;
                c.a = pulse;
                recDotImage.color = c;
            }

            // Blinking SEC-OS cursor at bottom-right
            _cursorTimer += Time.unscaledDeltaTime;
            if (_cursorTimer >= 0.52f)
            {
                _cursorTimer = 0f;
                _cursorVisible = !_cursorVisible;
                if (secOsTagText != null)
                    secOsTagText.text = _cursorVisible ? "SEC-OS v1.0 ▌" : "SEC-OS v1.0  ";
            }

            // Clock update (00:00 AM night shift or 03:17 Easter egg)
            _shiftSimSeconds += Time.unscaledDeltaTime;
            if (clockText != null)
            {
                DateTime now = DateTime.Now;
                bool is317 = (now.Hour == 3 && now.Minute == 17) || (Mathf.FloorToInt(_shiftSimSeconds) % 90 == 45);

                if (is317)
                {
                    clockText.text = "03:17 AM";
                    clockText.color = CurseRedColor;
                }
                else if (useSystemTime)
                {
                    clockText.text = now.ToString("hh:mm tt");
                    clockText.color = NormalClockColor;
                }
                else
                {
                    int mins = Mathf.FloorToInt(_shiftSimSeconds / 8f) % 60;
                    clockText.text = $"00:{mins:00} AM";
                    clockText.color = NormalClockColor;
                }
            }
        }
    }
}
