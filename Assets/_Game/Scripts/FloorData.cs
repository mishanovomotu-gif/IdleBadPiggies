using System;
using UnityEngine;
namespace ContraptionMine
{
    [CreateAssetMenu(menuName = "Contraption Mine/Floor")]
    public sealed class FloorData : ScriptableObject
    {
        public GameObject trackPrefab;
        public OreType oreType;
        public ChallengeZone[] zones = Array.Empty<ChallengeZone>();
        public float OreMass => oreType switch { OreType.Copper => .38f, OreType.Gold => .48f, _ => .32f };
        public float OreValue => oreType switch { OreType.Copper => 1.15f, OreType.Gold => 1.5f, _ => 1 };
        public string OreName => oreType.ToString().ToUpperInvariant();
        public Color OreColor => oreType switch { OreType.Copper => new Color(1, .56f, .24f), OreType.Gold => new Color(1, .87f, .22f), _ => Color.white };
        public int number, requiredOre, unlockPrice; public float distance, multiplier = 1; public string challenge; public Vector2[] terrain; public int gapAfter = -1; public int[] extraGaps = Array.Empty<int>(); public Color rockTint = new(.57f, .31f, .15f); public bool HasGap(int index) => index == gapAfter || Array.IndexOf(extraGaps ?? Array.Empty<int>(), index) >= 0; public VehicleDefinition blueprint;
    }
}
