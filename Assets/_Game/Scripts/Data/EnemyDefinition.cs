using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public EnemyArchetype archetype;
        public string displayName;
        public int maxHealth = 1;
        public float moveSpeed = 2f;
        public float attackInterval = 2f;
        public ElementId element;
        public int elementLevel = 1;
        public Color color = Color.red;
    }
}
