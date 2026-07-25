using UnityEngine;

namespace NidoCero
{
    public sealed class NidoProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private int damage = 1;
        [SerializeField] private bool friendly;
        [SerializeField] private ElementId element;
        [SerializeField] private int elementLevel;

        private GameObject owner;

        public void Launch(Vector3 direction, float speed, bool isFriendly, GameObject source, Color color,
            ElementId projectileElement = ElementId.Water, int level = 0)
        {
            friendly = isFriendly;
            owner = source;
            element = projectileElement;
            elementLevel = level;

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Material material = new Material(shader);
                material.color = color;
                renderer.material = material;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null) body.linearVelocity = direction.normalized * speed;

            Collider ownCollider = GetComponent<Collider>();
            Collider ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            if (ownCollider != null && ownerCollider != null)
                Physics.IgnoreCollision(ownCollider, ownerCollider, true);

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform))) return;

            if (friendly)
            {
                RobotEnemy enemy = other.GetComponentInParent<RobotEnemy>();
                if (enemy != null)
                {
                    int bonus = enemy.ElementalBonusAgainst(element, elementLevel);
                    enemy.TakeDamage(damage + bonus);
                    Destroy(gameObject);
                    return;
                }

                BossRelay relay = other.GetComponentInParent<BossRelay>();
                if (relay != null)
                {
                    relay.Hit();
                    Destroy(gameObject);
                    return;
                }

                BossCore core = other.GetComponentInParent<BossCore>();
                if (core != null)
                {
                    core.Hit(damage);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }

            if (!other.isTrigger && other.GetComponent<Checkpoint>() == null)
                Destroy(gameObject);
        }

        public static NidoProjectile Create(Vector3 position, float scale = 0.22f)
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
