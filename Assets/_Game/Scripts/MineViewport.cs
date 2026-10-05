using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    // Keep UI and icons at a uniform width scale; tall screens add room to the course.
    public sealed class MineViewport : MonoBehaviour
    {
#if UNITY_EDITOR
        int validationWidth, validationHeight;
        public void SetValidationSize(int width, int height) { validationWidth = width; validationHeight = height; enabled = false; Apply(); }
#endif
        bool runView;
        public void SetRunView(bool value) { runView = value; Apply(); }
        Canvas canvas; RectTransform content; Camera worldCamera; Rect previousSafe; int previousWidth, previousHeight;
        public static Rect Fit(Rect available)
        {
            if (available.width / available.height <= 1080f / 1920f) return available;
            float scale = Mathf.Min(available.width / 1080f, available.height / 1920f);
            var size = new Vector2(1080, 1920) * scale;
            return new Rect(available.center - size * .5f, size);
        }
        public void Initialize(Canvas target, RectTransform root, Camera world)
        {
            canvas = target; content = root; worldCamera = world; Apply();
        }
        void Update() { if (Screen.width != previousWidth || Screen.height != previousHeight || Screen.safeArea != previousSafe) { Apply(); FindFirstObjectByType<MineGame>()?.ViewportChanged(); } }
        void Apply()
        {
            previousWidth = Screen.width; previousHeight = Screen.height; previousSafe = Screen.safeArea;
            if (previousWidth <= 0 || previousHeight <= 0) return;
#if UNITY_EDITOR
            if (validationWidth > 0) { previousWidth = validationWidth; previousHeight = validationHeight; previousSafe = new Rect(0, 0, previousWidth, previousHeight); }
#endif
            var safe = previousSafe.width > 0 && previousSafe.height > 0 ? previousSafe : new Rect(0, 0, previousWidth, previousHeight);
            var fitted = Fit(safe); float scale = fitted.width / 1080f;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = scale; canvas.scaleFactor = scale;
            content.anchorMin = content.anchorMax = content.pivot = Vector2.one * .5f;
            content.sizeDelta = new Vector2(1080, fitted.height / scale); content.anchoredPosition = (fitted.center - new Vector2(previousWidth, previousHeight) * .5f) / scale;
            float extra = content.sizeDelta.y - 1920;
            float top = runView ? 365 : 567, bottom = (runView ? 1518 : 1047) + extra;
            worldCamera.rect = new Rect(fitted.x / previousWidth, (fitted.y + (content.sizeDelta.y - bottom) * scale) / previousHeight, fitted.width / previousWidth, (bottom - top) * scale / previousHeight);
#if UNITY_EDITOR
            if (validationWidth > 0) worldCamera.aspect = fitted.width / ((bottom - top) * scale);
#endif
        }
    }
}
