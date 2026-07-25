using UnityEngine;

namespace NidoCero
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class RobotEnemy : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private int floorIndex;
        [SerializeField] private bool carriesKey;
        [SerializeField] private bool triggersChoice;
        [SerializeField] private float patrolDistance = 3f;

        private Rigidbody body;
        private int health;
        private int stompCount;
        private float direction = 1f;
        private float originX;
        private float nextAttack;
        private PlayerController player;

        public string EnemyId => enemyId;
        public EnemyDefinition Definition => definition;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = definition == null || definition.archetype != EnemyArchetype.Flyer;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            originX = transform.position.x;
            health = definition != null ? Mathf.Max(1, definition.maxHealth) : 1;
        }

        private void Start()
        {
            if (GameSession.Instance != null && GameSession.Instance.State.defeatedEnemies.Contains(enemyId))
            {
                gameObject.SetActive(false);
                return;
            }

            player = FindFirstObjectByType<PlayerController>();
            nextAttack = Time.time + 1.5f;
        }

        private void FixedUpdate()
        {
            if (definition == null || player == null || CardChoiceController.IsOpen) return;

            float distance = player.transform.position.x - transform.position.x;
            bool sameBand = Mathf.Abs(player.transform.position.y - transform.position.y) < 3f;

            if (definition.archetype == EnemyArchetype.Flyer)
            {
                Vector3 hover = new Vector3(
                    originX + Mathf.Sin(Time.time * definition.moveSpeed) * patrolDistance,
                    transform.position.y + Mathf.Sin(Time.time * 2f) * 0.01f,
                    0f);
                body.MovePosition(hover);
            }
            else
            {
                if (Mathf.Abs(transform.position.x - originX) > patrolDistance)
                    direction = transform.position.x > originX ? -1f : 1f;
                body.linearVelocity = new Vector3(direction * definition.moveSpeed, body.linearVelocity.y, 0f);
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
            NidoProjectile projectile = NidoProjectile.Create(transform.position + directionVector * 0.9f, 0.19f);
            projectile.Launch(directionVector, 7f, false, gameObject,
                definition != null ? definition.color : Color.red,
                definition != null ? definition.element : ElementId.Fire,
                definition != null ? definition.elementLevel : 1);
        }

        public int ElementalBonusAgainst(ElementId attackElement, int attackLevel)
        {
            if (definition == null) return 0;
            ElementLevels levels = new ElementLevels();
            levels.Add(definition.element, definition.elementLevel);
            return ElementalResolver.Bonus(attackElement, attackLevel, levels);
        }

        public void TakeDamage(int amount)
        {
            health -= Mathf.Max(1, amount);
            if (health <= 0) Die();
        }

        public void Stomp()
        {
            stompCount++;
            int required = definition != null && definition.archetype == EnemyArchetype.Tank ? 2 : 1;
            if (stompCount >= required) Die();
        }

        private void Die()
        {
            if (GameSession.Instance != null)
            {
                RunState state = GameSession.Instance.State;
                if (!state.defeatedEnemies.Contains(enemyId)) state.defeatedEnemies.Add(enemyId);
                GameSession.Instance.NotifyChanged();
            }

            if (carriesKey) KeyPickup.Spawn(transform.position + Vector3.up * 0.8f, floorIndex);
            if (triggersChoice) CardChoiceController.Instance?.Open("floor_" + floorIndex);
            gameObject.SetActive(false);
        }

        public void Configure(string id, EnemyDefinition value, int floor, bool key, bool choice)
        {
            enemyId = id;
            definition = value;
            floorIndex = floor;
            carriesKey = key;
            triggersChoice = choice;
        }
    }
}
