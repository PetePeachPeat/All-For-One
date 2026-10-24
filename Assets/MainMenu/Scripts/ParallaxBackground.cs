using UnityEngine;
using UnityEngine.InputSystem;

namespace SecurityCenter.MainMenu
{
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private float maxOffset = 12f;
        [SerializeField] private float lerpSpeed = 3f;

        private Vector2 _baseAnchoredPos;

        private void Awake()
        {
            if (targetRect == null) targetRect = GetComponent<RectTransform>();
            if (targetRect != null) _baseAnchoredPos = targetRect.anchoredPosition;
        }

        private void Update()
        {
            if (targetRect == null || Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            float nx = Mathf.Clamp((mousePos.x / Mathf.Max(1f, Screen.width)) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp((mousePos.y / Mathf.Max(1f, Screen.height)) * 2f - 1f, -1f, 1f);

            Vector2 desired = _baseAnchoredPos + new Vector2(-nx * maxOffset, -ny * maxOffset * 0.65f);
            targetRect.anchoredPosition = Vector2.Lerp(targetRect.anchoredPosition, desired, Time.unscaledDeltaTime * lerpSpeed);
        }
    }
}
