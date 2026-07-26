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
            int effectiveElementLevel = Mathf.Max(
                Mathf.Max(0, attackLevel),
                attackerElements != null ? attackerElements.Get(attackElement) : 0);
            float baseDamage = PlayerOffensePower(attackerStats);
            float elemental = ElementalResolver.DamageMultiplier(
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
            int strength = defenderStats != null ? defenderStats.strength : 2;
            int speed = defenderStats != null ? defenderStats.speed : 5;
            int agility = defenderStats != null ? defenderStats.agility : 2;
            int stamina = defenderStats != null ? defenderStats.stamina : 100;
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
            float lifeFactor = Mathf.Clamp(Mathf.Sqrt(5f / Mathf.Max(1f, life)), 0.75f, 1.4f);
            float agilityFactor = 1f / (1f + Mathf.Max(0, agility) * 0.018f);
            float speedFactor = 1f / (1f + Mathf.Max(0, speed) * 0.012f);
            float strengthFactor = 1f / (1f + Mathf.Max(0, strength) * 0.008f);
            float staminaFactor = 1f /
                                  (1f + Mathf.Max(0, stamina - 50) * 0.0012f);
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
                rawDamage * defenseFactor * lifeFactor * agilityFactor * speedFactor *
                strengthFactor * staminaFactor * exposureFactor * elementalResistance,
                MinimumDamagePercent,
                MaximumRegularHitPercent));
        }

        public static float BossProjectileDamagePercent(RuntimeStats attackerStats,
            ElementLevels attackerElements, ElementId attackElement, int attackLevel,
            ElementId bossElement)
        {
            int effectiveElementLevel = Mathf.Max(
                Mathf.Max(0, attackLevel),
                attackerElements != null ? attackerElements.Get(attackElement) : 0);
            float baseDamage = PlayerOffensePower(attackerStats) * 0.72f;
            float elemental = ElementalResolver.DamageMultiplier(
                attackElement,
                effectiveElementLevel,
                bossElement,
                5);
            return RoundPercent(Mathf.Clamp(
                baseDamage * elemental,
                6f,
                MaximumBossHitPercent));
        }

        public static float RelayProjectileDamagePercent(RuntimeStats attackerStats,
            ElementLevels attackerElements, ElementId attackElement, int attackLevel,
            ElementId relayElement, int relayElementLevel)
        {
            int effectiveElementLevel = Mathf.Max(
                Mathf.Max(0, attackLevel),
                attackerElements != null ? attackerElements.Get(attackElement) : 0);
            float elemental = ElementalResolver.DamageMultiplier(
                attackElement,
                effectiveElementLevel,
                relayElement,
                relayElementLevel);
            return RoundPercent(Mathf.Clamp(
                PlayerOffensePower(attackerStats) * 0.9f * elemental,
                MinimumDamagePercent,
                MaximumRegularHitPercent));
        }

        public static float PlayerOffensePower(RuntimeStats stats)
        {
            RuntimeStats value = stats ?? new RuntimeStats();
            return 8.5f +
                   Mathf.Max(0, value.strength) * 1.65f +
                   Mathf.Max(0, value.speed) * 0.3f +
                   Mathf.Max(0, value.defense) * 0.18f +
                   Mathf.Max(0, value.agility) * 0.55f +
                   Mathf.Max(0, value.life) * 0.15f +
                   Mathf.Max(0, value.stamina - 50) * 0.02f;
        }

        public static int HitsToDefeat(float damagePercent)
        {
            return damagePercent <= 0f
                ? int.MaxValue
                : Mathf.CeilToInt(100f / damagePercent);
        }

        private static float ArchetypeArmor(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Tank: return 0.78f;
                case EnemyArchetype.Walker: return 0.92f;
                default: return 1f;
            }
        }

        private static float RoundPercent(float value)
        {
            return Mathf.Round(value * 10f) / 10f;
        }
    }
}
