using System.Collections;
using UnityEngine;

namespace NidoCero
{
    public sealed class RescueCageController : MonoBehaviour
    {
        public const string StoryFlag = "piso2_rescue_complete";

        [SerializeField] private string[] requiredEnemyIds =
        {
            "P2_CANGRE_1",
            "P2_CANGRE_2",
            "P2_CANGRE_3"
        };
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private Collider cageCollider;
        [SerializeField] private Transform cageVisual;
        [SerializeField] private float openingHeight = 1.3f;
        [SerializeField] private float openingDuration = 0.8f;

        private bool released;
        private Vector3 closedLocalPosition;

        public bool IsReleased => released;

        private void Awake()
        {
            if (cageCollider == null) cageCollider = GetComponent<Collider>();
            if (cageVisual == null) cageVisual = transform.Find("Rescue_Cage_Visual");
            if (cageVisual != null) closedLocalPosition = cageVisual.localPosition;
        }

        private void OnEnable()
        {
            GameSession.StateChanged += RefreshFromProgress;
        }

        private void OnDisable()
        {
            GameSession.StateChanged -= RefreshFromProgress;
        }

        private void Start()
        {
            RefreshFromProgress();
        }

        public void RefreshFromProgress()
        {
            if (released || GameSession.Instance == null) return;
            RunState state = GameSession.Instance.State;
            if (state.storyFlags.Contains(StoryFlag))
            {
                Release(false);
                return;
            }

            if (requiredEnemyIds == null || requiredEnemyIds.Length == 0) return;
            foreach (string enemyId in requiredEnemyIds)
                if (!state.defeatedEnemies.Contains(enemyId)) return;

            state.storyFlags.Add(StoryFlag);
            Release(true);
            GameSession.Instance.NotifyChanged();
        }

        public void Configure(TextMesh label, Collider blockingCollider, Transform visual)
        {
            statusLabel = label;
            cageCollider = blockingCollider;
            cageVisual = visual;
            if (cageVisual != null) closedLocalPosition = cageVisual.localPosition;
        }

        private void Release(bool animate)
        {
            if (released) return;
            released = true;
            if (cageCollider != null) cageCollider.enabled = false;
            if (statusLabel != null)
            {
                statusLabel.text = "ANIMAL LIBERADO";
                statusLabel.color = new Color(0.45f, 1f, 0.55f);
            }
            TintCage();
            HudController.Instance?.ShowNotification("Hábitat liberado. El descenso puede abrirse.");

            if (cageVisual == null) return;
            if (animate && isActiveAndEnabled)
                StartCoroutine(OpenCage());
            else
                cageVisual.localPosition = closedLocalPosition + Vector3.up * openingHeight;
        }

        private IEnumerator OpenCage()
        {
            Vector3 start = cageVisual.localPosition;
            Vector3 target = closedLocalPosition + Vector3.up * openingHeight;
            float elapsed = 0f;
            while (elapsed < openingDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, openingDuration));
                float eased = normalized * normalized * (3f - 2f * normalized);
                cageVisual.localPosition = Vector3.Lerp(start, target, eased);
                yield return null;
            }
            cageVisual.localPosition = target;
        }

        private void TintCage()
        {
            if (cageVisual == null) return;
            foreach (Renderer renderer in cageVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color releasedColor = new Color(0.42f, 1f, 0.5f, 1f);
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", releasedColor);
                if (renderer.sharedMaterial.HasProperty("_Color"))
                    block.SetColor("_Color", releasedColor);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
