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

        [Header("Combat")]
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileRange = 9f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;

        private Rigidbody body;
        private Camera mainCamera;
        private float currentStamina;
        private float lastSpendTime;
        private float nextShotTime;
        private int currentLife;
        private bool grounded;
        private Vector3 checkpoint;
        private bool paused;
        private float moveInput;
        private bool runHeld;
        private float lastGroundedTime = float.NegativeInfinity;
        private float jumpQueuedUntil = float.NegativeInfinity;

        public float CurrentStamina => currentStamina;
        public int CurrentLife => currentLife;
        public Vector3 Checkpoint => checkpoint;

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
            currentLife = Stats.life;
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
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
                return;
            }

            UpdateGrounded();
            if (grounded) lastGroundedTime = Time.time;

            bool running = Mathf.Abs(moveInput) > 0.01f && runHeld && currentStamina > 0f;
            float staminaRatio = Mathf.Clamp01(currentStamina / Mathf.Max(1f, Stats.stamina));
            float staminaMovement = 0.5f + 0.5f * staminaRatio;
            float statMovement = 0.75f + Stats.speed * 0.05f;
            float targetSpeed =
                moveInput * baseMoveSpeed * statMovement * staminaMovement * (running ? runMultiplier : 1f);
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
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            lastSpendTime = Time.time;
        }

        private void RegenerateStamina()
        {
            if (Time.time - lastSpendTime < regenerationDelay) return;
            currentStamina = Mathf.MoveTowards(currentStamina, Stats.stamina,
                regenerationPerSecond * Time.deltaTime);
        }

        public void TakeDamage(int rawDamage)
        {
            int reduced = Mathf.Max(1, rawDamage - Stats.defense / 2);
            currentLife -= reduced;
            HudController.Instance?.PlayDamageFlash();
            CameraFollow cameraFollow = mainCamera != null
                ? mainCamera.GetComponent<CameraFollow>()
                : FindFirstObjectByType<CameraFollow>();
            cameraFollow?.PlayDamageShake();
            if (currentLife <= 0) Respawn(true);
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpoint = position;
        }

        public void Fall()
        {
            currentStamina = Mathf.Max(0f, currentStamina - 15f);
            Respawn(false);
        }

        private void Respawn(bool fullPenalty)
        {
            currentLife = fullPenalty ? Stats.life : Mathf.Max(1, currentLife);
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
                TakeDamage(1);
            }
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void RefreshVitalsAfterStatsChanged(int previousLifeMaximum, int previousStaminaMaximum)
        {
            int lifeDifference = Stats.life - previousLifeMaximum;
            int staminaDifference = Stats.stamina - previousStaminaMaximum;
            currentLife = Mathf.Clamp(currentLife + lifeDifference, 1, Mathf.Max(1, Stats.life));
            currentStamina = Mathf.Clamp(currentStamina + staminaDifference, 0f, Mathf.Max(1f, Stats.stamina));
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
