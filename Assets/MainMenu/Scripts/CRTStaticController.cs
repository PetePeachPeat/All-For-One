using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SecurityCenter.MainMenu
{
    public class CRTStaticController : MonoBehaviour
    {
        [SerializeField] private RawImage staticRawImage;
        [SerializeField] private Material staticMaterial;
        [SerializeField] private float idleIntensity = 0.08f;

        private Material _runtimeMat;
        private float _currentIntensity = 1.0f;
        private Coroutine _tweenRoutine;

        private void Awake()
        {
            if (staticRawImage == null) staticRawImage = GetComponent<RawImage>();
            if (staticMaterial != null)
            {
                _runtimeMat = new Material(staticMaterial);
                if (staticRawImage != null) staticRawImage.material = _runtimeMat;
            }
            ApplyPrefsQuality();
        }

        public void ApplyPrefsQuality()
        {
            int crtMode = PlayerPrefs.GetInt("MM_CRTFx", 2); // 0=Off, 1=Low, 2=High
            bool grainOn = PlayerPrefs.GetInt("MM_Grain", 1) == 1;

            float mult = crtMode == 0 ? 0.15f : (crtMode == 1 ? 0.6f : 1.0f);
            if (!grainOn) mult *= 0.45f;

            if (_runtimeMat != null)
            {
                _runtimeMat.SetFloat("_ScanlineStrength", crtMode == 0 ? 0f : (crtMode == 1 ? 0.12f : 0.24f));
            }
            SetIntensityImmediate(_currentIntensity * mult);
        }

        public void SetIntensityImmediate(float value)
        {
            _currentIntensity = Mathf.Clamp01(value);
            if (_runtimeMat != null)
                _runtimeMat.SetFloat("_Intensity", _currentIntensity);
            else if (staticRawImage != null)
            {
                Color c = staticRawImage.color;
                c.a = _currentIntensity;
                staticRawImage.color = c;
            }
        }

        public void TweenIntensity(float target, float duration)
        {
            if (_tweenRoutine != null) StopCoroutine(_tweenRoutine);
            _tweenRoutine = StartCoroutine(TweenRoutine(target, duration));
        }

        public void PulseBurst(float burstIntensity = 0.25f, float duration = 0.16f)
        {
            if (PlayerPrefs.GetInt("MM_PhotoSafe", 0) == 1) return;
            if (_tweenRoutine != null) StopCoroutine(_tweenRoutine);
            _tweenRoutine = StartCoroutine(BurstRoutine(burstIntensity, duration));
        }

        private IEnumerator TweenRoutine(float target, float duration)
        {
            float start = _currentIntensity;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 1f;
                SetIntensityImmediate(Mathf.Lerp(start, target, t));
                yield return null;
            }
            SetIntensityImmediate(target);
        }

        private IEnumerator BurstRoutine(float peak, float duration)
        {
            SetIntensityImmediate(peak);
            yield return new WaitForSecondsRealtime(duration * 0.45f);
            yield return TweenRoutine(idleIntensity, duration * 0.55f);
        }
    }
}
