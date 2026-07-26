using UnityEngine;

namespace NidoCero
{
    public sealed class BossCore : MonoBehaviour
    {
        [SerializeField] private int health = 6;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private float elementInterval = 4f;
        private bool vulnerable;
        private float nextAttack;
        private float nextElementChange;
        private PlayerController player;
        private ElementId activeElement = ElementId.Water;

        public ElementId ActiveElement => activeElement;
        public int ElementLevel => 5;
        public string ElementLabel
        {
            get
            {
                switch (activeElement)
                {
                    case ElementId.Fire: return "FUEGO";
                    case ElementId.Vegetation: return "VEGETACIÓN";
                    default: return "AGUA";
                }
            }
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            nextElementChange = Time.time + elementInterval;
            ApplyCoreColor();
        }

        private void Update()
        {
            if (Time.time >= nextElementChange)
            {
                activeElement = (ElementId)(((int)activeElement + 1) % 3);
                nextElementChange = Time.time + elementInterval;
                ApplyCoreColor();
                BossEncounter.Instance?.ElementChanged();
            }

            if (player == null || Time.time < nextAttack ||
                CardChoiceController.IsOpen || DialogueController.IsOpen ||
                FinalSacrificeController.IsOpen) return;
            if (Mathf.Abs(player.transform.position.y - transform.position.y) > 8f) return;
            nextAttack = Time.time + (vulnerable ? 1.1f : 1.8f);
            Vector3 direction = (player.transform.position - transform.position).normalized;
            NidoProjectile projectile = NidoProjectile.Create(transform.position + direction * 1.2f, 0.24f);
            projectile.Launch(direction, vulnerable ? 9f : 7f, false, gameObject,
                ElementColor(activeElement), activeElement, ElementLevel);
        }

        public void SetVulnerable(bool value)
        {
            vulnerable = value;
            if (targetCollider != null)
                targetCollider.enabled = vulnerable;
            ApplyCoreColor();
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

        private void ApplyCoreColor()
        {
            if (targetRenderer == null) return;
            Color neutral = new Color(0.2f, 0.2f, 0.25f);
            targetRenderer.material.color = Color.Lerp(
                neutral, ElementColor(activeElement), vulnerable ? 0.72f : 0.32f);
        }

        private static Color ElementColor(ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return new Color(1f, 0.22f, 0.08f);
                case ElementId.Vegetation: return new Color(0.24f, 0.9f, 0.28f);
                default: return new Color(0.08f, 0.58f, 1f);
            }
        }
    }
}
