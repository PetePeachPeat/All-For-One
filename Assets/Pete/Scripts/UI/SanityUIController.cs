using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pete.Systems;

namespace Pete.UI
{
    public class SanityUIController : MonoBehaviour
    {
        [Header("Top Minimal HUD Bar")]
        [SerializeField] private GameObject topBarRoot;
        [SerializeField] private RectTransform sanityFillBarRect;
        [SerializeField] private Image sanityFillBarImage;
        [SerializeField] private TextMeshProUGUI sanityPercentText;
        [SerializeField] private TextMeshProUGUI sanityDeltaHintText;

        [Header("Progressive Dark Vignette (Darkens every 10% drop)")]
        [SerializeField] private Image vignetteEdgeImage;
        [SerializeField] private Image pitchBlackCoreImage;

        [Header("Hold Spacebar Close-Eyes Overlay")]
        [SerializeField] private GameObject closeEyesOverlayRoot;
        [SerializeField] private TextMeshProUGUI closeEyesStatusText;

        private float displayedNormalized = 1f;
        private float deltaHintTimer = 0f;
        private const string DefaultHintText = "[HOLD SPACE : CLOSE EYES]";

        private void Start()
        {
            if (closeEyesOverlayRoot != null)
            {
                foreach (var g in closeEyesOverlayRoot.GetComponentsInChildren<Graphic>(true))
                {
                    if (g != null) g.raycastTarget = false;
                }
                var cg = closeEyesOverlayRoot.GetComponent<CanvasGroup>();
                if (cg == null) cg = closeEyesOverlayRoot.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;

                closeEyesOverlayRoot.SetActive(false);
            }

            if (SanityManager.Instance != null)
            {
                SanityManager.Instance.OnSanityChanged += HandleSanityChanged;
                SanityManager.Instance.OnSanityDeltaEvent += HandleSanityDeltaEvent;
                SanityManager.Instance.OnEyesClosedChanged += HandleEyesClosedChanged;

                HandleSanityChanged(SanityManager.Instance.CurrentSanity, SanityManager.Instance.MaxSanity);
                HandleEyesClosedChanged(SanityManager.Instance.IsClosingEyes);
            }
            else
            {
                UpdateVisualsImmediate(100f, 100f);
            }

            if (sanityDeltaHintText != null)
            {
                sanityDeltaHintText.text = DefaultHintText;
                sanityDeltaHintText.color = new Color(0.62f, 0.78f, 0.84f, 0.85f);
            }
        }

        private void OnDestroy()
        {
            if (SanityManager.Instance != null)
            {
                SanityManager.Instance.OnSanityChanged -= HandleSanityChanged;
                SanityManager.Instance.OnSanityDeltaEvent -= HandleSanityDeltaEvent;
                SanityManager.Instance.OnEyesClosedChanged -= HandleEyesClosedChanged;
            }
        }

        private void Update()
        {
            float targetNorm = (SanityManager.Instance != null) ? SanityManager.Instance.SanityNormalized : 1f;
            displayedNormalized = Mathf.Lerp(displayedNormalized, targetNorm, Time.unscaledDeltaTime * 10f);

            if (sanityFillBarRect != null)
            {
                sanityFillBarRect.anchorMin = Vector2.zero;
                sanityFillBarRect.anchorMax = new Vector2(Mathf.Clamp01(displayedNormalized), 1f);
                sanityFillBarRect.offsetMin = Vector2.zero;
                sanityFillBarRect.offsetMax = Vector2.zero;
            }

            if (deltaHintTimer > 0f)
            {
                deltaHintTimer -= Time.unscaledDeltaTime;
                if (deltaHintTimer <= 0f && sanityDeltaHintText != null)
                {
                    bool eyesClosed = SanityManager.Instance != null && SanityManager.Instance.IsClosingEyes;
                    sanityDeltaHintText.text = eyesClosed ? "RECOVERING SANITY..." : DefaultHintText;
                    sanityDeltaHintText.color = eyesClosed
                        ? new Color(0.35f, 0.94f, 0.62f, 0.95f)
                        : new Color(0.62f, 0.78f, 0.84f, 0.85f);
                }
            }

            // Subtle breathing pulse on CloseEyes text while holding Space
            if (closeEyesOverlayRoot != null && closeEyesOverlayRoot.activeSelf && closeEyesStatusText != null)
            {
                float pulse = 0.55f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.35f;
                Color c = closeEyesStatusText.color;
                c.a = pulse;
                closeEyesStatusText.color = c;
            }
        }

