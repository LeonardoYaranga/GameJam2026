using UnityEngine;

namespace NidoCero
{
    public sealed class FloorMarker : MonoBehaviour
    {
        [SerializeField] private int floorIndex;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || GameSession.Instance == null) return;
            GameSession.Instance.State.currentFloor = floorIndex;
            GameSession.Instance.NotifyChanged();
        }

        public void Configure(int floor)
        {
            floorIndex = floor;
        }
    }
}
