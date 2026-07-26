using UnityEngine;
using UnityEngine.SceneManagement;

namespace NidoCero
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 6.25f;
        [SerializeField] private float runMultiplier = 1.55f;
        [SerializeField] private float jumpForce = 7.4f;
        [SerializeField] private float groundAcceleration = 58f;
        [SerializeField] private float groundDeceleration = 76f;
        [SerializeField] private float airAcceleration = 34f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.14f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Resources")]
        [SerializeField] private float runCostPerSecond = 12f;
        [SerializeField] private float jumpCost = 18f;
        [SerializeField] private float shootCost = 10f;
        [SerializeField] private float regenerationPerSecond = 22f;
        [SerializeField] private float regenerationDelay = 1f;
        [SerializeField, Range(0.05f, 0.75f)] private float sprintRecoveryThreshold = 0.25f;

        [Header("Combat")]
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileRange = 9f;
        [SerializeField] private float damageImmunityDuration = 0.28f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;

        private Rigidbody body;
        private Camera mainCamera;
        private float currentStamina;
        private float lastSpendTime;
        private float nextShotTime;
        private float currentLifeNormalized = 1f;
        private float nextDamageTime;
        private bool grounded;
        private Vector3 checkpoint;
        private bool paused;
        private float moveInput;
        private bool runHeld;
        private bool sprintExhausted;
        private float lastGroundedTime = float.NegativeInfinity;
        private float jumpQueuedUntil = float.NegativeInfinity;

        public float CurrentStamina => currentStamina;
        public float CurrentStaminaNormalized =>
            Mathf.Clamp01(currentStamina / Mathf.Max(1f, Stats.stamina));
        public int CurrentLife =>
            Mathf.Clamp(Mathf.CeilToInt(Stats.life * currentLifeNormalized), 0, Mathf.Max(1, Stats.life));
        public float CurrentLifeNormalized => Mathf.Clamp01(currentLifeNormalized);
        public bool IsSprinting { get; private set; }
        public bool IsSprintExhausted => sprintExhausted;
        public Vector3 Checkpoint => checkpoint;
        public float LastDamagePercent { get; private set; }
        public int RespawnCount { get; private set; }

        private RuntimeStats Stats =>
            GameSession.Instance != null ? GameSession.Instance.State.stats : new RuntimeStats();

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            mainCamera = Camera.main;
            if (visualRoot == null) visualRoot = transform.Find("Piquero_Visual");
            checkpoint = transform.position;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            currentStamina = Stats.stamina;
            currentLifeNormalized = 1f;
            sprintExhausted = false;
            IsSprinting = false;
            HudController.Instance?.BindPlayer(this);
        }

        private void Update()
        {
            if (!CardChoiceController.IsOpen && !DialogueController.IsOpen &&
                !FinalSacrificeController.IsOpen &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
            {
                if (HudController.Instance != null)
                    HudController.Instance.TogglePause();
                else
                {
                    paused = !paused;
                    Time.timeScale = paused ? 0f : 1f;
                }
            }

            if (paused || HudController.PauseActive || CardChoiceController.IsOpen ||
                DialogueController.IsOpen || FinalSacrificeController.IsOpen) return;

            moveInput = Input.GetAxisRaw("Horizontal");
            runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            UpdateVisualFacing();
            if (Input.GetKeyDown(KeyCode.Space))
                jumpQueuedUntil = Time.time + jumpBufferTime;

            HandleShoot();
            RegenerateStamina();
        }

        private void FixedUpdate()
        {
            if (paused || HudController.PauseActive || CardChoiceController.IsOpen ||
                DialogueController.IsOpen || FinalSacrificeController.IsOpen)
            {
                IsSprinting = false;
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
                return;
            }

            UpdateGrounded();
            if (grounded) lastGroundedTime = Time.time;

            bool running = Mathf.Abs(moveInput) > 0.01f && runHeld &&
                           !sprintExhausted && currentStamina > 0.01f;
            IsSprinting = running;
            float statMovement = 0.75f + Stats.speed * 0.05f;
            float targetSpeed =
                moveInput * baseMoveSpeed * statMovement * (running ? runMultiplier : 1f);
            float acceleration = grounded
                ? (Mathf.Abs(moveInput) > 0.01f ? groundAcceleration : groundDeceleration)
                : airAcceleration;
            float horizontalSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed,
                acceleration * Time.fixedDeltaTime);
            body.linearVelocity = new Vector3(horizontalSpeed, body.linearVelocity.y, 0f);

            if (running) SpendStamina(runCostPerSecond * Time.fixedDeltaTime);
            HandleJump();
        }

        private void HandleJump()
        {
            bool jumpBuffered = Time.time <= jumpQueuedUntil;
            bool canUseCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;
            if (!jumpBuffered || !canUseCoyoteTime || currentStamina < jumpCost) return;

            jumpQueuedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            grounded = false;
            SpendStamina(jumpCost);
            body.linearVelocity = new Vector3(body.linearVelocity.x, jumpForce, 0f);
        }

        private void HandleShoot()
        {
            if (!Input.GetMouseButton(0) || currentStamina < shootCost || Time.time < nextShotTime) return;

            float cadence = 1.4f + Stats.agility * 0.12f;
            nextShotTime = Time.time + 1f / cadence;
            SpendStamina(shootCost);

            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 target = mainCamera != null
                ? mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                    Mathf.Abs(mainCamera.transform.position.z)))
                : transform.position + Vector3.right;
            target.z = 0f;
            Vector3 direction = (target - transform.position).normalized;
            if (direction.sqrMagnitude < 0.1f) direction = Vector3.right;

            ElementId element = StrongestElement();
            int level = GameSession.Instance != null ? GameSession.Instance.State.elements.Get(element) : 0;
            Color color = GameSession.Instance != null && GameSession.Instance.Catalog != null
                ? GameSession.Instance.Catalog.FindElement(element)?.color ?? Color.cyan
                : Color.cyan;

            Vector3 origin = transform.position + direction * 0.9f;
            NidoProjectile projectile = NidoProjectile.Create(origin);
            projectile.Launch(direction, projectileSpeed, true, gameObject, color, element, level);
            Destroy(projectile.gameObject, projectileRange / projectileSpeed);
        }

        private ElementId StrongestElement()
        {
            if (GameSession.Instance == null) return ElementId.Water;
            ElementLevels levels = GameSession.Instance.State.elements;
            ElementId result = ElementId.Water;
            if (levels.fire > levels.water) result = ElementId.Fire;
            if (levels.vegetation > levels.Get(result)) result = ElementId.Vegetation;
            return result;
        }

        private void UpdateGrounded()
        {
            grounded = Physics.Raycast(transform.position, Vector3.down, 1.15f, groundMask,
                QueryTriggerInteraction.Ignore);
        }

        private void SpendStamina(float amount)
        {
            if (amount <= 0f) return;
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            lastSpendTime = Time.time;
            if (currentStamina <= 0.01f)
            {
                currentStamina = 0f;
                sprintExhausted = true;
                IsSprinting = false;
            }
        }

        private void RegenerateStamina()
        {
            if (Time.time - lastSpendTime < regenerationDelay) return;
            currentStamina = Mathf.MoveTowards(currentStamina, Stats.stamina,
                regenerationPerSecond * Time.deltaTime);
            if (sprintExhausted &&
                currentStamina >= Mathf.Max(1f, Stats.stamina) * sprintRecoveryThreshold)
                sprintExhausted = false;
        }

        public void TakeDamage(int rawDamage)
        {
            float rawPercent = 100f * Mathf.Max(1, rawDamage) / Mathf.Max(1f, Stats.life);
            ApplyDamagePercent(rawPercent);
        }

        public float TakeCombatDamage(ElementId attackElement, int attackLevel,
            EnemyArchetype attackerArchetype, bool contact)
        {
            if (Time.time < nextDamageTime) return 0f;
            float damagePercent = CombatMath.PlayerIncomingDamagePercent(
                Stats,
                GameSession.Instance != null ? GameSession.Instance.State.elements : new ElementLevels(),
                attackElement,
                attackLevel,
                attackerArchetype,
                contact);
            nextDamageTime = Time.time + Mathf.Max(0f, damageImmunityDuration);
            ApplyDamagePercent(damagePercent);
            return damagePercent;
        }

        private void ApplyDamagePercent(float damagePercent)
        {
            LastDamagePercent = Mathf.Clamp(damagePercent, 0f, 100f);
            currentLifeNormalized = Mathf.Max(0f,
                currentLifeNormalized - LastDamagePercent / 100f);
            HudController.Instance?.PlayDamageFlash();
            CameraFollow cameraFollow = mainCamera != null
                ? mainCamera.GetComponent<CameraFollow>()
                : FindFirstObjectByType<CameraFollow>();
            cameraFollow?.PlayDamageShake();
            if (currentLifeNormalized <= 0f) Respawn(true);
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpoint = position;
        }

        public void Fall()
        {
            SpendStamina(15f);
            Respawn(false);
        }

        private void Respawn(bool fullPenalty)
        {
            if (fullPenalty)
            {
                currentLifeNormalized = 1f;
                RespawnCount++;
            }
            body.linearVelocity = Vector3.zero;
            transform.position = checkpoint;
        }

        private void OnCollisionEnter(Collision collision)
        {
            RobotEnemy enemy = collision.collider.GetComponentInParent<RobotEnemy>();
            if (enemy == null || enemy.IsDead) return;

            if (body.linearVelocity.y <= 0.25f && transform.position.y > enemy.transform.position.y + 0.5f)
            {
                enemy.Stomp();
                body.linearVelocity = new Vector3(body.linearVelocity.x, jumpForce * 0.65f, 0f);
            }
            else
            {
                EnemyDefinition enemyDefinition = enemy.Definition;
                TakeCombatDamage(
                    enemyDefinition != null ? enemyDefinition.element : ElementId.Fire,
                    enemyDefinition != null ? enemyDefinition.elementLevel : 1,
                    enemyDefinition != null ? enemyDefinition.archetype : EnemyArchetype.Walker,
                    true);
            }
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void RefreshVitalsAfterStatsChanged(int previousLifeMaximum, int previousStaminaMaximum)
        {
            float previousLifePoints = Mathf.Max(1, previousLifeMaximum) * currentLifeNormalized;
            int lifeDifference = Stats.life - previousLifeMaximum;
            int staminaDifference = Stats.stamina - previousStaminaMaximum;
            float nextLifePoints = Mathf.Clamp(
                previousLifePoints + lifeDifference,
                1f,
                Mathf.Max(1f, Stats.life));
            currentLifeNormalized = nextLifePoints / Mathf.Max(1f, Stats.life);
            currentStamina = Mathf.Clamp(currentStamina + staminaDifference, 0f, Mathf.Max(1f, Stats.stamina));
            if (currentStamina <= 0.01f)
                sprintExhausted = true;
            else if (currentStamina >= Mathf.Max(1f, Stats.stamina) * sprintRecoveryThreshold)
                sprintExhausted = false;
        }

        public void ConfigureVisual(Transform value)
        {
            visualRoot = value;
        }

        private void UpdateVisualFacing()
        {
            if (visualRoot == null || Mathf.Abs(moveInput) < 0.01f) return;
            visualRoot.localRotation = Quaternion.Euler(0f, moveInput > 0f ? 90f : -90f, 0f);
        }
    }
}