        private void HandleSanityChanged(float current, float max)
        {
            UpdateVisualsImmediate(current, max);
        }

        private void HandleSanityDeltaEvent(float delta, string label)
        {
            if (sanityDeltaHintText == null) return;
            deltaHintTimer = 2.6f;
            sanityDeltaHintText.text = label;
            sanityDeltaHintText.color = (delta >= 0f)
                ? new Color(0.29f, 0.92f, 0.52f, 1f)
                : new Color(0.98f, 0.28f, 0.28f, 1f);
        }

        private void HandleEyesClosedChanged(bool closed)
        {
            if (closeEyesOverlayRoot != null)
                closeEyesOverlayRoot.SetActive(closed);

            if (deltaHintTimer <= 0f && sanityDeltaHintText != null)
            {
                sanityDeltaHintText.text = closed ? "RECOVERING SANITY..." : DefaultHintText;
                sanityDeltaHintText.color = closed
                    ? new Color(0.35f, 0.94f, 0.62f, 0.95f)
                    : new Color(0.62f, 0.78f, 0.84f, 0.85f);
            }
        }

        private void UpdateVisualsImmediate(float current, float max)
        {
            float norm = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(norm * 100f);
            if (current <= 0.01f) pct = 0;

            if (sanityPercentText != null)
            {
                sanityPercentText.text = $"{pct}%";
                sanityPercentText.color = GetBarColor(norm);
            }

            if (sanityFillBarImage != null)
            {
                sanityFillBarImage.color = GetBarColor(norm);
            }

            // Progressive Dark Vignette: steps every 10% loss (0..10 steps) + smooth blend
            // Step 0 (91-100%): 0.0
            // Step 5 (41-50%): ~0.55
            // Step 9 (1-10%): ~0.92
            // Step 10 (0%): 1.0 (100% black)
            int lostSteps = Mathf.Clamp(10 - Mathf.CeilToInt(norm * 10f), 0, 10);
            float steppedDarkness = lostSteps / 10f;
            float smoothDarkness = 1f - norm;
            float combinedVignetteAlpha = Mathf.Clamp01(Mathf.Lerp(steppedDarkness, smoothDarkness, 0.5f) * 0.96f);
            if (norm <= 0.001f) combinedVignetteAlpha = 1f;

            if (vignetteEdgeImage != null)
            {
                vignetteEdgeImage.color = new Color(0f, 0f, 0f, combinedVignetteAlpha);
            }

            // Core darkness overlay kicks in below 40% Sanity and reaches 100% black at 0% Sanity
            if (pitchBlackCoreImage != null)
            {
                float coreAlpha = 0f;
                if (norm <= 0.001f)
                    coreAlpha = 1f;
                else if (norm < 0.40f)
                    coreAlpha = Mathf.Lerp(0.82f, 0f, norm / 0.40f);

                pitchBlackCoreImage.color = new Color(0f, 0f, 0f, coreAlpha);
            }
        }

        private static Color GetBarColor(float norm)
        {
            if (norm >= 0.60f)
                return new Color(0.24f, 0.88f, 0.54f, 1f); // Healthy Emerald
            if (norm >= 0.30f)
                return new Color(0.96f, 0.76f, 0.18f, 1f); // Warning Amber
            return new Color(0.95f, 0.22f, 0.22f, 1f);     // Critical Crimson
        }
    }
}
