using UnityEngine;
namespace ContraptionMine
{
    public enum PartType { Frame, Wheel, Engine, Cargo, Spring, Balloon, HeavyWheel, PowerfulEngine, Propeller, Ballast }
    [CreateAssetMenu(menuName = "Contraption Mine/Part")]
    public sealed class PartData : ScriptableObject
    {
        public PartType type; public string label; public float mass = 1, power, grip = 1, radius = .48f, lift; public int capacity, stock = 8, unlockCost, upgradeCost = 80; public Color color = Color.white;
        public bool IsWheel => type == PartType.Wheel || type == PartType.HeavyWheel;
        public bool IsEngine => type == PartType.Engine || type == PartType.PowerfulEngine;
        public float Power(int level) => power * (1 + .2f * (level - 1));
        public int Capacity(int level) => capacity > 0 ? capacity + 2 * (level - 1) : 0;
    }
}
