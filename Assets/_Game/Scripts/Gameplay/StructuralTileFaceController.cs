using UnityEngine;

namespace NidoCero
{
    public sealed class StructuralTileFaceController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private GameObject[] floorGroups;
        [SerializeField] private float floorHeight = 5f;

        private int activePhysicalFloor = -1;

        public int ActivePhysicalFloor => activePhysicalFloor;

        private void Start()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }

            RefreshImmediate();
        }

        private void LateUpdate()
        {
            RefreshImmediate();
        }

        public void Configure(Transform value, GameObject[] groups, float roomHeight)
        {
            target = value;
            floorGroups = groups;
            floorHeight = Mathf.Max(1f, roomHeight);
            activePhysicalFloor = -1;
            RefreshImmediate();
        }

        public void RefreshImmediate()
        {
            if (target == null || floorGroups == null || floorGroups.Length == 0) return;

            int physicalFloor = Mathf.Clamp(
                Mathf.FloorToInt((target.position.y + 0.75f) / floorHeight),
                0, floorGroups.Length - 1);
            if (physicalFloor == activePhysicalFloor) return;

            activePhysicalFloor = physicalFloor;
            for (int index = 0; index < floorGroups.Length; index++)
            {
                if (floorGroups[index] != null)
                    floorGroups[index].SetActive(index == activePhysicalFloor);
            }
        }
    }
}
