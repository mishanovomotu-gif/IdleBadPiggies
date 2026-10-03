using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    // Fit the authored portrait as one unit, including the physics viewport.
    public sealed class MineViewport : MonoBehaviour
    {
        Canvas canvas; RectTransform content; Camera worldCamera; Rect previousSafe; int previousWidth, previousHeight;
        public static Rect Fit(Rect available)
        {
            float scale = Mathf.Min(available.width / 1080f, available.height / 1920f);
            var size = new Vector2(1080, 1920) * scale;
            return new Rect(available.center - size * .5f, size);
        }
        public void Initialize(Canvas target, RectTransform root, Camera world)
        {
            canvas = target; content = root; worldCamera = world; Apply();
        }
        void Update() { if (Screen.width != previousWidth || Screen.height != previousHeight || Screen.safeArea != previousSafe) Apply(); }
        void Apply()
        {
            previousWidth = Screen.width; previousHeight = Screen.height; previousSafe = Screen.safeArea;
            if (previousWidth <= 0 || previousHeight <= 0) return;
            var safe = previousSafe.width > 0 && previousSafe.height > 0 ? previousSafe : new Rect(0, 0, previousWidth, previousHeight);
            var fitted = Fit(safe); float scale = fitted.width / 1080f;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = scale; canvas.scaleFactor = scale;
            content.anchorMin = content.anchorMax = content.pivot = Vector2.one * .5f;
            content.sizeDelta = new Vector2(1080, 1920); content.anchoredPosition = (fitted.center - new Vector2(previousWidth, previousHeight) * .5f) / scale;
            worldCamera.rect = new Rect(fitted.x / previousWidth, (fitted.y + fitted.height * .455f) / previousHeight, fitted.width / previousWidth, fitted.height * .25f / previousHeight);
        }
    }
}
