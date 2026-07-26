using System;
using UnityEngine;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class DialogueController : MonoBehaviour
    {
        public static DialogueController Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private GameObject overlay;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text continueText;

        private string[] lines;
        private int lineIndex;
        private int openedFrame;
        private float previousTimeScale = 1f;
        private Action completed;

        public int CurrentLineIndex => lineIndex;
        public int LineCount => lines != null ? lines.Length : 0;

        private void Awake()
        {
            Instance = this;
            IsOpen = false;
            if (overlay != null) overlay.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= openedFrame) return;
            if (Input.GetKeyDown(KeyCode.E) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetMouseButtonDown(0))
                Advance();
        }

        public bool Open(string speaker, string[] dialogueLines, Action onCompleted)
        {
            if (IsOpen || CardChoiceController.IsOpen || FinalSacrificeController.IsOpen ||
                dialogueLines == null || dialogueLines.Length == 0)
                return false;

            lines = dialogueLines;
            lineIndex = 0;
            completed = onCompleted;
            previousTimeScale = Time.timeScale;
            openedFrame = Time.frameCount;
            if (speakerText != null) speakerText.text = speaker;
            if (bodyText != null) bodyText.text = lines[0];
            if (continueText != null)
                continueText.text = lines.Length > 1
                    ? "[E / ESPACIO / CLIC] CONTINUAR"
                    : "[E / ESPACIO / CLIC] ACEPTAR MISIÓN";
            if (overlay != null)
            {
                overlay.SetActive(true);
                overlay.transform.SetAsLastSibling();
            }

            IsOpen = true;
            Time.timeScale = 0f;
            return true;
        }

        public void Advance()
        {
            if (!IsOpen) return;
            lineIndex++;
            if (lineIndex < lines.Length)
            {
                if (bodyText != null) bodyText.text = lines[lineIndex];
                if (continueText != null && lineIndex == lines.Length - 1)
                    continueText.text = "[E / ESPACIO / CLIC] ACEPTAR MISIÓN";
                return;
            }

            Action callback = completed;
            completed = null;
            lines = null;
            IsOpen = false;
            if (overlay != null) overlay.SetActive(false);
            Time.timeScale = HudController.PauseActive || CardChoiceController.IsOpen
                ? 0f
                : Mathf.Max(0.0001f, previousTimeScale);
            callback?.Invoke();
        }

        public void Configure(GameObject overlayObject, Text speaker, Text body, Text continueLabel)
        {
            overlay = overlayObject;
            speakerText = speaker;
            bodyText = body;
            continueText = continueLabel;
        }
    }
}
