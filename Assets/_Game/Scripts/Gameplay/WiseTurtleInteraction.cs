using UnityEngine;

namespace NidoCero
{
    public sealed class WiseTurtleInteraction : MonoBehaviour
    {
        public const string MissionStoryFlag = "george_mission_complete";

        [SerializeField] private string requiredChoiceId = "george_first_choice";
        [SerializeField] private int gateFloorIndex;
        [SerializeField] private TextMesh promptLabel;
        [SerializeField] private string speakerName = "GEORGE ZOILO MIYAGI";
        [SerializeField] private string[] dialogueLines =
        {
            "Los humanos desaparecieron. Su control no.",
            "Debajo de este volcán guardan a los que quedan.",
            "Tu elección ya tiene un costo. Llega al ojo y abre las jaulas."
        };

        private PlayerController nearbyPlayer;

        public bool IsPlayerNearby => nearbyPlayer != null;
        public bool IsCompleted =>
            GameSession.Instance != null &&
            GameSession.Instance.State.storyFlags.Contains(MissionStoryFlag);

        private void Start()
        {
            RefreshPrompt();
            if (IsCompleted) OpenRequiredGate();
        }

        private void Update()
        {
            if (nearbyPlayer == null || DialogueController.IsOpen ||
                CardChoiceController.IsOpen || HudController.PauseActive)
                return;
            if (Input.GetKeyDown(KeyCode.E)) TryBeginConversation();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            nearbyPlayer = player;
            RefreshPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != nearbyPlayer) return;
            nearbyPlayer = null;
            RefreshPrompt();
        }

        public bool TryBeginConversation()
        {
            if (nearbyPlayer == null || GameSession.Instance == null ||
                DialogueController.Instance == null || IsCompleted)
                return false;

            if (!GameSession.Instance.State.resolvedChoices.Contains(requiredChoiceId))
            {
                if (promptLabel != null) promptLabel.text = "TORTUGA SABIA\nRECUPERA EL MÓDULO";
                return false;
            }

            return DialogueController.Instance.Open(speakerName, dialogueLines, CompleteConversation);
        }

        public void CompleteConversation()
        {
            if (GameSession.Instance == null) return;
            RunState state = GameSession.Instance.State;
            if (!state.storyFlags.Contains(MissionStoryFlag))
                state.storyFlags.Add(MissionStoryFlag);
            GameSession.Instance.NotifyChanged();
            RefreshPrompt();
            OpenRequiredGate();
        }

        private void OpenRequiredGate()
        {
            GateUnlockZone[] gates =
                FindObjectsByType<GateUnlockZone>(FindObjectsSortMode.None);
            foreach (GateUnlockZone gate in gates)
            {
                if (gate != null && gate.FloorIndex == gateFloorIndex)
                    gate.TryOpenFromProgress();
            }
        }

        private void RefreshPrompt()
        {
            if (promptLabel == null) return;
            if (IsCompleted)
            {
                promptLabel.text = "TORTUGA SABIA\nMISIÓN ACEPTADA";
                return;
            }

            bool choiceReady = GameSession.Instance != null &&
                               GameSession.Instance.State.resolvedChoices.Contains(requiredChoiceId);
            promptLabel.text = nearbyPlayer != null
                ? (choiceReady ? "TORTUGA SABIA\n[E] HABLAR" : "TORTUGA SABIA\nRECUPERA EL MÓDULO")
                : "TORTUGA SABIA";
        }

        public void Configure(TextMesh label, string choiceId, int floor)
        {
            promptLabel = label;
            requiredChoiceId = choiceId;
            gateFloorIndex = floor;
        }
    }
}
