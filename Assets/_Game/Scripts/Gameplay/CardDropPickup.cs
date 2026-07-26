using UnityEngine;

namespace NidoCero
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CardDropPickup : MonoBehaviour
    {
        [SerializeField] private string choiceId;
        [SerializeField] private float rotationSpeed = 70f;
        [SerializeField] private float bobHeight = 0.16f;
        [SerializeField] private float bobSpeed = 2.4f;

        private float baseY;
        private bool collected;
        private Transform labelTransform;
        private static Material sharedDropMaterial;

        public string ChoiceId => choiceId;

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            baseY = transform.position.y;
        }

        private void Update()
        {
            transform.Rotate(18f * Time.deltaTime, rotationSpeed * Time.deltaTime, 26f * Time.deltaTime,
                Space.World);
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = position;

            if (labelTransform == null) labelTransform = transform.Find("PickupLabel");
            if (labelTransform != null)
            {
                labelTransform.position = transform.position + new Vector3(0f, 0.78f, -0.55f);
                labelTransform.rotation = Quaternion.identity;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || other.GetComponentInParent<PlayerController>() == null) return;
            CardChoiceController controller = CardChoiceController.Instance;
            if (controller == null || !controller.Open(choiceId)) return;
            collected = true;
            gameObject.SetActive(false);
        }

        public void Configure(string id)
        {
            choiceId = id;
        }

        public static CardDropPickup Spawn(Vector3 position, string sourceId)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CardDrop_" + sourceId;
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.72f;
            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = DropMaterial();

            CardDropPickup pickup = cube.AddComponent<CardDropPickup>();
            pickup.Configure("enemy_drop_" + sourceId);
            pickup.baseY = position.y;

            GameObject labelObject = new GameObject("PickupLabel");
            labelObject.transform.SetParent(cube.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.92f, -0.55f);
            labelObject.transform.localRotation = Quaternion.identity;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "DECISIÓN";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 42;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.85f, 1f, 1f);

            return pickup;
        }

        private static Material DropMaterial()
        {
            if (sharedDropMaterial != null) return sharedDropMaterial;
            Shader shader = Shader.Find("Standard");
            sharedDropMaterial = new Material(shader)
            {
                name = "Runtime_CardDrop_Material",
                color = new Color(0.1f, 0.78f, 0.92f)
            };
            sharedDropMaterial.EnableKeyword("_EMISSION");
            sharedDropMaterial.SetColor("_EmissionColor", new Color(0.04f, 0.42f, 0.62f));
            return sharedDropMaterial;
        }
    }
}
