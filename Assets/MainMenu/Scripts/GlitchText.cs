using System.Collections;
using TMPro;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    public class GlitchText : MonoBehaviour
    {
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private RectTransform redLayerRect;
        [SerializeField] private RectTransform cyanLayerRect;
        [SerializeField] private TMP_Text faceText;
        [SerializeField] private TMP_Text redText;
        [SerializeField] private TMP_Text cyanText;

        [SerializeField] private float burstMinInterval = 4.0f;
        [SerializeField] private float burstMaxInterval = 8.5f;
        [SerializeField] private float idleSplitX = 2.2f;

        private Vector2 _baseRootPos;
        private float _nextBurstTime;
        private float _jitterTimer;
        private bool _intenseDangerMode;
        private bool _isBursting;

        private void Awake()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            if (rootRect != null) _baseRootPos = rootRect.anchoredPosition;
            ScheduleNextBurst();
        }

        public void SetDangerMode(bool danger)
        {
            _intenseDangerMode = danger;
            if (danger && !_isBursting)
            {
                TriggerBurst(0.22f, false);
            }
        }

        public void TriggerIntroGlitchIn()
        {
            StopAllCoroutines();
            StartCoroutine(IntroGlitchRoutine());
        }

        public void TriggerBurst(float duration = 0.2f, bool playSfx = true)
        {
            if (PlayerPrefs.GetInt("MM_PhotoSafe", 0) == 1) return;
            StopAllCoroutines();
            StartCoroutine(BurstRoutine(duration, playSfx));
        }

        private void ScheduleNextBurst()
        {
            float min = _intenseDangerMode ? 1.1f : burstMinInterval;
            float max = _intenseDangerMode ? 2.1f : burstMaxInterval;
            _nextBurstTime = Time.unscaledTime + Random.Range(min, max);
        }

        private void Update()
        {
            bool photoSafe = PlayerPrefs.GetInt("MM_PhotoSafe", 0) == 1;
            if (photoSafe)
            {
                if (redLayerRect != null) redLayerRect.anchoredPosition = new Vector2(-idleSplitX, 0f);
                if (cyanLayerRect != null) cyanLayerRect.anchoredPosition = new Vector2(idleSplitX, 0f);
                if (rootRect != null) rootRect.anchoredPosition = _baseRootPos;
                return;
            }

            if (!_isBursting)
            {
                _jitterTimer += Time.unscaledDeltaTime;
                if (_jitterTimer >= 0.075f)
                {
                    _jitterTimer = 0f;
                    float jx = Random.Range(-0.7f, 0.7f);
                    float jy = Random.Range(-0.4f, 0.4f);
                    if (redLayerRect != null)
                        redLayerRect.anchoredPosition = new Vector2(-idleSplitX + jx, jy);
                    if (cyanLayerRect != null)
                        cyanLayerRect.anchoredPosition = new Vector2(idleSplitX - jx, -jy);
                }

                if (Time.unscaledTime >= _nextBurstTime)
                {
                    ScheduleNextBurst();
                    TriggerBurst(Random.Range(0.14f, 0.28f), true);
                }
            }
        }

        private IEnumerator IntroGlitchRoutine()
        {
            _isBursting = true;
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.Play("glitch_burst", 0.04f);

            float dur = 0.42f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - (elapsed / dur);
                if (redLayerRect != null)
                    redLayerRect.anchoredPosition = new Vector2(Random.Range(-22f, 22f) * damp, Random.Range(-4f, 4f) * damp);
                if (cyanLayerRect != null)
                    cyanLayerRect.anchoredPosition = new Vector2(Random.Range(-22f, 22f) * damp, Random.Range(-4f, 4f) * damp);
                if (rootRect != null)
                    rootRect.anchoredPosition = _baseRootPos + new Vector2(Random.Range(-14f, 14f) * damp, 0f);
                yield return new WaitForSecondsRealtime(0.03f);
            }

            ResetPositions();
            _isBursting = false;
            ScheduleNextBurst();
        }

        private IEnumerator BurstRoutine(float duration, bool playSfx)
        {
            _isBursting = true;
            if (playSfx && MenuAudioManager.Instance != null)
                MenuAudioManager.Instance.Play("glitch_burst", 0.05f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += 0.032f;
                if (redLayerRect != null)
                    redLayerRect.anchoredPosition = new Vector2(Random.Range(-14f, -4f), Random.Range(-3f, 3f));
                if (cyanLayerRect != null)
                    cyanLayerRect.anchoredPosition = new Vector2(Random.Range(4f, 14f), Random.Range(-3f, 3f));
                if (rootRect != null)
                    rootRect.anchoredPosition = _baseRootPos + new Vector2(Random.Range(-8f, 8f), Random.Range(-2f, 2f));
                if (faceText != null)
                {
                    Color c = faceText.color;
                    c.a = Random.value > 0.35f ? 1f : 0.45f;
                    faceText.color = c;
                }
                yield return new WaitForSecondsRealtime(0.032f);
            }

            ResetPositions();
            _isBursting = false;
        }

        private void ResetPositions()
        {
            if (redLayerRect != null) redLayerRect.anchoredPosition = new Vector2(-idleSplitX, 0f);
            if (cyanLayerRect != null) cyanLayerRect.anchoredPosition = new Vector2(idleSplitX, 0f);
            if (rootRect != null) rootRect.anchoredPosition = _baseRootPos;
            if (faceText != null)
            {
                Color c = faceText.color;
                c.a = 1f;
                faceText.color = c;
            }
        }
    }
}
