using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public StatDefinition[] stats;
        public ElementDefinition[] elements;
        public CardTemplate[] cards;
        public EnemyDefinition[] enemies;
        public FloorDefinition[] floors;

        public StatDefinition FindStat(StatId id)
        {
            if (stats == null) return null;
            foreach (StatDefinition definition in stats)
                if (definition != null && definition.id == id) return definition;
            return null;
        }

        public EnemyDefinition FindEnemy(EnemyArchetype archetype)
        {
            if (enemies == null) return null;
            foreach (EnemyDefinition definition in enemies)
                if (definition != null && definition.archetype == archetype) return definition;
            return null;
        }

        public ElementDefinition FindElement(ElementId id)
        {
            if (elements == null) return null;
            foreach (ElementDefinition definition in elements)
                if (definition != null && definition.id == id) return definition;
            return null;
        }
    }
}
