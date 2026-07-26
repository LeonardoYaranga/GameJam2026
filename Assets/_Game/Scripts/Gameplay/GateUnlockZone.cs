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

        private void Start()
        {
            if (GameSession.Instance != null && GameSession.Instance.State.openedGates.Contains(floorIndex))
                Open();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || GameSession.Instance == null) return;
            RunState state = GameSession.Instance.State;
            if (!string.IsNullOrWhiteSpace(requiredChoiceId) &&
                !state.resolvedChoices.Contains(requiredChoiceId))
            {
                if (label != null) label.text = "FALTA LA DECISIÓN";
                return;
            }

            if (state.keyFloor == floorIndex)
            {
                state.keyFloor = -1;
                if (!state.openedGates.Contains(floorIndex)) state.openedGates.Add(floorIndex);
                Open();
                GameSession.Instance.NotifyChanged();
            }
            else if (label != null)
            {
                label.text = "FALTA LA LLAVE";
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

        public void Configure(int floor, GameObject gate, TextMesh textMesh, string labelWhenClosed,
            string choiceId)
        {
            floorIndex = floor;
            barrier = gate;
            label = textMesh;
            closedLabel = labelWhenClosed;
            requiredChoiceId = choiceId;
        }
    }
}
