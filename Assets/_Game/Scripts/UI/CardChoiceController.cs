using System;
using System.Collections.Generic;
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
        [SerializeField] private Renderer[] cardPlanes;
        [SerializeField] private Text[] titles;
        [SerializeField] private Text[] descriptions;
        [SerializeField] private Text[] values;

        private readonly CardTemplate[] presented = new CardTemplate[3];
        private string currentChoiceId;

        private void Awake()
        {
            Instance = this;
            SetVisible(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Choose(2);
        }

        public void Open(string choiceId)
        {
            if (IsOpen || GameSession.Instance == null || GameSession.Instance.Catalog == null) return;
            RunState state = GameSession.Instance.State;
            if (state.resolvedChoices.Contains(choiceId)) return;

            List<CardTemplate> available = new List<CardTemplate>();
            foreach (CardTemplate card in GameSession.Instance.Catalog.cards)
                if (card != null && !state.retiredCards.Contains(card.cardId)) available.Add(card);

            if (available.Count < 3) return;

            int seed = state.randomSeed ^ choiceId.GetHashCode();
            System.Random random = new System.Random(seed);
            for (int i = available.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                CardTemplate temp = available[i];
                available[i] = available[swap];
                available[swap] = temp;
            }

            currentChoiceId = choiceId;
            for (int i = 0; i < 3; i++)
            {
                presented[i] = available[i];
                Populate(i, available[i]);
            }

            IsOpen = true;
            Time.timeScale = 0f;
            SetVisible(true);
        }

        private void Populate(int index, CardTemplate card)
        {
            if (titles != null && index < titles.Length && titles[index] != null)
                titles[index].text = (index + 1) + " — " + card.title;
            if (descriptions != null && index < descriptions.Length && descriptions[index] != null)
                descriptions[index].text = card.description;
            if (values != null && index < values.Length && values[index] != null)
                values[index].text =
                    "+" + card.gainAmount + " " + DisplayStat(card.gainStat) +
                    "\n−" + Mathf.Abs(card.lossAmount) + " " + DisplayStat(card.lossStat) +
                    "\n+" + card.elementAmount + " " + DisplayElement(card.element);
            if (cardPlanes != null && index < cardPlanes.Length && cardPlanes[index] != null)
                cardPlanes[index].material.color = card.accent;
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

        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= presented.Length || presented[index] == null ||
                GameSession.Instance == null) return;

            RunState state = GameSession.Instance.State;
            state.ApplyCard(GameSession.Instance.Catalog, presented[index]);
            for (int i = 0; i < presented.Length; i++)
            {
                CardTemplate card = presented[i];
                if (card != null && !state.retiredCards.Contains(card.cardId))
                    state.retiredCards.Add(card.cardId);
                presented[i] = null;
            }
            if (!state.resolvedChoices.Contains(currentChoiceId)) state.resolvedChoices.Add(currentChoiceId);
            GameSession.Instance.NotifyChanged();

            IsOpen = false;
            Time.timeScale = 1f;
            SetVisible(false);
        }

        private void SetVisible(bool value)
        {
            if (overlay != null) overlay.SetActive(value);
            if (cardObjects != null)
                foreach (GameObject card in cardObjects)
                    if (card != null) card.SetActive(value);
        }

        public void Configure(GameObject overlayObject, GameObject[] objects, Renderer[] planes,
            Text[] titleTexts, Text[] descriptionTexts, Text[] valueTexts)
        {
            overlay = overlayObject;
            cardObjects = objects;
            cardPlanes = planes;
            titles = titleTexts;
            descriptions = descriptionTexts;
            values = valueTexts;
        }
    }

}
