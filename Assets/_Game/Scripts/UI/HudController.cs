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
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Text notificationText;
        [SerializeField] private float notificationDuration = 2.4f;

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
        private float notificationRemaining;

        public bool IsDamageFlashActive => damageFlashRemaining > 0f;
        public string LastNotification { get; private set; }

        private void Awake()
        {
            Instance = this;
            isPaused = false;
            EnsureNotification();
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
            UpdateNotification();
            if (!isPaused && !CardChoiceController.IsOpen && !FinalSacrificeController.IsOpen)
                runTime += Time.deltaTime;
            UpdateTimer();
            if (player == null) return;

            int maximumLife = GameSession.Instance != null ? GameSession.Instance.State.stats.life : 5;
            int maximumStamina = GameSession.Instance != null ? GameSession.Instance.State.stats.stamina : 100;
            if (lifeText != null)
                lifeText.text = Mathf.Max(0, player.CurrentLife) + " / " + maximumLife;
            if (staminaText != null)
                staminaText.text = Mathf.CeilToInt(player.CurrentStamina) + " / " + maximumStamina;
            SetHorizontalFill(lifeFill, player.CurrentLifeNormalized);
            SetHorizontalFill(staminaFill, player.CurrentStaminaNormalized);
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

        public void ShowNotification(string message, float duration = -1f)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            EnsureNotification();
            LastNotification = message;
            notificationRemaining = duration > 0f ? duration : notificationDuration;
            if (notificationText != null) notificationText.text = message;
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(true);
                notificationPanel.transform.SetAsLastSibling();
            }
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
                if (sequence == 0 && run.openedGates.Contains(0))
                    objectiveText.text = "Paso abierto. Desciende al Piso 2.";
                else if (sequence == 0 && run.resolvedChoices.Contains("george_first_choice") &&
                         !run.storyFlags.Contains(WiseTurtleInteraction.MissionStoryFlag))
                    objectiveText.text = "Habla con la Tortuga Sabia [E].";
                else
                    objectiveText.text = run.keyFloor >= 0
                        ? (sequence == 2
                            ? "Acceso concedido. La decisión sigue pendiente."
                            : "Lleva el módulo al acceso de descenso.")
                        : objective;
            }
        }

        public void TogglePause()
        {
            if (CardChoiceController.IsOpen || DialogueController.IsOpen ||
                FinalSacrificeController.IsOpen) return;
            GameAudio.Play(GameSfx.MenuClick, 0.48f);
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
            Time.timeScale = value || CardChoiceController.IsOpen ||
                             FinalSacrificeController.IsOpen ? 0f : 1f;
        }

        public void RestartScene()
        {
            GameAudio.Play(GameSfx.MenuClick, 0.48f);
            isPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            GameAudio.Play(GameSfx.MenuClick, 0.48f);
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

        private void UpdateNotification()
        {
            if (notificationPanel == null || !notificationPanel.activeSelf) return;
            notificationRemaining = Mathf.Max(0f,
                notificationRemaining - Time.unscaledDeltaTime);
            if (notificationRemaining <= 0f) notificationPanel.SetActive(false);
        }

        private void EnsureNotification()
        {
            if (notificationPanel != null && notificationText != null) return;
            Transform existing = transform.Find("HUDNotification");
            if (existing != null)
            {
                notificationPanel = existing.gameObject;
                notificationText = existing.GetComponentInChildren<Text>(true);
                return;
            }

            notificationPanel = new GameObject(
                "HUDNotification",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            notificationPanel.transform.SetParent(transform, false);
            RectTransform panelRect = notificationPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.27f, 0.745f);
            panelRect.anchorMax = new Vector2(0.73f, 0.805f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image background = notificationPanel.GetComponent<Image>();
            background.color = new Color(0.015f, 0.025f, 0.03f, 0.9f);
            background.raycastTarget = false;

            GameObject label = new GameObject(
                "NotificationText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            label.transform.SetParent(notificationPanel.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 2f);
            labelRect.offsetMax = new Vector2(-12f, -2f);
            notificationText = label.GetComponent<Text>();
            notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            notificationText.fontSize = 15;
            notificationText.alignment = TextAnchor.MiddleCenter;
            notificationText.color = new Color(0.92f, 0.96f, 0.95f);
            notificationText.raycastTarget = false;
            notificationPanel.SetActive(false);
        }

        private void SetDamageFlashAlpha(float alpha)
        {
            damageFlash.color = new Color(0.78f, 0.02f, 0.025f, Mathf.Clamp01(alpha));
        }

        private static void SetHorizontalFill(Image image, float normalized)
        {
            if (image == null) return;
            float value = Mathf.Clamp01(normalized);
            image.fillAmount = value;

            // Images without a sprite ignore Image.fillAmount and render their full rectangle.
            // Adjusting the right anchor keeps the bars functional with the generated HUD assets.
            RectTransform rect = image.rectTransform;
            Vector2 maximum = rect.anchorMax;
            maximum.x = Mathf.Lerp(rect.anchorMin.x, 1f, value);
            rect.anchorMax = maximum;
        }
    }
}
