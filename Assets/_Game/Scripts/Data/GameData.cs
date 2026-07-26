using System;
using System.Collections.Generic;
using UnityEngine;

namespace NidoCero
{
    public enum StatId
    {
        Strength,
        Speed,
        Defense,
        Agility,
        Life,
        Stamina
    }

    public enum ElementId
    {
        Water,
        Fire,
        Vegetation
    }

    public enum EnemyArchetype
    {
        Walker,
        Flyer,
        Tank
    }

    [Serializable]
    public sealed class RuntimeStats
    {
        public int strength = 2;
        public int speed = 5;
        public int defense = 1;
        public int agility = 2;
        public int life = 5;
        public int stamina = 100;

        public int Get(StatId id)
        {
            switch (id)
            {
                case StatId.Strength: return strength;
                case StatId.Speed: return speed;
                case StatId.Defense: return defense;
                case StatId.Agility: return agility;
                case StatId.Life: return life;
                case StatId.Stamina: return stamina;
                default: return 0;
            }
        }

        public void Set(StatId id, int value)
        {
            switch (id)
            {
                case StatId.Strength: strength = value; break;
                case StatId.Speed: speed = value; break;
                case StatId.Defense: defense = value; break;
                case StatId.Agility: agility = value; break;
                case StatId.Life: life = value; break;
                case StatId.Stamina: stamina = value; break;
            }
        }

        public void ResetToCatalog(GameCatalog catalog)
        {
            if (catalog == null || catalog.stats == null) return;
            foreach (StatDefinition definition in catalog.stats)
                if (definition != null) Set(definition.id, definition.defaultValue);
        }

        public int ApplyDelta(GameCatalog catalog, StatId id, int delta)
        {
            StatDefinition definition = catalog != null ? catalog.FindStat(id) : null;
            int minimum = definition != null ? definition.minimum : 0;
            int maximum = definition != null ? definition.maximum : 999;
            int next = Mathf.Clamp(Get(id) + delta, minimum, maximum);
            Set(id, next);
            return next;
        }
    }

    [Serializable]
    public sealed class ElementLevels
    {
        public int water;
        public int fire;
        public int vegetation;

        public int Get(ElementId id)
        {
            switch (id)
            {
                case ElementId.Water: return water;
                case ElementId.Fire: return fire;
                case ElementId.Vegetation: return vegetation;
                default: return 0;
            }
        }

        public void Add(ElementId id, int amount)
        {
            switch (id)
            {
                case ElementId.Water: water = Mathf.Max(0, water + amount); break;
                case ElementId.Fire: fire = Mathf.Max(0, fire + amount); break;
                case ElementId.Vegetation: vegetation = Mathf.Max(0, vegetation + amount); break;
            }
        }
    }

    [Serializable]
    public sealed class RunState
    {
        public RuntimeStats stats = new RuntimeStats();
        public ElementLevels elements = new ElementLevels();
        public int currentFloor;
        public int keyFloor = -1;
        public int randomSeed;
        public List<string> defeatedEnemies = new List<string>();
        public List<string> resolvedChoices = new List<string>();
        public List<string> retiredCards = new List<string>();
        public List<int> openedGates = new List<int>();

        public void Reset(GameCatalog catalog)
        {
            stats = new RuntimeStats();
            stats.ResetToCatalog(catalog);
            elements = new ElementLevels();
            currentFloor = 0;
            keyFloor = -1;
            randomSeed = UnityEngine.Random.Range(1000, 999999);
            defeatedEnemies.Clear();
            resolvedChoices.Clear();
            retiredCards.Clear();
            openedGates.Clear();
        }

        public void ApplyCard(GameCatalog catalog, CardTemplate card)
        {
            if (card == null) return;
            stats.ApplyDelta(catalog, card.gainStat, card.gainAmount);
            stats.ApplyDelta(catalog, card.lossStat, -Mathf.Abs(card.lossAmount));
            elements.Add(card.element, Mathf.Max(0, card.elementAmount));
        }

        public void ApplyOffer(GameCatalog catalog, RuntimeCardOffer offer)
        {
            if (offer == null) return;
            for (int i = 0; i < 3; i++)
                stats.ApplyDelta(catalog, offer.GetStat(i), offer.GetDelta(i));
            elements.Add(offer.element, Mathf.Max(0, offer.elementAmount));
        }
    }
}
