using UnityEngine;

namespace NidoCero
{
    public sealed class NidoProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private bool friendly;
        [SerializeField] private ElementId element;
        [SerializeField] private int elementLevel;
        [SerializeField] private float animationFramesPerSecond = 12f;

        private GameObject owner;
        private SpriteRenderer spriteRenderer;
        private Sprite[] spriteFrames;
        private int currentFrame;
        private float nextFrameTime;

        public bool IsFriendly => friendly;
        public ElementId Element => element;
        public int ElementLevel => elementLevel;
        public Color ProjectileColor { get; private set; }
        public bool UsesAnimatedSprite => spriteRenderer != null && spriteFrames != null &&
                                          spriteFrames.Length > 0;
        public int SpriteFrameCount => spriteFrames != null ? spriteFrames.Length : 0;
        public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

        public void Launch(Vector3 direction, float speed, bool isFriendly, GameObject source,
            ElementId projectileElement = ElementId.Water, int level = 0)
        {
            friendly = isFriendly;
            owner = source;
            element = projectileElement;
            elementLevel = Mathf.Max(0, level);
            ProjectileColor = ElementalResolver.ProjectileColor(element, elementLevel);
            gameObject.name = friendly
                ? (elementLevel > 0 ? "Projectile_Player_" + element : "Projectile_Player_Neutral")
                : "Projectile_Enemy_" + element;

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");
                Material material = new Material(shader);
                material.color = ProjectileColor;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", ProjectileColor);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", ProjectileColor * 0.45f);
                }
                renderer.material = material;
            }

            ConfigureAnimatedSprite(direction);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null) body.linearVelocity = direction.normalized * speed;

            Collider ownCollider = GetComponent<Collider>();
            Collider ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            if (ownCollider != null && ownerCollider != null)
                Physics.IgnoreCollision(ownCollider, ownerCollider, true);

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!UsesAnimatedSprite || Time.time < nextFrameTime) return;
            currentFrame = (currentFrame + 1) % spriteFrames.Length;
            spriteRenderer.sprite = spriteFrames[currentFrame];
            nextFrameTime = Time.time + 1f / Mathf.Max(1f, animationFramesPerSecond);
        }

        private void ConfigureAnimatedSprite(Vector3 direction)
        {
            bool elementalVisual = !friendly || elementLevel > 0;
            if (!elementalVisual) return;

            spriteFrames = LoadFrames(element);
            if (spriteFrames == null || spriteFrames.Length == 0) return;

            Renderer meshRenderer = GetComponent<Renderer>();
            if (meshRenderer != null) meshRenderer.enabled = false;

            GameObject visual = new GameObject("ProjectileVisual");
            visual.transform.SetParent(transform, false);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 180f);

            spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = spriteFrames[0];
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 12;
            currentFrame = 0;
            nextFrameTime = Time.time + 1f / Mathf.Max(1f, animationFramesPerSecond);
        }

        private static Sprite[] LoadFrames(ElementId projectileElement)
        {
            string elementName;
            switch (projectileElement)
            {
                case ElementId.Fire:
                    elementName = "fire";
                    break;
                case ElementId.Vegetation:
                    elementName = "nature";
                    break;
                default:
                    elementName = "water";
                    break;
            }

            Sprite[] frames = new Sprite[4];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                frames[frame] = Resources.Load<Sprite>(
                    "Visuals/Projectiles/projectile_" + elementName + "_" +
                    (frame + 1).ToString("00"));
                if (frames[frame] == null) return null;
            }
            return frames;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform))) return;

            if (friendly)
            {
                RobotEnemy enemy = other.GetComponentInParent<RobotEnemy>();
                if (enemy != null)
                {
                    enemy.TakeProjectileHit(element, elementLevel);
                    Destroy(gameObject);
                    return;
                }

                BossRelay relay = other.GetComponentInParent<BossRelay>();
                if (relay != null)
                {
                    relay.TakeProjectileHit(element, elementLevel);
                    Destroy(gameObject);
                    return;
                }

                BossCore core = other.GetComponentInParent<BossCore>();
                if (core != null)
                {
                    core.TakeProjectileHit(element, elementLevel);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    RobotEnemy sourceEnemy = owner != null
                        ? owner.GetComponent<RobotEnemy>()
                        : null;
                    EnemyDefinition sourceDefinition = sourceEnemy != null
                        ? sourceEnemy.Definition
                        : null;
                    player.TakeCombatDamage(
                        element,
                        elementLevel,
                        sourceDefinition != null
                            ? sourceDefinition.archetype
                            : EnemyArchetype.Flyer,
                        false);
                    Destroy(gameObject);
                    return;
                }
            }

            if (!other.isTrigger && other.GetComponent<Checkpoint>() == null)
                Destroy(gameObject);
        }

        public static NidoProjectile Create(Vector3 position, float scale = 0.34f)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";
            projectile.transform.position = position;
            projectile.transform.localScale = Vector3.one * scale;
            SphereCollider collider = projectile.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            Rigidbody body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            return projectile.AddComponent<NidoProjectile>();
        }
    }
}
