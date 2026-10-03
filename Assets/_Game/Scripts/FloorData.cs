using System;
using UnityEngine;
namespace ContraptionMine
{
    [CreateAssetMenu(menuName = "Contraption Mine/Floor")]
    public sealed class FloorData : ScriptableObject
    {
        public GameObject trackPrefab;
        public int number, requiredOre, unlockPrice; public float distance, multiplier = 1; public string challenge; public Vector2[] terrain; public int gapAfter = -1; public int[] extraGaps = Array.Empty<int>(); public Color rockTint = new(.57f,.31f,.15f); public bool HasGap(int index) => index == gapAfter || Array.IndexOf(extraGaps ?? Array.Empty<int>(), index) >= 0; public VehicleDefinition blueprint;
    }
}
