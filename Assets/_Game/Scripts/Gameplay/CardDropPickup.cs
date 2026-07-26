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
        [SerializeField] private float magnetRadius = 5.5f;
        [SerializeField] private float magnetSpeed = 6f;
        [SerializeField] private bool grantsKey;
        [SerializeField] private int keyFloorIndex = -1;

        private float baseY;
        private bool collected;
        private PlayerController player;
        private Transform labelTransform;
        private static Material sharedDropMaterial;

        public string ChoiceId => choiceId;
        public bool GrantsKey => grantsKey;
        public int KeyFloorIndex => keyFloorIndex;

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

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            Vector3 pickupTarget = player != null
                ? player.transform.position + new Vector3(0f, 0.75f, 0f)
                : transform.position;
            bool magnetized = player != null &&
                              (pickupTarget - transform.position).sqrMagnitude <=
                              magnetRadius * magnetRadius;

            if (magnetized)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, pickupTarget, magnetSpeed * Time.deltaTime);
                baseY = transform.position.y;
                if ((pickupTarget - transform.position).sqrMagnitude <= 0.35f * 0.35f)
                    TryCollect(player);
            }
            else
            {
                Vector3 position = transform.position;
                position.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = position;
            }

            if (labelTransform == null) labelTransform = transform.Find("PickupLabel");
            if (labelTransform != null)
            {
                labelTransform.position = transform.position + new Vector3(0f, 0.78f, -0.55f);
                labelTransform.rotation = Quaternion.identity;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other.GetComponentInParent<PlayerController>());
        }

        private void TryCollect(PlayerController collector)
        {
            if (collected || collector == null || GameSession.Instance == null) return;

            CardChoiceController controller = CardChoiceController.Instance;
            RunState state = GameSession.Instance.State;
            bool alreadyResolved = state.resolvedChoices.Contains(choiceId);
            if (!alreadyResolved && controller == null) return;

            if (grantsKey && keyFloorIndex >= 0 &&
                !state.openedGates.Contains(keyFloorIndex))
            {
                state.keyFloor = keyFloorIndex;
                GameSession.Instance.NotifyChanged();
            }

            if (!alreadyResolved && !controller.Open(choiceId)) return;
            collected = true;
            gameObject.SetActive(false);
        }

        public void Configure(string id, bool key = false, int floor = -1)
        {
            choiceId = id;
            grantsKey = key;
            keyFloorIndex = floor;
        }

        public static CardDropPickup Spawn(Vector3 position, string sourceId, bool key = false,
            int floor = -1, string choiceIdOverride = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CardDrop_" + sourceId;
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.72f;
            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = DropMaterial();

            CardDropPickup pickup = cube.AddComponent<CardDropPickup>();
            pickup.Configure(
                string.IsNullOrWhiteSpace(choiceIdOverride)
                    ? "enemy_drop_" + sourceId
                    : choiceIdOverride,
                key,
                floor);
            pickup.baseY = position.y;

            GameObject labelObject = new GameObject("PickupLabel");
            labelObject.transform.SetParent(cube.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.92f, -0.55f);
            labelObject.transform.localRotation = Quaternion.identity;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = key ? "MÓDULO + LLAVE" : "DECISIÓN";
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
