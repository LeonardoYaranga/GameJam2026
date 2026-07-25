using UnityEngine;

namespace NidoCero
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -20f);
        [SerializeField] private float smoothing = 6f;
        [SerializeField] private float minY = 3.5f;
        [SerializeField] private float maxY = 33f;

        private void Start()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            desired.x = Mathf.Clamp(desired.x, -9f, 9f);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }
    }
}
