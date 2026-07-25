using UnityEngine;

namespace NidoCero
{
    public sealed class BossCore : MonoBehaviour
    {
        [SerializeField] private int health = 6;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Collider targetCollider;
        private bool vulnerable;
        private float nextAttack;
        private PlayerController player;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        private void Update()
        {
            if (player == null || Time.time < nextAttack || CardChoiceController.IsOpen) return;
            if (Mathf.Abs(player.transform.position.y - transform.position.y) > 8f) return;
            nextAttack = Time.time + (vulnerable ? 1.1f : 1.8f);
            Vector3 direction = (player.transform.position - transform.position).normalized;
            NidoProjectile projectile = NidoProjectile.Create(transform.position + direction * 1.2f, 0.24f);
            projectile.Launch(direction, vulnerable ? 9f : 7f, false, gameObject,
                vulnerable ? new Color(1f, 0.2f, 0.1f) : new Color(0.65f, 0.2f, 1f),
                ElementId.Fire, 2);
        }

        public void SetVulnerable(bool value)
        {
            vulnerable = value;
            if (targetRenderer != null)
                targetRenderer.material.color = vulnerable
                    ? new Color(1f, 0.2f, 0.15f)
                    : new Color(0.2f, 0.2f, 0.25f);
        }

        public void Hit(int amount)
        {
            if (!vulnerable) return;
            health -= Mathf.Max(1, amount);
            HudController.Instance?.SetBoss("IA CENTRAL — NÚCLEO", Mathf.Clamp01(health / 6f));
            if (health <= 0)
            {
                gameObject.SetActive(false);
                BossEncounter.Instance?.CoreDestroyed();
            }
        }

        public void Configure(Renderer rendererValue, Collider colliderValue)
        {
            targetRenderer = rendererValue;
            targetCollider = colliderValue;
        }
    }
}
