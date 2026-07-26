using UnityEngine;

namespace NidoCero
{
    public sealed class BossRelay : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private ElementId element;
        [SerializeField] private int elementLevel = 3;
        private float healthPercent = 100f;
        private MaterialPropertyBlock colorProperties;

        public bool IsAlive => healthPercent > 0f && gameObject.activeSelf;
        public int RemainingHits => healthPercent <= 0f
            ? 0
            : Mathf.CeilToInt(healthPercent / CombatMath.MaximumRegularHitPercent);
        public float HealthNormalized => Mathf.Clamp01(healthPercent / 100f);
        public ElementId Element => element;
        public int ElementLevel => elementLevel;
        public float LastDamagePercent { get; private set; }

        private void Awake()
        {
            healthPercent = 100f;
            ApplyColor();
        }

        public void Hit()
        {
            if (!IsAlive) return;
            ApplyDamagePercent(CombatMath.MaximumRegularHitPercent);
        }

        public float TakeProjectileHit(ElementId attackElement, int attackLevel)
        {
            if (!IsAlive) return 0f;
            RuntimeStats stats = GameSession.Instance != null
                ? GameSession.Instance.State.stats
                : new RuntimeStats();
            ElementLevels elements = GameSession.Instance != null
                ? GameSession.Instance.State.elements
                : new ElementLevels();
            float damagePercent = CombatMath.RelayProjectileDamagePercent(
                stats,
                elements,
                attackElement,
                attackLevel,
                element,
                elementLevel);
            ApplyDamagePercent(damagePercent);
            return damagePercent;
        }

        private void ApplyDamagePercent(float damagePercent)
        {
            LastDamagePercent = Mathf.Clamp(damagePercent, 0f, 100f);
            healthPercent = Mathf.Max(0f, healthPercent - LastDamagePercent);
            ApplyColor();
            if (healthPercent <= 0f)
            {
                gameObject.SetActive(false);
                BossEncounter.Instance?.RelayDestroyed();
            }
        }

        public void Configure(Renderer value, ElementId relayElement = ElementId.Water)
        {
            targetRenderer = value;
            element = relayElement;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (targetRenderer == null) return;
            Color elemental = ElementalResolver.ProjectileColor(element, Mathf.Max(1, elementLevel));
            Color displayColor = IsAlive
                ? Color.Lerp(Color.gray, elemental, 0.45f + HealthNormalized * 0.4f)
                : Color.gray;
            if (colorProperties == null) colorProperties = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(colorProperties);
            colorProperties.SetColor("_Color", displayColor);
            colorProperties.SetColor("_BaseColor", displayColor);
            targetRenderer.SetPropertyBlock(colorProperties);
        }
    }
}
