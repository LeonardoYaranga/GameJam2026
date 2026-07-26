using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private string newGameScene = "01_CinematicIntro";
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Text volumeValue;
        [SerializeField] private Text fullscreenValue;

        private void Awake()
        {
            Time.timeScale = 1f;
            ClosePanels();
            RefreshOptions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanels();
        }

        public void NewGame()
        {
            Time.timeScale = 1f;
            GameSession.Ensure(catalog, true);
            SceneManager.LoadScene(newGameScene);
        }

        public void OpenOptions()
        {
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            RefreshOptions();
        }

        public void OpenCredits()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void ClosePanels()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void VolumeDown()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume - 0.1f);
            RefreshOptions();
        }

        public void VolumeUp()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 0.1f);
            RefreshOptions();
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshOptions();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Configure(GameCatalog gameCatalog, GameObject options, GameObject credits, Text volume,
            Text fullscreen, string scene)
        {
            catalog = gameCatalog;
            optionsPanel = options;
            creditsPanel = credits;
            volumeValue = volume;
            fullscreenValue = fullscreen;
            newGameScene = scene;
            ClosePanels();
            RefreshOptions();
        }

        private void RefreshOptions()
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
            if (fullscreenValue != null) fullscreenValue.text = Screen.fullScreen ? "ACTIVADA" : "DESACTIVADA";
        }
    }
}
