using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Stat Definition")]
    public sealed class StatDefinition : ScriptableObject
    {
        public StatId id;
        public string displayName;
        public int defaultValue;
        public int minimum;
        public int maximum;
    }
}
