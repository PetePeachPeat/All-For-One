using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    public class Extrude3DText : MonoBehaviour
    {
        [SerializeField] private TMP_Text faceText;
        [SerializeField] private RectTransform extrudeContainer;
        [SerializeField] private int layers = 10;
        [SerializeField] private Vector2 stepOffset = new Vector2(-1.8f, -1.6f);
        [SerializeField] private Color colorBack = new Color(0.18f, 0.19f, 0.21f, 0.95f);
        [SerializeField] private Color colorFront = new Color(0.62f, 0.64f, 0.68f, 0.98f);

        [SerializeField] private List<TMP_Text> generatedLayers = new List<TMP_Text>();

        public void SetText(string value)
        {
            if (faceText != null) faceText.text = value;
            foreach (var l in generatedLayers)
            {
                if (l != null) l.text = value;
            }
        }

        public void RebuildExtrudeLayers()
        {
            if (faceText == null || extrudeContainer == null) return;

            for (int i = extrudeContainer.childCount - 1; i >= 0; i--)
            {
                var child = extrudeContainer.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            generatedLayers.Clear();

            // Build from back (deepest layer) to front (closest to face)
            for (int i = layers; i >= 1; i--)
            {
                var go = new GameObject($"Extrude_{i:00}", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(extrudeContainer, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = stepOffset * i;

                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.font = faceText.font;
                tmp.fontSize = faceText.fontSize;
                tmp.fontStyle = faceText.fontStyle;
                tmp.alignment = faceText.alignment;
                tmp.characterSpacing = faceText.characterSpacing;
                tmp.textWrappingMode = faceText.textWrappingMode;
                tmp.text = faceText.text;
                tmp.raycastTarget = false;

                float t = layers > 1 ? 1f - ((i - 1f) / (layers - 1f)) : 1f;
                tmp.color = Color.Lerp(colorBack, colorFront, t);

                generatedLayers.Add(tmp);
            }
        }
    }
}
