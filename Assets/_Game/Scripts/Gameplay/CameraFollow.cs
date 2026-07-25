using UnityEngine;

namespace NidoCero
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float floorHeight = 5f;
        [SerializeField] private float roomCenterOffset = 2.5f;
        [SerializeField] private float horizontalSmoothing = 18f;
        [SerializeField] private float verticalSmoothing = 11f;
        [SerializeField] private float levelHalfWidth = 18f;
        [SerializeField] private float horizontalPadding = 0.35f;
        [SerializeField] private int maxRoomIndex = 6;

        private Camera viewCamera;

        private void Start()
        {
            viewCamera = GetComponent<Camera>();
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }

            if (target != null) SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = DesiredPosition();
            float horizontalBlend = 1f - Mathf.Exp(-horizontalSmoothing * Time.unscaledDeltaTime);
            float verticalBlend = 1f - Mathf.Exp(-verticalSmoothing * Time.unscaledDeltaTime);
            Vector3 current = transform.position;
            transform.position = new Vector3(
                Mathf.Lerp(current.x, desired.x, horizontalBlend),
                Mathf.Lerp(current.y, desired.y, verticalBlend),
                desired.z);
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        private Vector3 DesiredPosition()
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>();

            float viewportHalfWidth = viewCamera != null && viewCamera.orthographic
                ? viewCamera.orthographicSize * viewCamera.aspect
                : 6f;
            float horizontalLimit = Mathf.Max(0f, levelHalfWidth - viewportHalfWidth - horizontalPadding);
            int roomIndex = Mathf.Clamp(
                Mathf.FloorToInt((target.position.y + 0.75f) / floorHeight), 0, maxRoomIndex);

            return new Vector3(
                Mathf.Clamp(target.position.x, -horizontalLimit, horizontalLimit),
                roomIndex * floorHeight + roomCenterOffset,
                -20f);
        }

        private void SnapToTarget()
        {
            transform.position = DesiredPosition();
        }
    }
}
