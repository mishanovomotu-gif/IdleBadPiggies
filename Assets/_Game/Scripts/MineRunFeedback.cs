using TMPro;
using UnityEngine;
namespace ContraptionMine
{
    public sealed partial class MineGame
    {
        string RunHint(bool success, string reason)
        {
            if (success && vehicle.cargo < Floor.requiredOre) return $"Add cargo: this floor needs {Floor.requiredOre} {Floor.OreName}.";
            if (success && vehicle.peakImpact > 7) return "Hard landing: try a spring or spread the wheels farther apart.";
            if (success) return "Try moving cargo lower or adding a low propeller, then compare income.";
            if (reason.Contains("Flipped")) return "Lower cargo or add ballast below the frame. High thrust can tip the body.";
            if (reason.Contains("Fell")) return "Add lift near both ends or a low propeller for the gap approach.";
            if (reason.Contains("Stalled")) return "Use more power or less cargo. On ice, enter with momentum; torque alone spins wheels.";
            if (reason.Contains("disconnected")) return "Widen the wheelbase and soften landings with a spring.";
            if (reason.Contains("Time")) return "Reduce weight or add forward thrust. Dense ore needs more power.";
            return "Move a part, test again, and compare your last successful income.";
        }
        void RunResultPanel()
        {
            var attempt = Current.lastAttempt; if (!showResults || running || attempt == null) return;
            Box("Test result", 28, 632, 1024, 313, Navy, true);
            Label(attempt.success ? "DELIVERY COMPLETE!" : "TRY ANOTHER BUILD", 54, 644, 970, 43, 32, attempt.success ? new Color(.65f, 1, .33f) : Gold, true);
            float change = attempt.rate - (Current.automated?.rate ?? 0);
            Label($"{attempt.ore} {Floor.OreName} · {attempt.seconds:0.0}s · {attempt.distance:0}/{Floor.distance:0} m\n" + (attempt.success ? $"{attempt.rate:0}/min · {(change >= 0 ? "+" : "")}{change:0}/min vs AUTO" : attempt.reason), 54, 691, 970, 77, 25, Color.white, true);
            Label($"AIRTIME {attempt.airSeconds:0.0}s · IMPACT {attempt.impact:0.0} m/s", 54, 769, 970, 33, 21, new Color(.43f, .85f, 1), true);
            Label(attempt.hint, 54, 804, 970, 57, 22, Gold, true);
            Button("RETRY", 54, 870, 470, 58, StartRun, Green, 26, true);
            Button("MODIFY BUILD", 542, 870, 482, 58, () => { showResults = false; message = MineChallenges.Brief(Floor); Refresh(); }, Blue, 26, true);
        }
        void DecorateChallenges()
        {
            foreach (var zone in Floor.zones)
            {
                float ground = MineChallenges.GroundHeight(Floor, zone.from);
                if (zone.kind == ChallengeKind.Ceiling)
                {
                    VehiclePhysics.Shape("Low timber roof", track, new Vector2((zone.from + zone.to) * .5f, zone.strength), new Vector2(zone.to - zone.from, .45f), Wood, false, 1);
                    for (float x = zone.from; x <= zone.to; x += 4) VehiclePhysics.Shape("Roof bolt", track, new Vector2(x, zone.strength), Vector2.one * .14f, Color.white, true, 2);
                }
                if (zone.kind == ChallengeKind.Ice)
                    for (int i = 0; i < Floor.terrain.Length - 1; i++)
                    {
                        if (Floor.HasGap(i)) continue; float start = Mathf.Max(zone.from, Floor.terrain[i].x), end = Mathf.Min(zone.to, Floor.terrain[i + 1].x); if (end <= start) continue;
                        Vector2 a = new(start, MineChallenges.GroundHeight(Floor, start)), b = new(end, MineChallenges.GroundHeight(Floor, end));
                        var frost = VehiclePhysics.Shape("Visible icy surface", track, (a + b) * .5f + Vector2.up * .03f, new Vector2(Vector2.Distance(a, b), .2f), new Color(.52f, .95f, 1), false, 2);
                        frost.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                    }
                if (zone.kind == ChallengeKind.Wind)
                    for (float x = zone.from + 1; x < zone.to; x += 5)
                    {
                        var gust = VehiclePhysics.Shape("Wind direction", track, new Vector2(x, ground + 3.5f), new Vector2(1.6f, .08f), new Color(.6f, .92f, 1, .6f), false, 1);
                        VehiclePhysics.Shape("Wind tail", gust.transform, new Vector2(zone.strength < 0 ? -.35f : .35f, .5f), new Vector2(.35f, .13f), Color.white, false, 2);
                    }
                VehiclePhysics.Shape("Challenge sign", track, new Vector2(zone.from, ground + 5), new Vector2(6, .8f), Blue, false, 2);
                var sign = new GameObject(zone.Label); sign.transform.SetParent(track, false); sign.transform.position = new Vector3(zone.from, ground + 5, -.1f);
                var text = sign.AddComponent<TextMeshPro>(); text.text = zone.Label; text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); text.fontSize = 2.8f; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.rectTransform.sizeDelta = new Vector2(6, .7f); text.GetComponent<MeshRenderer>().sortingOrder = 3;
            }
        }
    }
}
