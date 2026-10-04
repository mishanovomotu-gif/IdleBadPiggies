using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    // A lightweight illustration of recorded production; economy never waits for this animation.
    public sealed class DeliveryLoop : MonoBehaviour
    {
        public RectTransform cart; public Image[] wheels; public TMP_Text status;
        public double productionRate; public FloorState floor; public float duration = 6, from = 210, to = 790, y = -185, phase;
        void Update()
        {
            if (cart == null) return;
            if (!floor.manager && floor.stored >= productionRate * SaveManager.OfflineHours * 60 - .01) { status.text = "STORAGE FULL · COLLECT EARNINGS"; return; }
            float t = Mathf.Repeat(Time.unscaledTime / duration + phase, 1); bool returning = t > .60f;
            float travel = t < .45f ? t / .45f : t < .60f ? 1 : t < .95f ? 1 - (t - .60f) / .35f : 0;
            cart.anchoredPosition = new Vector2(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, travel)), y + (t < .45f || returning && t < .95f ? Mathf.Sin(t * 100) * 2 : 0));
            cart.localScale = new Vector3(returning ? -1 : 1, 1, 1);
            foreach (var wheel in wheels) if (wheel != null) wheel.rectTransform.localRotation = Quaternion.Euler(0, 0, travel * -1500);
            if (status != null) status.text = t >= .45f && t < .60f ? "UNLOADING + COINS" : returning ? "RETURNING TO MINE" : "DELIVERING ORE";
        }
    }
}
