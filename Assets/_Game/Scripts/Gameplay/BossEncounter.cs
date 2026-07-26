using UnityEngine;
using UnityEngine.SceneManagement;

namespace NidoCero
{
    public sealed class BossEncounter : MonoBehaviour
    {
        public static BossEncounter Instance { get; private set; }

        [SerializeField] private BossRelay[] relays;
        [SerializeField] private BossCore core;
        [SerializeField] private TextMesh statusText;
        private bool engaged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Refresh();
        }

        public void RelayDestroyed()
        {
            Refresh();
        }

        private void Refresh()
        {
            int remaining = 0;
            if (relays != null)
                foreach (BossRelay relay in relays)
                    if (relay != null && relay.IsAlive) remaining++;

            bool vulnerable = remaining == 0;
            if (core != null) core.SetVulnerable(vulnerable);
            if (statusText != null)
                statusText.text = vulnerable
                    ? "NÚCLEO EXPUESTO — " + (core != null ? core.ElementLabel : string.Empty)
                    : "RELÉS ACTIVOS: " + remaining;
            if (engaged)
                HudController.Instance?.SetBoss(
                    vulnerable
                        ? "IA CENTRAL — NÚCLEO " + (core != null ? core.ElementLabel : string.Empty)
                        : "IA CENTRAL — RELÉS",
                    vulnerable ? 1f : (3 - remaining) / 3f);
        }

        public void ElementChanged()
        {
            Refresh();
        }

        public void Begin()
        {
            engaged = true;
            HudController.Instance?.ShowNotification("Rompe los tres relés.");
            Refresh();
        }

        public void CoreDestroyed()
        {
            Time.timeScale = 0f;
            if (FinalSacrificeController.Instance != null &&
                FinalSacrificeController.Instance.Begin(LoadEnding))
                return;
            LoadEnding();
        }

        public void Configure(BossRelay[] values, BossCore value, TextMesh text)
        {
            relays = values;
            core = value;
            statusText = text;
        }

        private static void LoadEnding()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("03_CinematicEnd");
        }
    }
}
