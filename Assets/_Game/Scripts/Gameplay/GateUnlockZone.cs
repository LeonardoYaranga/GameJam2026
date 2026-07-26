using UnityEngine;

namespace NidoCero
{
    public sealed class GateUnlockZone : MonoBehaviour
    {
        [SerializeField] private int floorIndex;
        [SerializeField] private GameObject barrier;
        [SerializeField] private TextMesh label;
        [SerializeField] private string closedLabel;
        [SerializeField] private string requiredChoiceId;
        [SerializeField] private string requiredStoryFlag;

        private void Start()
        {
            if (GameSession.Instance != null && GameSession.Instance.State.openedGates.Contains(floorIndex))
                Open();
            else if (!string.IsNullOrWhiteSpace(requiredStoryFlag))
                TryOpenFromProgress();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || GameSession.Instance == null) return;
            RunState state = GameSession.Instance.State;
            if (!string.IsNullOrWhiteSpace(requiredStoryFlag) &&
                !state.storyFlags.Contains(requiredStoryFlag))
            {
                if (label != null)
                    label.text = floorIndex == 0 ? "HABLA CON GEORGE" : "LIBERA EL HÁBITAT";
                return;
            }

            if (!string.IsNullOrWhiteSpace(requiredChoiceId) &&
                !state.resolvedChoices.Contains(requiredChoiceId))
            {
                if (label != null) label.text = "FALTA LA DECISIÓN";
                return;
            }

            if (!TryOpenFromProgress() && label != null)
            {
                label.text = "FALTA LA LLAVE";
                HudController.Instance?.ShowNotification(
                    "La llave está dentro de un módulo enemigo.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null && label != null &&
                (GameSession.Instance == null || !GameSession.Instance.State.openedGates.Contains(floorIndex)))
                label.text = closedLabel;
        }

        private void Open()
        {
            if (barrier != null) barrier.SetActive(false);
            if (label != null) label.text = "ABIERTO";
        }

        public bool TryOpenFromProgress()
        {
            if (GameSession.Instance == null) return false;
            RunState state = GameSession.Instance.State;
            if (state.openedGates.Contains(floorIndex))
            {
                Open();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(requiredChoiceId) &&
                !state.resolvedChoices.Contains(requiredChoiceId))
                return false;
            if (!string.IsNullOrWhiteSpace(requiredStoryFlag) &&
                !state.storyFlags.Contains(requiredStoryFlag))
                return false;
            if (state.keyFloor != floorIndex) return false;

            state.keyFloor = -1;
            state.openedGates.Add(floorIndex);
            Open();
            GameSession.Instance.NotifyChanged();
            return true;
        }

        public int FloorIndex => floorIndex;
        public bool IsOpen => barrier == null || !barrier.activeSelf;

        public void Configure(int floor, GameObject gate, TextMesh textMesh, string labelWhenClosed,
            string choiceId, string storyFlag = null)
        {
            floorIndex = floor;
            barrier = gate;
            label = textMesh;
            closedLabel = labelWhenClosed;
            requiredChoiceId = choiceId;
            requiredStoryFlag = storyFlag;
        }
    }
}
