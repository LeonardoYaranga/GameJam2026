using System;
using UnityEngine;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class FinalSacrificeController : MonoBehaviour
    {
        public static FinalSacrificeController Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private GameObject overlay;
        [SerializeField] private Image holdFill;
        [SerializeField] private Text progressText;
        [SerializeField] private float holdDuration = 1.5f;
        [SerializeField] private float fillMaxAnchor = 0.99f;

        private float heldTime;
        private Action confirmed;

        public float HoldProgress =>
            Mathf.Clamp01(heldTime / Mathf.Max(0.1f, holdDuration));

        private void Awake()
        {
            Instance = this;
            IsOpen = false;
            if (overlay != null) overlay.SetActive(false);
            RefreshProgress();
        }

        private void Update()
        {
            if (!IsOpen) return;
            bool holding = Input.GetKey(KeyCode.E) ||
                           Input.GetKey(KeyCode.Space) ||
                           Input.GetKey(KeyCode.Return) ||
                           Input.GetMouseButton(0);
            heldTime = holding
                ? heldTime + Time.unscaledDeltaTime
                : Mathf.Max(0f, heldTime - Time.unscaledDeltaTime * 1.5f);
            RefreshProgress();
            if (heldTime >= Mathf.Max(0.1f, holdDuration)) ConfirmNow();
        }

        public bool Begin(Action onConfirmed)
        {
            if (IsOpen || overlay == null) return false;
            confirmed = onConfirmed;
            heldTime = 0f;
            IsOpen = true;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            RefreshProgress();
            return true;
        }

        public void ConfirmNow()
        {
            if (!IsOpen) return;
            if (GameSession.Instance != null)
            {
                RunState state = GameSession.Instance.State;
                state.stats = new RuntimeStats();
                state.stats.ResetToCatalog(GameSession.Instance.Catalog);
                state.elements = new ElementLevels();
                GameSession.Instance.NotifyChanged();
            }

            Action callback = confirmed;
            confirmed = null;
            IsOpen = false;
            if (overlay != null) overlay.SetActive(false);
            Time.timeScale = 1f;
            callback?.Invoke();
        }

        public void Configure(GameObject overlayObject, Image fill, Text progress)
        {
            overlay = overlayObject;
            holdFill = fill;
            progressText = progress;
            if (overlay != null) overlay.SetActive(false);
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            float progress = HoldProgress;
            if (holdFill != null)
            {
                holdFill.fillAmount = progress;
                RectTransform fillTransform = holdFill.rectTransform;
                Vector2 anchorMax = fillTransform.anchorMax;
                anchorMax.x = Mathf.Lerp(fillTransform.anchorMin.x, fillMaxAnchor, progress);
                fillTransform.anchorMax = anchorMax;
            }
            if (progressText != null)
                progressText.text = progress <= 0f
                    ? "MANTÉN [E / ESPACIO / CLIC] — CONFIRMAR"
                    : "CONFIRMANDO " + Mathf.RoundToInt(progress * 100f) + "%";
        }
    }
}
