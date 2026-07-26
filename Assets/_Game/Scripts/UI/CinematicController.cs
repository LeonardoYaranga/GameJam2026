using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class CinematicController : MonoBehaviour
    {
        [SerializeField] private string[] lines;
        [SerializeField] private Text subtitle;
        [SerializeField] private Slider progress;
        [SerializeField] private string nextScene;
        [SerializeField] private float secondsPerLine = 3.2f;

        private float elapsed;
        private int currentLine = -1;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "01_CinematicIntro")
                GameAudio.Play(GameSfx.Rockfall, 0.62f);
            ShowLine(0);
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            int index = Mathf.FloorToInt(elapsed / Mathf.Max(0.5f, secondsPerLine));
            if (index != currentLine) ShowLine(index);
            float duration = Mathf.Max(1f, lines.Length * secondsPerLine);
            if (progress != null) progress.value = Mathf.Clamp01(elapsed / duration);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || elapsed >= duration)
                Continue();
        }

        private void ShowLine(int index)
        {
            currentLine = index;
            if (subtitle != null && lines != null && index >= 0 && index < lines.Length)
                subtitle.text = lines[index];
        }

        public void Continue()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrWhiteSpace(nextScene)) SceneManager.LoadScene(nextScene);
        }

        public void Configure(string[] content, Text target, Slider progressSlider, string scene, float pace)
        {
            lines = content;
            subtitle = target;
            progress = progressSlider;
            nextScene = scene;
            secondsPerLine = pace;
        }
    }
}
