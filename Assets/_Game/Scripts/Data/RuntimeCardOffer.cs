using System;
using System.Collections.Generic;

namespace NidoCero
{
    [Serializable]
    public sealed class RuntimeCardOffer
    {
        public string sourceCardId;
        public string title;
        public string description;
        public ElementId element;
        public int elementAmount = 1;
        public StatId positiveStatA;
        public int positiveAmountA;
        public StatId positiveStatB;
        public int positiveAmountB;
        public StatId negativeStat;
        public int negativeAmount;

        public StatId GetStat(int index)
        {
            switch (index)
            {
                case 0: return positiveStatA;
                case 1: return positiveStatB;
                default: return negativeStat;
            }
        }

        public int GetDelta(int index)
        {
            switch (index)
            {
                case 0: return Math.Abs(positiveAmountA);
                case 1: return Math.Abs(positiveAmountB);
                default: return -Math.Abs(negativeAmount);
            }
        }
    }

    public static class CardOfferGenerator
    {
        private static readonly string[] WaterNames =
        {
            "Marea creciente", "Escudo de agua", "Torrente vital", "Pulso abisal", "Lluvia azul"
        };

        private static readonly string[] FireNames =
        {
            "Llama ascendente", "Furia ígnea", "Calor del núcleo", "Brasa viva", "Impulso solar"
        };

        private static readonly string[] VegetationNames =
        {
            "Abrazo de Gaia", "Raíces vitales", "Manto verde", "Brote ancestral", "Espíritu del bosque"
        };

        public static RuntimeCardOffer Generate(GameCatalog catalog, RunState state, ElementId element, int seed)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (state == null) throw new ArgumentNullException(nameof(state));

            System.Random random = new System.Random(seed ^ (((int)element + 1) * 7919));
            List<StatId> positiveCandidates = BuildCandidates(catalog, state, true);
            List<StatId> negativeCandidates = BuildCandidates(catalog, state, false);
            Shuffle(positiveCandidates, random);
            Shuffle(negativeCandidates, random);

            StatId positiveA = TakeCandidate(positiveCandidates, null, StatId.Strength);
            StatId positiveB = TakeCandidate(positiveCandidates, new[] { positiveA }, StatId.Speed);
            StatId negative = TakeCandidate(negativeCandidates, new[] { positiveA, positiveB }, StatId.Defense);

            CardTemplate template = PickTemplate(catalog, state, element, random);
            RuntimeCardOffer offer = new RuntimeCardOffer
            {
                sourceCardId = template != null ? template.cardId : "runtime_" + element + "_" + seed,
                title = template != null && !string.IsNullOrWhiteSpace(template.title)
                    ? template.title
                    : PickElementName(element, random),
                description = "Dos mejoras exigen una renuncia. Las opciones rechazadas desaparecen.",
                element = element,
                elementAmount = 1,
                positiveStatA = positiveA,
                positiveAmountA = RollAmount(catalog, state, positiveA, true, random),
                positiveStatB = positiveB,
                positiveAmountB = RollAmount(catalog, state, positiveB, true, random),
                negativeStat = negative,
                negativeAmount = RollAmount(catalog, state, negative, false, random)
            };

            return offer;
        }

        public static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (value == null) return hash;
                for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
        }

        private static List<StatId> BuildCandidates(GameCatalog catalog, RunState state, bool positive)
        {
            List<StatId> result = new List<StatId>();
            foreach (StatId stat in Enum.GetValues(typeof(StatId)))
            {
                StatDefinition definition = catalog.FindStat(stat);
                if (definition == null)
                {
                    result.Add(stat);
                    continue;
                }

                int value = state.stats.Get(stat);
                if (positive ? value < definition.maximum : value > definition.minimum)
                    result.Add(stat);
            }

            if (result.Count == 0)
                foreach (StatId stat in Enum.GetValues(typeof(StatId))) result.Add(stat);
            return result;
        }

        private static StatId TakeCandidate(List<StatId> candidates, StatId[] excluded, StatId fallback)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                StatId candidate = candidates[i];
                bool isExcluded = false;
                if (excluded != null)
                    for (int j = 0; j < excluded.Length; j++)
                        if (candidate == excluded[j]) isExcluded = true;
                if (!isExcluded) return candidate;
            }

            foreach (StatId stat in Enum.GetValues(typeof(StatId)))
            {
                bool isExcluded = false;
                if (excluded != null)
                    for (int j = 0; j < excluded.Length; j++)
                        if (stat == excluded[j]) isExcluded = true;
                if (!isExcluded) return stat;
            }

            return fallback;
        }

        private static int RollAmount(GameCatalog catalog, RunState state, StatId stat, bool positive,
            System.Random random)
        {
            int raw;
            switch (stat)
            {
                case StatId.Stamina:
                    raw = (positive ? random.Next(2, 5) : random.Next(1, 4)) * 5;
                    break;
                case StatId.Life:
                case StatId.Strength:
                case StatId.Agility:
                    raw = positive ? random.Next(1, 3) : 1;
                    break;
                default:
                    raw = 1;
                    break;
            }

            StatDefinition definition = catalog.FindStat(stat);
            if (definition == null) return Math.Max(1, raw);
            int current = state.stats.Get(stat);
            int room = positive ? definition.maximum - current : current - definition.minimum;
            return Math.Max(1, Math.Min(raw, Math.Max(1, room)));
        }

        private static CardTemplate PickTemplate(GameCatalog catalog, RunState state, ElementId element,
            System.Random random)
        {
            List<CardTemplate> matching = new List<CardTemplate>();
            if (catalog.cards != null)
                foreach (CardTemplate card in catalog.cards)
                    if (card != null && card.element == element && !state.retiredCards.Contains(card.cardId))
                        matching.Add(card);
            return matching.Count == 0 ? null : matching[random.Next(matching.Count)];
        }

        private static string PickElementName(ElementId element, System.Random random)
        {
            string[] names;
            switch (element)
            {
                case ElementId.Fire:
                    names = FireNames;
                    break;
                case ElementId.Vegetation:
                    names = VegetationNames;
                    break;
                default:
                    names = WaterNames;
                    break;
            }

            return names[random.Next(names.Length)];
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                T value = list[i];
                list[i] = list[swap];
                list[swap] = value;
            }
        }
    }
}
