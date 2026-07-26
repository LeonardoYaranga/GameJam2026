using System.Collections;
using UnityEngine;

namespace NidoCero
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RobotEnemy : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private int floorIndex;
        [SerializeField] private bool carriesKey;
        [SerializeField] private bool triggersChoice;
        [SerializeField] private string choiceIdOverride;
        [SerializeField] private float patrolDistance = 3f;
        [Header("Elemental appearance")]
        [SerializeField, Range(0f, 0.3f)] private float elementalTintStrength = 0.28f;
        [Header("Damage feedback")]
        [SerializeField] private float hitFeedbackDuration = 0.3f;
        [SerializeField] private float hitFlashInterval = 0.055f;
        [SerializeField] private float hitShakeDistance = 0.075f;
        [Header("Death feedback")]
        [SerializeField] private float groundedDeathJumpVelocity = 3.8f;
        [SerializeField] private float flyerDeathJumpVelocity = 1.45f;
        [SerializeField] private float groundedFlipDuration = 0.72f;
        [SerializeField] private float flyerFlipDuration = 0.92f;
        [SerializeField] private float corpseSettleDelay = 0.38f;

        private Rigidbody body;
        private Collider enemyCollider;
        private float healthPercent = 100f;
        private int stompCount;
        private float direction = 1f;
        private float originX;
        private float originY;
        private float nextAttack;
        private PlayerController player;
        private Transform visualRoot;
        private Vector3 visualBaseLocalPosition;
        private Renderer[] visualRenderers;
        private Color[] originalRendererColors;
        private Color[] elementalRendererColors;
        private Coroutine hitFeedbackRoutine;
        private bool isDead;
        private bool corpsePoseSettled;
        private float deathFloorSurfaceY;
        private EnemyHealthIndicator healthIndicator;

        public string EnemyId => enemyId;
        public EnemyDefinition Definition => definition;
        public bool IsDead => isDead;
        public bool IsHitFeedbackActive => hitFeedbackRoutine != null;
        public bool CorpsePoseSettled => corpsePoseSettled;
        public float ElementalTintStrength => Mathf.Clamp(elementalTintStrength, 0f, 0.3f);
        public float HealthNormalized => Mathf.Clamp01(healthPercent / 100f);
        public float LastDamagePercent { get; private set; }
        public int RequiredStomps =>
            definition != null && definition.archetype == EnemyArchetype.Tank ? 2 : 1;
        public EnemyHealthIndicator HealthIndicator => healthIndicator;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            enemyCollider = GetComponent<Collider>();
            body.useGravity = definition == null || definition.archetype != EnemyArchetype.Flyer;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            originX = transform.position.x;
            originY = transform.position.y;
            healthPercent = 100f;
            visualRoot = transform.Find("Cangrejo_Visual") ??
                         transform.Find("Tortuga_Visual") ??
                         transform.Find("Fragata_Visual");
            if (visualRoot != null) visualBaseLocalPosition = visualRoot.localPosition;
            CacheVisualRenderers();
            CreateHealthIndicator();
        }

        private void Start()
        {
            if (GameSession.Instance != null && GameSession.Instance.State.defeatedEnemies.Contains(enemyId))
            {
                if (healthIndicator != null) healthIndicator.Dispose();
                gameObject.SetActive(false);
                return;
            }

            player = FindFirstObjectByType<PlayerController>();
            nextAttack = Time.time + 1.5f;
        }

        private void FixedUpdate()
        {
            if (isDead || definition == null || player == null ||
                CardChoiceController.IsOpen ||
                FinalSacrificeController.IsOpen) return;

            float distance = player.transform.position.x - transform.position.x;
            bool sameBand = Mathf.Abs(player.transform.position.y - transform.position.y) < 3f;

            if (definition.archetype == EnemyArchetype.Flyer)
            {
                Vector3 hover = new Vector3(
                    originX + Mathf.Sin(Time.time * definition.moveSpeed) * patrolDistance,
                    originY + Mathf.Sin(Time.time * 2f) * 0.08f,
                    0f);
                body.MovePosition(hover);
                if (visualRoot != null)
                {
                    float facing = Mathf.Cos(Time.time * definition.moveSpeed) >= 0f ? 90f : -90f;
                    visualRoot.localRotation = Quaternion.Euler(90f, facing, 0f);
                }
            }
            else
            {
                if (Mathf.Abs(transform.position.x - originX) > patrolDistance)
                    direction = transform.position.x > originX ? -1f : 1f;
                body.linearVelocity = new Vector3(direction * definition.moveSpeed, body.linearVelocity.y, 0f);
                if (visualRoot != null)
                    visualRoot.localRotation = Quaternion.Euler(0f, direction > 0f ? 90f : -90f, 0f);
            }

            if (sameBand && Mathf.Abs(distance) < 9f && Time.time >= nextAttack)
            {
                nextAttack = Time.time + Mathf.Max(0.8f, definition.attackInterval);
                Shoot(Mathf.Sign(distance));
            }
        }

        private void Shoot(float horizontalDirection)
        {
            Vector3 directionVector = new Vector3(horizontalDirection == 0f ? 1f : horizontalDirection, 0f, 0f);
            NidoProjectile projectile = NidoProjectile.Create(transform.position + directionVector * 0.9f, 0.26f);
            projectile.Launch(directionVector, 7f, false, gameObject,
                definition != null ? definition.element : ElementId.Fire,
                definition != null ? definition.elementLevel : 1);
            EnemyArchetype archetype = definition != null
                ? definition.archetype
                : EnemyArchetype.Walker;
            GameAudio.Play(
                archetype == EnemyArchetype.Tank ? GameSfx.TankShot :
                archetype == EnemyArchetype.Flyer ? GameSfx.RobotCry :
                GameSfx.CrabClaw,
                0.46f);
        }

        public int ElementalBonusAgainst(ElementId attackElement, int attackLevel)
        {
            if (definition == null) return 0;
            ElementLevels levels = new ElementLevels();
            levels.Add(definition.element, definition.elementLevel);
            return ElementalResolver.Bonus(attackElement, attackLevel, levels);
        }

        public float TakeProjectileHit(ElementId attackElement, int attackLevel)
        {
            if (isDead) return 0f;
            RuntimeStats attackerStats = GameSession.Instance != null
                ? GameSession.Instance.State.stats
                : new RuntimeStats();
            ElementLevels attackerElements = GameSession.Instance != null
                ? GameSession.Instance.State.elements
                : new ElementLevels();
            float damagePercent = CombatMath.EnemyProjectileDamagePercent(
                attackerStats,
                attackerElements,
                attackElement,
                attackLevel,
                definition);
            TakeDamagePercent(damagePercent);
            return damagePercent;
        }

        public void TakeDamage(int amount)
        {
            TakeDamagePercent(Mathf.Max(1f, amount));
        }

        private void TakeDamagePercent(float amount)
        {
            if (isDead) return;
            LastDamagePercent = Mathf.Clamp(amount, 0f, 100f);
            healthPercent = Mathf.Max(0f, healthPercent - LastDamagePercent);
            if (healthIndicator != null) healthIndicator.SetHealth(HealthNormalized);
            PlayHitFeedback();
            if (healthPercent <= 0f) Die();
        }

        public void Stomp()
        {
            if (isDead) return;
            GameAudio.Play(GameSfx.TankStomp, 0.5f);
            stompCount++;
            PlayHitFeedback();
            healthPercent = Mathf.Max(0f,
                100f * (RequiredStomps - stompCount) / Mathf.Max(1, RequiredStomps));
            if (healthIndicator != null) healthIndicator.SetHealth(HealthNormalized);
            if (RequiredStomps == 2 && stompCount == 1)
                HudController.Instance?.ShowNotification("Blindaje al 50%.");
            if (stompCount >= RequiredStomps) Die();
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            GameAudio.Play(GameSfx.RobotDeath, 0.68f);
            healthPercent = 0f;
            if (healthIndicator != null) healthIndicator.SetHealth(0f);

            if (GameSession.Instance != null)
            {
                RunState state = GameSession.Instance.State;
                if (!state.defeatedEnemies.Contains(enemyId)) state.defeatedEnemies.Add(enemyId);
                GameSession.Instance.NotifyChanged();
            }

            if (triggersChoice)
            {
                CardDropPickup.Spawn(
                    transform.position + Vector3.up * 0.85f,
                    enemyId,
                    carriesKey,
                    floorIndex,
                    choiceIdOverride);
            }
            else if (carriesKey)
            {
                KeyPickup.Spawn(transform.position + Vector3.up * 0.8f, floorIndex);
            }

            deathFloorSurfaceY = ResolveDeathFloorSurface();
            bool flyer = definition != null && definition.archetype == EnemyArchetype.Flyer;
            body.useGravity = true;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            float horizontalKick = flyer ? direction * 0.55f : body.linearVelocity.x * 0.22f;
            body.linearVelocity = new Vector3(horizontalKick,
                flyer ? flyerDeathJumpVelocity : groundedDeathJumpVelocity, 0f);
            body.angularVelocity = Vector3.zero;
            StartCoroutine(PlayDeathAnimation(flyer));
        }

        private void CacheVisualRenderers()
        {
            visualRenderers = visualRoot != null
                ? visualRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
            originalRendererColors = new Color[visualRenderers.Length];
            elementalRendererColors = new Color[visualRenderers.Length];
            Color elementalColor = definition != null ? definition.color : Color.white;
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Material material = visualRenderers[i].sharedMaterial;
                originalRendererColors[i] = material != null && material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material != null && material.HasProperty("_Color")
                        ? material.GetColor("_Color")
                        : Color.white;
                elementalRendererColors[i] =
                    Color.Lerp(originalRendererColors[i], elementalColor, ElementalTintStrength);
                SetRendererColor(visualRenderers[i], elementalRendererColors[i]);
            }
        }

        private void CreateHealthIndicator()
        {
            float colliderHeight = enemyCollider != null ? enemyCollider.bounds.extents.y : 0.8f;
            float width;
            switch (definition != null ? definition.archetype : EnemyArchetype.Walker)
            {
                case EnemyArchetype.Tank:
                    width = 2.15f;
                    break;
                case EnemyArchetype.Flyer:
                    width = 1.55f;
                    break;
                default:
                    width = 1.7f;
                    break;
            }

            healthIndicator = EnemyHealthIndicator.Create(
                this,
                definition != null ? definition.color : Color.red,
                colliderHeight + 0.32f,
                width);
        }

        private void PlayHitFeedback()
        {
            if (hitFeedbackRoutine != null)
            {
                StopCoroutine(hitFeedbackRoutine);
                RestoreVisualFeedback();
            }
            hitFeedbackRoutine = StartCoroutine(PlayHitFeedbackRoutine());
        }

        private IEnumerator PlayHitFeedbackRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, hitFeedbackDuration);
            float interval = Mathf.Max(0.025f, hitFlashInterval);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                int phase = Mathf.FloorToInt(elapsed / interval);
                SetVisualTint(phase % 2 == 0);
                if (visualRoot != null)
                {
                    Vector2 offset = Random.insideUnitCircle *
                                     (hitShakeDistance * (1f - normalized));
                    visualRoot.localPosition =
                        visualBaseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                }
                yield return null;
            }

            RestoreVisualFeedback();
            hitFeedbackRoutine = null;
        }

        private void SetVisualTint(bool red)
        {
            if (visualRenderers == null) return;
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Color baseColor = elementalRendererColors[i];
                Color target = red
                    ? Color.Lerp(baseColor, new Color(1f, 0.02f, 0.02f, 1f), 0.72f)
                    : baseColor;
                SetRendererColor(visualRenderers[i], target);
            }
        }

        private void RestoreVisualFeedback()
        {
            if (visualRoot != null) visualRoot.localPosition = visualBaseLocalPosition;
            if (visualRenderers == null) return;
            for (int i = 0; i < visualRenderers.Length; i++)
                SetRendererColor(visualRenderers[i], elementalRendererColors[i]);
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (renderer.sharedMaterial.HasProperty("_BaseColor")) block.SetColor("_BaseColor", color);
            if (renderer.sharedMaterial.HasProperty("_Color")) block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private IEnumerator PlayDeathAnimation(bool flyer)
        {
            Quaternion startRotation = visualRoot != null
                ? visualRoot.localRotation
                : Quaternion.identity;
            float duration = Mathf.Max(0.1f, flyer ? flyerFlipDuration : groundedFlipDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = normalized * normalized * (3f - 2f * normalized);
                if (visualRoot != null)
                    visualRoot.localRotation =
                        Quaternion.AngleAxis(180f * eased, Vector3.forward) * startRotation;
                yield return null;
            }

            if (visualRoot != null)
                visualRoot.localRotation = Quaternion.AngleAxis(180f, Vector3.forward) * startRotation;
            yield return new WaitForSeconds(Mathf.Max(0f, corpseSettleDelay));
            SettleCorpseOnFloor();
        }

        private void SettleCorpseOnFloor()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.useGravity = false;
            if (enemyCollider != null)
            {
                Vector3 settledPosition = transform.position;
                settledPosition.y = deathFloorSurfaceY + enemyCollider.bounds.extents.y + 0.01f;
                settledPosition.z = 0f;
                transform.position = settledPosition;
            }

            Physics.SyncTransforms();
            if (visualRoot != null)
                visualRoot.localPosition =
                    new Vector3(visualBaseLocalPosition.x, 0f, visualBaseLocalPosition.z);
            if (enemyCollider != null) enemyCollider.enabled = false;
            corpsePoseSettled = true;
        }

        private float ResolveDeathFloorSurface()
        {
            int physicalFloor = Mathf.Clamp(3 - floorIndex, 0, 3);
            GameObject floor = GameObject.Find("Floor_" + physicalFloor + "_Blocking");
            Collider floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
            if (floorCollider != null) return floorCollider.bounds.max.y;
            return enemyCollider != null
                ? transform.position.y - enemyCollider.bounds.extents.y
                : transform.position.y;
        }

        private void OnDestroy()
        {
            healthIndicator = null;
        }

        public void Configure(string id, EnemyDefinition value, int floor, bool key, bool choice,
            string overrideChoiceId = null)
        {
            enemyId = id;
            definition = value;
            floorIndex = floor;
            carriesKey = key;
            triggersChoice = choice;
            choiceIdOverride = overrideChoiceId;
        }
    }
}
