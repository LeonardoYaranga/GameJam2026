using UnityEngine;

namespace NidoCero
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DescendingElevatorController : MonoBehaviour
    {
        private enum ElevatorState
        {
            Waiting,
            Preparing,
            Descending,
            Arrived
        }

        [SerializeField] private int sourceFloorIndex;
        [SerializeField] private int destinationFloorIndex;
        [SerializeField] private float travelDistance = 5f;
        [SerializeField] private float departureDelay = 0.3f;
        [SerializeField] private float descentDuration = 1.8f;
        [SerializeField] private float destinationCheckpointX;
        [SerializeField] private GateUnlockZone departureGate;
        [SerializeField] private GameObject arrivalBarrier;
        [SerializeField] private TextMesh statusLabel;

        private Rigidbody elevatorBody;
        private ElevatorState state;
        private PlayerController passenger;
        private Rigidbody passengerBody;
        private bool passengerWasKinematic;
        private bool passengerUsedGravity;
        private float stateElapsed;
        private Vector3 topPosition;
        private Vector3 bottomPosition;
        private Vector3 passengerOffset;
        private CameraFollow cameraFollow;

        public int SourceFloorIndex => sourceFloorIndex;
        public int DestinationFloorIndex => destinationFloorIndex;
        public float TravelDistance => travelDistance;
        public bool IsDescending => state == ElevatorState.Preparing || state == ElevatorState.Descending;
        public bool IsAtDestination => state == ElevatorState.Arrived;
        public GameObject ArrivalBarrier => arrivalBarrier;

        private void Awake()
        {
            elevatorBody = GetComponent<Rigidbody>();
            elevatorBody.isKinematic = true;
            elevatorBody.useGravity = false;
            elevatorBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Start()
        {
            topPosition = transform.position;
            bottomPosition = topPosition + Vector3.down * Mathf.Max(0.1f, travelDistance);

            bool alreadyDescended = GameSession.Instance != null &&
                                    GameSession.Instance.State.currentFloor > sourceFloorIndex;
            if (alreadyDescended)
            {
                elevatorBody.position = bottomPosition;
                state = ElevatorState.Arrived;
                departureGate?.SealAfterDeparture();
                SetArrivalOpen(true);
                SetLabel("ASCENSOR EN PISO " + (3 - destinationFloorIndex));
            }
            else
            {
                state = ElevatorState.Waiting;
                SetArrivalOpen(false);
                SetLabel("SUBE AL ASCENSOR");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) BeginDescent(player);
        }

        private void FixedUpdate()
        {
            if (state == ElevatorState.Preparing)
            {
                stateElapsed += Time.fixedDeltaTime;
                if (stateElapsed < Mathf.Max(0f, departureDelay)) return;

                state = ElevatorState.Descending;
                stateElapsed = 0f;
                departureGate?.SealAfterDeparture();
                SetLabel("DESCENDIENDO");
                return;
            }

            if (state != ElevatorState.Descending) return;

            stateElapsed += Time.fixedDeltaTime;
            float duration = Mathf.Max(0.1f, descentDuration);
            float t = Mathf.Clamp01(stateElapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 nextPosition = Vector3.Lerp(topPosition, bottomPosition, eased);
            elevatorBody.MovePosition(nextPosition);
            if (passengerBody != null)
                passengerBody.position = nextPosition + passengerOffset;
            if (t >= 1f) CompleteDescent();
        }

        public bool BeginDescent(PlayerController player)
        {
            if (player == null || state != ElevatorState.Waiting ||
                departureGate == null || !departureGate.IsOpen)
                return false;

            passenger = player;
            passengerBody = player.GetComponent<Rigidbody>();
            if (passengerBody == null) return false;

            passengerWasKinematic = passengerBody.isKinematic;
            passengerUsedGravity = passengerBody.useGravity;
            passengerBody.linearVelocity = Vector3.zero;
            passengerBody.angularVelocity = Vector3.zero;
            passengerBody.isKinematic = true;
            passengerBody.useGravity = false;
            passenger.enabled = false;
            passengerOffset = passengerBody.position - topPosition;

            cameraFollow = Camera.main != null
                ? Camera.main.GetComponent<CameraFollow>()
                : FindFirstObjectByType<CameraFollow>();
            cameraFollow?.SetElevatorTransition(true);

            state = ElevatorState.Preparing;
            stateElapsed = 0f;
            SetArrivalOpen(false);
            SetLabel("PUERTAS CERRANDO");
            HudController.Instance?.ShowNotification("Ascensor en descenso.");
            return true;
        }

        private void CompleteDescent()
        {
            elevatorBody.position = bottomPosition;
            state = ElevatorState.Arrived;
            SetArrivalOpen(true);
            SetLabel("PISO " + (3 - destinationFloorIndex) + " · SALIDA");

            if (passenger != null)
            {
                Vector3 safePosition = passenger.transform.position;
                safePosition.y = bottomPosition.y + 1.55f;
                safePosition.z = 0f;
                passenger.transform.position = safePosition;
                passenger.SetCheckpoint(new Vector3(
                    destinationCheckpointX,
                    bottomPosition.y + 1.55f,
                    0f));
                passenger.enabled = true;
            }

            if (passengerBody != null)
            {
                passengerBody.isKinematic = passengerWasKinematic;
                passengerBody.useGravity = passengerUsedGravity;
                passengerBody.linearVelocity = Vector3.zero;
            }

            if (GameSession.Instance != null)
            {
                GameSession.Instance.State.currentFloor =
                    Mathf.Max(GameSession.Instance.State.currentFloor, destinationFloorIndex);
                GameSession.Instance.NotifyChanged();
            }

            cameraFollow?.SetElevatorTransition(false);
            HudController.Instance?.ShowNotification(
                "Piso " + (3 - destinationFloorIndex) + " alcanzado. Nido registrado.");
        }

        private void SetArrivalOpen(bool open)
        {
            if (arrivalBarrier != null) arrivalBarrier.SetActive(!open);
        }

        private void SetLabel(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }

        public void Configure(int sourceFloor, int destinationFloor, float distance,
            float checkpointX, GateUnlockZone gate, GameObject destinationBarrier, TextMesh label)
        {
            sourceFloorIndex = sourceFloor;
            destinationFloorIndex = destinationFloor;
            travelDistance = Mathf.Max(0.1f, distance);
            destinationCheckpointX = checkpointX;
            departureGate = gate;
            arrivalBarrier = destinationBarrier;
            statusLabel = label;
        }
    }
}
