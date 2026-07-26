using UnityEngine;

namespace NidoCero
{
    public static class ElementalResolver
    {
        public static readonly Color NeutralProjectileColor = new Color(1f, 1f, 1f, 1f);

        public static ElementId CounteredBy(ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return ElementId.Water;
                case ElementId.Vegetation: return ElementId.Fire;
                default: return ElementId.Vegetation;
            }
        }

        public static ElementId StrongAgainst(ElementId element)
        {
            switch (element)
            {
                case ElementId.Water: return ElementId.Fire;
                case ElementId.Fire: return ElementId.Vegetation;
                default: return ElementId.Water;
            }
        }

        public static int Bonus(ElementId attackElement, int attackerLevel, ElementLevels defender)
        {
            if (defender == null || attackerLevel <= 0) return 0;
            ElementId weakElement = StrongAgainst(attackElement);
            ElementId counterElement = CounteredBy(weakElement);
            int exposure = Mathf.Max(0, defender.Get(weakElement) - defender.Get(counterElement));
            return Mathf.Min(attackerLevel, exposure);
        }

        public static Color ProjectileColor(ElementId element, int level)
        {
            if (level <= 0) return NeutralProjectileColor;
            switch (element)
            {
                case ElementId.Fire:
                    return new Color(1f, 0.08f, 0.035f, 1f);
                case ElementId.Vegetation:
                    return new Color(0.08f, 0.9f, 0.18f, 1f);
                default:
                    return new Color(0.04f, 0.46f, 1f, 1f);
            }
        }

        public static float DamageMultiplier(ElementId attackElement, int attackLevel,
            ElementId defenderElement, int defenderLevel)
        {
            if (attackLevel <= 0) return 1f;

            int attacker = Mathf.Max(0, attackLevel);
            int defender = Mathf.Max(0, defenderLevel);
            int levelDifference = attacker - defender;
            if (StrongAgainst(attackElement) == defenderElement)
            {
                float advantage = 1.18f + attacker * 0.045f +
                                  Mathf.Clamp(levelDifference * 0.02f, -0.08f, 0.1f);
                return Mathf.Clamp(advantage, 1.1f, 1.55f);
            }

            if (StrongAgainst(defenderElement) == attackElement)
            {
                float resistance = 0.78f + attacker * 0.015f - defender * 0.025f;
                return Mathf.Clamp(resistance, 0.55f, 0.86f);
            }

            float sameElement = 0.9f + Mathf.Clamp(levelDifference * 0.025f, -0.1f, 0.12f);
            return Mathf.Clamp(sameElement, 0.8f, 1.02f);
        }

        public static int GetDominantElements(ElementLevels levels, ElementId[] results,
            out int dominantLevel)
        {
            dominantLevel = 0;
            if (levels == null || results == null || results.Length == 0) return 0;

            dominantLevel = Mathf.Max(levels.water, Mathf.Max(levels.fire, levels.vegetation));
            if (dominantLevel <= 0) return 0;

            int count = 0;
            for (int index = 0; index < 3 && count < results.Length; index++)
            {
                ElementId element = (ElementId)index;
                if (levels.Get(element) == dominantLevel)
                    results[count++] = element;
            }
            return count;
        }
    }
}
