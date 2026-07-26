using UnityEngine;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class CardChoiceController : MonoBehaviour
    {
        public static CardChoiceController Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private GameObject overlay;
        [SerializeField] private GameObject[] cardObjects;
        [SerializeField] private Image[] cardBackgrounds;
        [SerializeField] private Image[] elementIcons;
        [SerializeField] private Text[] elementLabels;
        [SerializeField] private Text[] titles;
        [SerializeField] private Text[] descriptions;
        [SerializeField] private Image[] modifierIcons;
        [SerializeField] private Text[] modifierTexts;
        [SerializeField] private Sprite[] elementSprites;
        [SerializeField] private Sprite[] statSprites;

        private readonly RuntimeCardOffer[] presented = new RuntimeCardOffer[3];
        private string currentChoiceId;

        private void Awake()
        {
            Instance = this;
            IsOpen = false;
            SetVisible(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Choose(2);
        }

        public bool Open(string choiceId)
        {
            if (IsOpen || GameSession.Instance == null || GameSession.Instance.Catalog == null) return false;
            RunState state = GameSession.Instance.State;
            if (state.resolvedChoices.Contains(choiceId)) return false;

            currentChoiceId = choiceId;
            int seed = state.randomSeed ^ CardOfferGenerator.StableHash(choiceId);
            ElementId[] elements = { ElementId.Water, ElementId.Fire, ElementId.Vegetation };
            for (int i = 0; i < elements.Length; i++)
            {
                presented[i] = CardOfferGenerator.Generate(GameSession.Instance.Catalog, state, elements[i],
                    seed + i * 104729);
                Populate(i, presented[i]);
            }

            IsOpen = true;
            Time.timeScale = 0f;
            if (overlay != null) overlay.transform.SetAsLastSibling();
            SetVisible(true);
            return true;
        }

        private void Populate(int index, RuntimeCardOffer offer)
        {
            if (titles != null && index < titles.Length && titles[index] != null)
                titles[index].text = offer.title.ToUpperInvariant();
            if (descriptions != null && index < descriptions.Length && descriptions[index] != null)
                descriptions[index].text = "DOS MEJORAS · UN COSTO";
            if (elementLabels != null && index < elementLabels.Length && elementLabels[index] != null)
            {
                elementLabels[index].text = DisplayElement(offer.element).ToUpperInvariant();
                elementLabels[index].color = ElementColor(offer.element);
            }

            int elementIndex = (int)offer.element;
            if (cardBackgrounds != null && index < cardBackgrounds.Length && cardBackgrounds[index] != null &&
                elementSprites != null && elementSprites.Length >= 6)
                cardBackgrounds[index].sprite = elementSprites[3 + elementIndex];
            if (elementIcons != null && index < elementIcons.Length && elementIcons[index] != null &&
                elementSprites != null && elementIndex < elementSprites.Length)
                elementIcons[index].sprite = elementSprites[elementIndex];

            for (int row = 0; row < 3; row++)
            {
                int flatIndex = index * 3 + row;
                StatId stat = offer.GetStat(row);
                int delta = offer.GetDelta(row);
                if (modifierTexts != null && flatIndex < modifierTexts.Length && modifierTexts[flatIndex] != null)
                {
                    modifierTexts[flatIndex].text =
                        (delta > 0 ? "+" : "−") + Mathf.Abs(delta) + " " + DisplayStat(stat).ToUpperInvariant();
                    modifierTexts[flatIndex].color = delta > 0
                        ? new Color(0.42f, 1f, 0.48f)
                        : new Color(1f, 0.32f, 0.3f);
                }

                if (modifierIcons != null && flatIndex < modifierIcons.Length && modifierIcons[flatIndex] != null &&
                    statSprites != null && (int)stat < statSprites.Length)
                    modifierIcons[flatIndex].sprite = statSprites[(int)stat];
            }
        }

        private static string DisplayStat(StatId stat)
        {
            GameCatalog catalog = GameSession.Instance != null ? GameSession.Instance.Catalog : null;
            StatDefinition definition = catalog != null ? catalog.FindStat(stat) : null;
            return definition != null ? definition.displayName : stat.ToString();
        }

        private static string DisplayElement(ElementId element)
        {
            GameCatalog catalog = GameSession.Instance != null ? GameSession.Instance.Catalog : null;
            ElementDefinition definition = catalog != null ? catalog.FindElement(element) : null;
            return definition != null ? definition.displayName : element.ToString();
        }

        private static Color ElementColor(ElementId element)
        {
            GameCatalog catalog = GameSession.Instance != null ? GameSession.Instance.Catalog : null;
            ElementDefinition definition = catalog != null ? catalog.FindElement(element) : null;
            return definition != null ? definition.color : Color.white;
        }

        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= presented.Length || presented[index] == null ||
                GameSession.Instance == null) return;

            RunState state = GameSession.Instance.State;
            int previousLife = state.stats.life;
            int previousStamina = state.stats.stamina;
            state.ApplyOffer(GameSession.Instance.Catalog, presented[index]);
            if (!state.resolvedChoices.Contains(currentChoiceId)) state.resolvedChoices.Add(currentChoiceId);
            for (int i = 0; i < presented.Length; i++)
            {
                if (i == index || presented[i] == null || string.IsNullOrWhiteSpace(presented[i].sourceCardId))
                    continue;
                if (!state.retiredCards.Contains(presented[i].sourceCardId))
                    state.retiredCards.Add(presented[i].sourceCardId);
            }
            for (int i = 0; i < presented.Length; i++) presented[i] = null;
            GameSession.Instance.NotifyChanged();

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.RefreshVitalsAfterStatsChanged(previousLife, previousStamina);

            IsOpen = false;
            Time.timeScale = HudController.PauseActive ? 0f : 1f;
            SetVisible(false);
        }

        public RuntimeCardOffer GetPresentedOffer(int index)
        {
            return index >= 0 && index < presented.Length ? presented[index] : null;
        }

        private void SetVisible(bool value)
        {
            if (overlay != null) overlay.SetActive(value);
            if (cardObjects == null) return;
            foreach (GameObject card in cardObjects)
                if (card != null) card.SetActive(value);
        }

        public void Configure(GameObject overlayObject, GameObject[] objects, Image[] backgrounds,
            Image[] elementImages, Text[] elementTexts, Text[] titleTexts, Text[] descriptionTexts,
            Image[] statImages, Text[] statTexts, Sprite[] elements, Sprite[] stats)
        {
            overlay = overlayObject;
            cardObjects = objects;
            cardBackgrounds = backgrounds;
            elementIcons = elementImages;
            elementLabels = elementTexts;
            titles = titleTexts;
            descriptions = descriptionTexts;
            modifierIcons = statImages;
            modifierTexts = statTexts;
            elementSprites = elements;
            statSprites = stats;
        }

        public void Configure(GameObject overlayObject, GameObject[] objects, Renderer[] unusedPlanes,
            Text[] titleTexts, Text[] descriptionTexts, Text[] legacyValueTexts)
        {
            overlay = overlayObject;
            cardObjects = objects;
            titles = titleTexts;
            descriptions = descriptionTexts;
            modifierTexts = legacyValueTexts;
        }
    }
}
