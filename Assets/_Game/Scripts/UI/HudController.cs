using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class HudController : MonoBehaviour
    {
        public static HudController Instance { get; private set; }
        public static bool PauseActive => Instance != null && Instance.isPaused;

        [Header("Status")]
        [SerializeField] private Text lifeText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Image lifeFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image damageFlash;
        [SerializeField] private float damageFlashDuration = 0.24f;
        [SerializeField] private float damageFlashMaximumAlpha = 0.32f;

        [Header("Run information")]
        [SerializeField] private Text statsText;
        [SerializeField] private Text elementsText;
        [SerializeField] private Text[] statValues;
        [SerializeField] private Text[] elementValues;
        [SerializeField] private Text floorText;
        [SerializeField] private Text keyText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text timerText;

        [Header("Pause")]
        [SerializeField] private GameObject pausePanel;

        [Header("Boss")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private Text bossText;
        [SerializeField] private Slider bossSlider;

        private PlayerController player;
        private bool isPaused;
        private float runTime;
        private float damageFlashRemaining;

        public bool IsDamageFlashActive => damageFlashRemaining > 0f;

        private void Awake()
        {
            Instance = this;
            isPaused = false;
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
            UpdateDamageFlash();
            if (!isPaused && !CardChoiceController.IsOpen) runTime += Time.deltaTime;
            UpdateTimer();
            if (player == null) return;

            int maximumLife = GameSession.Instance != null ? GameSession.Instance.State.stats.life : 5;
            int maximumStamina = GameSession.Instance != null ? GameSession.Instance.State.stats.stamina : 100;
            if (lifeText != null)
                lifeText.text = Mathf.Max(0, player.CurrentLife) + " / " + maximumLife;
            if (staminaText != null)
                staminaText.text = Mathf.CeilToInt(player.CurrentStamina) + " / " + maximumStamina;
            if (lifeFill != null)
                lifeFill.fillAmount = Mathf.Clamp01(player.CurrentLife / Mathf.Max(1f, maximumLife));
            if (staminaFill != null)
                staminaFill.fillAmount = Mathf.Clamp01(player.CurrentStamina / Mathf.Max(1f, maximumStamina));
        }

        public void BindPlayer(PlayerController value)
        {
            player = value;
        }

        public void PlayDamageFlash()
        {
            if (damageFlash == null) return;
            damageFlashRemaining = Mathf.Max(0.01f, damageFlashDuration);
            damageFlash.gameObject.SetActive(true);
            damageFlash.transform.SetAsLastSibling();
            SetDamageFlashAlpha(damageFlashMaximumAlpha);
        }

        public void ConfigureDamageFlash(Image value)
        {
            damageFlash = value;
            if (damageFlash == null) return;
            damageFlash.raycastTarget = false;
            damageFlash.gameObject.SetActive(false);
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

            int[] statNumbers =
            {
                stats.strength, stats.defense, stats.life, stats.agility, stats.speed, stats.stamina
            };
            if (statValues != null)
                for (int i = 0; i < statValues.Length && i < statNumbers.Length; i++)
                    if (statValues[i] != null) statValues[i].text = statNumbers[i].ToString();

            int[] elementNumbers = { run.elements.fire, run.elements.water, run.elements.vegetation };
            if (elementValues != null)
                for (int i = 0; i < elementValues.Length && i < elementNumbers.Length; i++)
                    if (elementValues[i] != null) elementValues[i].text = elementNumbers[i].ToString();

            int sequence = Mathf.Clamp(run.currentFloor, 0, 3);
            int physicalFloor = 3 - sequence;
            if (floorText != null) floorText.text = "PISO " + physicalFloor;
            if (keyText != null)
                keyText.text = run.keyFloor >= 0
                    ? (sequence == 2 ? "LLAVE DORADA" : "MÓDULO LISTO")
                    : "SIN LLAVE";
            if (objectiveText != null)
            {
                FloorDefinition[] floors = GameSession.Instance.Catalog.floors;
                string objective = floors != null && sequence < floors.Length && floors[sequence] != null
                    ? floors[sequence].objective
                    : "Desciende hasta Núcleo Cero.";
                objectiveText.text = run.keyFloor >= 0
                    ? (sequence == 2
                        ? "Acceso concedido. La decisión sigue pendiente."
                        : "Lleva el módulo al acceso de descenso.")
                    : objective;
            }
        }

        public void TogglePause()
        {
            if (CardChoiceController.IsOpen) return;
            SetPause(!isPaused);
        }

        public void SetPause(bool value)
        {
            isPaused = value;
            if (pausePanel != null)
            {
                pausePanel.SetActive(value);
                if (value) pausePanel.transform.SetAsLastSibling();
            }
            Time.timeScale = value || CardChoiceController.IsOpen ? 0f : 1f;
        }

        public void RestartScene()
        {
            isPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            isPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("00_Launcher");
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

        public void ConfigureEnhanced(Text life, Text stamina, Image lifeBar, Image staminaBar,
            Text[] stats, Text[] elements, Text floor, Text key, Text objective, Text timer,
            GameObject pause, GameObject boss, Text bossLabel, Slider bossHealth)
        {
            lifeText = life;
            staminaText = stamina;
            lifeFill = lifeBar;
            staminaFill = staminaBar;
            statValues = stats;
            elementValues = elements;
            floorText = floor;
            keyText = key;
            objectiveText = objective;
            timerText = timer;
            pausePanel = pause;
            bossPanel = boss;
            bossText = bossLabel;
            bossSlider = bossHealth;
        }

        private void UpdateTimer()
        {
            if (timerText == null) return;
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(runTime));
            timerText.text = (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        }

        private void UpdateDamageFlash()
        {
            if (damageFlash == null) return;
            if (damageFlashRemaining <= 0f)
            {
                if (damageFlash.gameObject.activeSelf) damageFlash.gameObject.SetActive(false);
                return;
            }

            damageFlashRemaining = Mathf.Max(0f, damageFlashRemaining - Time.unscaledDeltaTime);
            float normalized = damageFlashDuration > 0f
                ? damageFlashRemaining / damageFlashDuration
                : 0f;
            SetDamageFlashAlpha(damageFlashMaximumAlpha * normalized);
            if (damageFlashRemaining <= 0f) damageFlash.gameObject.SetActive(false);
        }

        private void SetDamageFlashAlpha(float alpha)
        {
            damageFlash.color = new Color(0.78f, 0.02f, 0.025f, Mathf.Clamp01(alpha));
        }
    }
}
