using UnityEngine;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class HudController : MonoBehaviour
    {
        public static HudController Instance { get; private set; }

        [SerializeField] private Text lifeText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text elementsText;
        [SerializeField] private Text floorText;
        [SerializeField] private Text keyText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private Text bossText;
        [SerializeField] private Slider bossSlider;

        private PlayerController player;

        private void Awake()
        {
            Instance = this;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (bossPanel != null) bossPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameSession.StateChanged += RefreshStatic;
        }

        private void OnDisable()
        {
            GameSession.StateChanged -= RefreshStatic;
        }

        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            RefreshStatic();
        }

        private void Update()
        {
            if (player == null) return;
            if (lifeText != null)
                lifeText.text = "VIDA  " + Mathf.Max(0, player.CurrentLife) + " / " +
                                (GameSession.Instance != null ? GameSession.Instance.State.stats.life : 5);
            if (staminaText != null)
                staminaText.text = "ENERGIA  " + Mathf.CeilToInt(player.CurrentStamina) + " / " +
                                   (GameSession.Instance != null ? GameSession.Instance.State.stats.stamina : 100);
        }

        public void BindPlayer(PlayerController value)
        {
            player = value;
        }

        public void RefreshStatic()
        {
            if (GameSession.Instance == null) return;
            RunState run = GameSession.Instance.State;
            RuntimeStats stats = run.stats;
            if (statsText != null)
                statsText.text =
                    "FUE " + stats.strength + "   VEL " + stats.speed + "   DEF " + stats.defense +
                    "   AGI " + stats.agility;
            if (elementsText != null)
                elementsText.text =
                    "<color=#45B9FF>AGUA " + run.elements.water + "</color>   " +
                    "<color=#FF6B36>FUEGO " + run.elements.fire + "</color>   " +
                    "<color=#67C96B>VEG " + run.elements.vegetation + "</color>";
            if (floorText != null) floorText.text = "PISO " + (run.currentFloor + 1) + " / 6";
            if (keyText != null) keyText.text = run.keyFloor >= 0 ? "LLAVE: LISTA" : "LLAVE: --";
            if (objectiveText != null)
                objectiveText.text = run.keyFloor >= 0
                    ? "Lleva la llave a la puerta"
                    : "Encuentra al robot que custodia la llave";
        }

        public void SetPause(bool value)
        {
            if (pausePanel != null) pausePanel.SetActive(value);
        }

        public void SetBoss(string label, float normalized)
        {
            if (bossPanel != null) bossPanel.SetActive(true);
            if (bossText != null) bossText.text = label;
            if (bossSlider != null) bossSlider.value = Mathf.Clamp01(normalized);
        }

        public void Configure(Text life, Text stamina, Text stats, Text elements, Text floor, Text key,
            Text objective, GameObject pause, GameObject boss, Text bossLabel, Slider bossHealth)
        {
            lifeText = life;
            staminaText = stamina;
            statsText = stats;
            elementsText = elements;
            floorText = floor;
            keyText = key;
            objectiveText = objective;
            pausePanel = pause;
            bossPanel = boss;
            bossText = bossLabel;
            bossSlider = bossHealth;
        }
    }
}
