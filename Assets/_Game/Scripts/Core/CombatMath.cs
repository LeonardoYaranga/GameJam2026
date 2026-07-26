using UnityEngine;

namespace NidoCero
{
    public static class CombatMath
    {
        public const float MinimumDamagePercent = 8f;
        public const float MaximumRegularHitPercent = 34f;
        public const float MaximumBossHitPercent = 25f;

        public static float EnemyProjectileDamagePercent(RuntimeStats attackerStats,
            ElementLevels attackerElements, ElementId attackElement, int attackLevel,
            EnemyDefinition defender)
        {
            int strength = attackerStats != null ? attackerStats.strength : 2;
            int effectiveElementLevel = Mathf.Max(
                Mathf.Max(0, attackLevel),
                attackerElements != null ? attackerElements.Get(attackElement) : 0);
            float baseDamage = 15f + Mathf.Clamp(strength, 1, 8) * 2f;
            float elemental = ElementalMultiplier(
                attackElement,
                effectiveElementLevel,
                defender != null ? defender.element : ElementId.Fire,
                defender != null ? Mathf.Max(0, defender.elementLevel) : 0);
            float armor = ArchetypeArmor(defender != null
                ? defender.archetype
                : EnemyArchetype.Walker);

            return RoundPercent(Mathf.Clamp(
                baseDamage * elemental * armor,
                MinimumDamagePercent,
                MaximumRegularHitPercent));
        }

        public static float PlayerIncomingDamagePercent(RuntimeStats defenderStats,
            ElementLevels defenderElements, ElementId attackElement, int attackLevel,
            EnemyArchetype attackerArchetype, bool contact)
        {
            int defense = defenderStats != null ? defenderStats.defense : 1;
            int life = defenderStats != null ? defenderStats.life : 5;
            float archetypePower;
            switch (attackerArchetype)
            {
                case EnemyArchetype.Tank:
                    archetypePower = 7f;
                    break;
                case EnemyArchetype.Walker:
                    archetypePower = 4f;
                    break;
                default:
                    archetypePower = 2f;
                    break;
            }

            float rawDamage = (contact ? 25f : 20f) +
                              Mathf.Max(0, attackLevel) * 2f +
                              archetypePower;
            float defenseFactor = 1f / (1f + Mathf.Max(0, defense) * 0.12f);
            float lifeFactor = Mathf.Clamp(5f / Mathf.Max(1f, life), 0.65f, 1.4f);
            int exposure = ElementalResolver.Bonus(
                attackElement,
                Mathf.Max(0, attackLevel),
                defenderElements);
            float exposureFactor = 1f + exposure * 0.05f;
            int counterLevel = defenderElements != null
                ? defenderElements.Get(ElementalResolver.CounteredBy(attackElement))
                : 0;
            float elementalResistance = 1f - Mathf.Min(0.35f, counterLevel * 0.05f);

            return RoundPercent(Mathf.Clamp(
                rawDamage * defenseFactor * lifeFactor * exposureFactor * elementalResistance,
                MinimumDamagePercent,
                MaximumRegularHitPercent));
        }

        public static float BossProjectileDamagePercent(RuntimeStats attackerStats,
            ElementLevels attackerElements, ElementId attackElement, int attackLevel,
            ElementId bossElement)
        {
            int strength = attackerStats != null ? attackerStats.strength : 2;
            int effectiveElementLevel = Mathf.Max(
                Mathf.Max(0, attackLevel),
                attackerElements != null ? attackerElements.Get(attackElement) : 0);
            float baseDamage = 10f + Mathf.Clamp(strength, 1, 8) * 1.5f;
            float elemental = ElementalMultiplier(
                attackElement,
                effectiveElementLevel,
                bossElement,
                5);
            return RoundPercent(Mathf.Clamp(
                baseDamage * elemental,
                6f,
                MaximumBossHitPercent));
        }

        private static float ElementalMultiplier(ElementId attackElement, int attackLevel,
            ElementId defenderElement, int defenderLevel)
        {
            ElementLevels defender = new ElementLevels();
            defender.Add(defenderElement, defenderLevel);
            int exposure = ElementalResolver.Bonus(attackElement, attackLevel, defender);
            bool hasAdvantage = ElementalResolver.StrongAgainst(attackElement) == defenderElement;
            bool isResisted = ElementalResolver.StrongAgainst(defenderElement) == attackElement;

            if (hasAdvantage)
                return 1.12f + attackLevel * 0.05f + exposure * 0.04f;
            if (isResisted)
                return Mathf.Min(1f, 0.78f + attackLevel * 0.025f);
            return 1f + attackLevel * 0.025f;
        }

        private static float ArchetypeArmor(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Tank: return 0.72f;
                case EnemyArchetype.Walker: return 0.9f;
                default: return 1f;
            }
        }

        private static float RoundPercent(float value)
        {
            return Mathf.Round(value * 10f) / 10f;
        }
    }
}
