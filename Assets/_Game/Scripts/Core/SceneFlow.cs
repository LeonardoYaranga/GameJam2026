using UnityEngine;
using UnityEngine.SceneManagement;

namespace NidoCero
{
    public sealed class SceneFlow : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private bool resetRun;

        public void Go()
        {
            Time.timeScale = 1f;
            if (catalog != null) GameSession.Ensure(catalog, resetRun);
            if (!string.IsNullOrWhiteSpace(targetScene)) SceneManager.LoadScene(targetScene);
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void Configure(string scene, GameCatalog gameCatalog, bool shouldReset)
        {
            targetScene = scene;
            catalog = gameCatalog;
            resetRun = shouldReset;
        }
    }
}
