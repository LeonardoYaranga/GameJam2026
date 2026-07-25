using UnityEngine;

namespace NidoCero
{
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private int floorIndex;
        [SerializeField] private Renderer beacon;

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.SetCheckpoint(transform.position + Vector3.up * 1.1f);
            if (GameSession.Instance != null)
            {
                GameSession.Instance.State.currentFloor = Mathf.Max(GameSession.Instance.State.currentFloor, floorIndex);
                GameSession.Instance.NotifyChanged();
            }
            if (beacon != null) beacon.material.color = Color.green;
        }

        public void Configure(int floor, Renderer value)
        {
            floorIndex = floor;
            beacon = value;
        }
    }
}
