using UnityEngine;
using UnityEngine.UI;

namespace OrangeWorld
{
    // Builds a small world-space UI Text label from code, for a HUD readout or in-world signage. Rotated 180 on
    // Y so it reads correctly facing back toward whoever it was placed in front of.
    public static class WorldText
    {
        const float CanvasScale = 0.01f;

        public static Text Create(string name, Transform parent, Vector3 localPosition, Vector2 worldSize, float worldFontHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * CanvasScale;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = worldSize / CanvasScale;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var uiText = textGo.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = Mathf.RoundToInt(worldFontHeight / CanvasScale);
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.color = Color.white;

            return uiText;
        }
    }
}
