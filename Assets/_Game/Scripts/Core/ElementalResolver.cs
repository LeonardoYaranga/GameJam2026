using UnityEngine;

namespace NidoCero
{
    public static class ElementalResolver
    {
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
    }
}
