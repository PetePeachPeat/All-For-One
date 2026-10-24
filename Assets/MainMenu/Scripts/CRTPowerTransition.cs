using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class CRTPowerTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform powerLineRect;
        [SerializeField] private Image powerLineImage;
        [SerializeField] private Image fadeBlackImage;
        [SerializeField] private float onDuration = 0.28f;
        [SerializeField] private float offDuration = 0.28f;

        public void PlayPowerOn(Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(PowerOnRoutine(onComplete));
        }

        public void PlayPowerOff(Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(PowerOffRoutine(onComplete));
        }

        private IEnumerator PowerOnRoutine(Action onComplete)
        {
            if (fadeBlackImage != null)
            {
                fadeBlackImage.gameObject.SetActive(true);
                fadeBlackImage.color = new Color(0.02f, 0.02f, 0.02f, 1f);
            }
            if (powerLineRect != null)
            {
                powerLineRect.gameObject.SetActive(true);
                powerLineRect.sizeDelta = new Vector2(1920f, 3f);
            }
            if (powerLineImage != null)
                powerLineImage.color = new Color(0.95f, 0.96f, 0.94f, 0f);

            yield return new WaitForSecondsRealtime(0.25f);
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("crt_on", 0.01f);

            // Expand thin white horizontal line vertically to full screen
            float t = 0f;
            while (t < onDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / onDuration);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                if (powerLineRect != null)
                    powerLineRect.sizeDelta = new Vector2(1920f, Mathf.Lerp(4f, 1080f, ease));
                if (powerLineImage != null)
                    powerLineImage.color = new Color(0.95f, 0.96f, 0.94f, Mathf.Lerp(0.95f, 0f, p));
                if (fadeBlackImage != null)
                    fadeBlackImage.color = new Color(0.02f, 0.02f, 0.02f, 1f - ease);
                yield return null;
            }

            if (powerLineRect != null) powerLineRect.gameObject.SetActive(false);
            if (fadeBlackImage != null) fadeBlackImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator PowerOffRoutine(Action onComplete)
        {
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("crt_off", 0.01f);

            if (fadeBlackImage != null)
            {
                fadeBlackImage.gameObject.SetActive(true);
                fadeBlackImage.color = new Color(0.02f, 0.02f, 0.02f, 0f);
            }
            if (powerLineRect != null)
            {
                powerLineRect.gameObject.SetActive(true);
                powerLineRect.sizeDelta = new Vector2(1920f, 1080f);
            }
            if (powerLineImage != null)
                powerLineImage.color = new Color(0.95f, 0.96f, 0.94f, 0.25f);

            // Step 1: Collapse Y to thin white line (0.15s)
            float half = offDuration * 0.55f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                if (powerLineRect != null)
                    powerLineRect.sizeDelta = new Vector2(1920f, Mathf.Lerp(1080f, 6f, p));
                if (powerLineImage != null)
                    powerLineImage.color = new Color(0.96f, 0.96f, 0.94f, Mathf.Lerp(0.35f, 1f, p));
                if (fadeBlackImage != null)
                    fadeBlackImage.color = new Color(0.02f, 0.02f, 0.02f, p);
                yield return null;
            }

            // Step 2: Collapse X from line to center dot (0.12s)
            float second = offDuration * 0.45f;
            t = 0f;
            while (t < second)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / second);
                if (powerLineRect != null)
                    powerLineRect.sizeDelta = new Vector2(Mathf.Lerp(1920f, 0f, p), Mathf.Lerp(6f, 2f, p));
                if (powerLineImage != null)
                    powerLineImage.color = new Color(0.96f, 0.96f, 0.94f, 1f - p);
                yield return null;
            }

            if (powerLineRect != null) powerLineRect.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.32f);
            onComplete?.Invoke();
        }
    }
}
