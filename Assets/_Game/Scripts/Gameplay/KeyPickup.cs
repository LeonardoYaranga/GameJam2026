using UnityEngine;

namespace NidoCero
{
    public sealed class KeyPickup : MonoBehaviour
    {
        [SerializeField] private int floorIndex;
        private float baseY;

        private void Start()
        {
            baseY = transform.position.y;
        }

        private void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * 3f) * 0.2f;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || GameSession.Instance == null) return;
            GameSession.Instance.State.keyFloor = floorIndex;
            GameSession.Instance.NotifyChanged();
            Destroy(gameObject);
        }

        public static void Spawn(Vector3 position, int floor)
        {
            GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cube);
            key.name = "Key_Floor_" + (floor + 1);
            key.transform.position = position;
            key.transform.localScale = new Vector3(0.3f, 0.75f, 0.18f);
            key.GetComponent<BoxCollider>().isTrigger = true;
            Renderer renderer = key.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = new Color(1f, 0.82f, 0.15f) };
            renderer.material = material;
            Rigidbody body = key.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            KeyPickup pickup = key.AddComponent<KeyPickup>();
            pickup.floorIndex = floor;
        }
    }
}
