using UnityEngine;
using UnityEngine.SceneManagement;

namespace NidoCero
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float runMultiplier = 1.55f;
        [SerializeField] private float jumpForce = 7f;
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

        private Rigidbody body;
        private Camera mainCamera;
        private float currentStamina;
        private float lastSpendTime;
        private float nextShotTime;
        private int currentLife;
        private bool grounded;
        private Vector3 checkpoint;
        private bool paused;

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
            mainCamera = Camera.main;
            checkpoint = transform.position;
        }

        private void Start()
        {
            currentStamina = Stats.stamina;
            currentLife = Stats.life;
            HudController.Instance?.BindPlayer(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
                HudController.Instance?.SetPause(paused);
            }

            if (paused || CardChoiceController.IsOpen) return;

            UpdateGrounded();
            HandleJump();
            HandleShoot();
            RegenerateStamina();
        }

        private void FixedUpdate()
        {
            if (paused || CardChoiceController.IsOpen) return;

            float input = Input.GetAxisRaw("Horizontal");
            bool running = Mathf.Abs(input) > 0.01f && Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f;
            float staminaRatio = Mathf.Clamp01(currentStamina / Mathf.Max(1f, Stats.stamina));
            float staminaMovement = 0.5f + 0.5f * staminaRatio;
            float statMovement = 0.75f + Stats.speed * 0.05f;
            float velocity = baseMoveSpeed * statMovement * staminaMovement * (running ? runMultiplier : 1f);
            body.linearVelocity = new Vector3(input * velocity, body.linearVelocity.y, 0f);

            if (running) SpendStamina(runCostPerSecond * Time.fixedDeltaTime);
        }

        private void HandleJump()
        {
            if (!grounded || !Input.GetKeyDown(KeyCode.Space) || currentStamina < jumpCost) return;
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
            if (enemy == null) return;

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
    }
}
