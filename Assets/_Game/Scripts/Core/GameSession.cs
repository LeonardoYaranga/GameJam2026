using System;
using UnityEngine;

namespace NidoCero
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private RunState state = new RunState();
        [SerializeField] private bool resetOnAwake;

        public static event Action StateChanged;

        public GameCatalog Catalog => catalog;
        public RunState State => state;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (state == null) state = new RunState();
            if (resetOnAwake || state.randomSeed == 0) ResetRun();
        }

        public void Configure(GameCatalog value, bool reset)
        {
            catalog = value;
            if (reset) ResetRun();
        }

        public void ResetRun()
        {
            if (state == null) state = new RunState();
            state.Reset(catalog);
            StateChanged?.Invoke();
        }

        public void NotifyChanged()
        {
            StateChanged?.Invoke();
        }

        public static void Ensure(GameCatalog catalog, bool reset)
        {
            if (Instance != null)
            {
                Instance.Configure(catalog, reset);
                return;
            }

            GameObject host = new GameObject("GameSession");
            GameSession session = host.AddComponent<GameSession>();
            session.Configure(catalog, reset);
        }
    }
}
