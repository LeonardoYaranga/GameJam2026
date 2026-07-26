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
        [Header("Damage feedback")]
        [SerializeField] private float damageShakeDuration = 0.18f;
        [SerializeField] private float damageShakeMagnitude = 0.12f;

        private Camera viewCamera;
        private Vector3 smoothedPosition;
        private float shakeRemaining;
        private float activeShakeDuration;
        private float activeShakeMagnitude;

        public bool IsShaking => shakeRemaining > 0f;

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
            smoothedPosition = new Vector3(
                Mathf.Lerp(smoothedPosition.x, desired.x, horizontalBlend),
                Mathf.Lerp(smoothedPosition.y, desired.y, verticalBlend),
                desired.z);

            Vector3 shakeOffset = Vector3.zero;
            if (shakeRemaining > 0f)
            {
                shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
                float strength = activeShakeDuration > 0f
                    ? shakeRemaining / activeShakeDuration
                    : 0f;
                Vector2 randomOffset = Random.insideUnitCircle * (activeShakeMagnitude * strength);
                shakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
            }

            transform.position = smoothedPosition + shakeOffset;
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        public void ConfigureLayout(float roomHeight, float centerOffset, float halfWidth, int maximumRoomIndex)
        {
            floorHeight = Mathf.Max(1f, roomHeight);
            roomCenterOffset = centerOffset;
            levelHalfWidth = Mathf.Max(1f, halfWidth);
            horizontalPadding = 0f;
            maxRoomIndex = Mathf.Max(0, maximumRoomIndex);
        }

        public void PlayDamageShake()
        {
            activeShakeDuration = Mathf.Max(0.01f, damageShakeDuration);
            activeShakeMagnitude = Mathf.Max(0f, damageShakeMagnitude);
            shakeRemaining = activeShakeDuration;
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
            smoothedPosition = DesiredPosition();
            transform.position = smoothedPosition;
        }
    }
}
