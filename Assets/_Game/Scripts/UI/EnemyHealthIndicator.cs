using UnityEngine;
using UnityEngine.UI;

namespace NidoCero
{
    public sealed class EnemyHealthIndicator : MonoBehaviour
    {
        private RobotEnemy target;
        private Image fill;
        private float verticalOffset;
        private const float FillMaximumAnchor = 0.97f;

        public float FillAmount => fill != null ? fill.fillAmount : 0f;
        public RectTransform FillTransform => fill != null ? fill.rectTransform : null;

        public static EnemyHealthIndicator Create(RobotEnemy enemy, Color elementalColor,
            float yOffset, float width)
        {
            GameObject root = new GameObject(
                "HealthIndicator_" + enemy.gameObject.name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(EnemyHealthIndicator));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(Mathf.Max(1.35f, width) * 100f, 16f);
            rootRect.localScale = Vector3.one * 0.01f;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 80;

            Image background = CreateImage(root.transform, "Background",
                new Color(0.015f, 0.02f, 0.025f, 0.92f));
            Stretch(background.rectTransform, Vector2.zero, Vector2.one);

            Image damageTrack = CreateImage(root.transform, "DamageTrack",
                new Color(0.3f, 0.04f, 0.04f, 0.94f));
            Stretch(damageTrack.rectTransform, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f));

            Image healthFill = CreateImage(root.transform, "HealthFill",
                Color.Lerp(new Color(0.25f, 0.9f, 0.38f, 1f), elementalColor, 0.3f));
            Stretch(healthFill.rectTransform, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f));
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = 0;

            EnemyHealthIndicator indicator = root.GetComponent<EnemyHealthIndicator>();
            indicator.target = enemy;
            indicator.fill = healthFill;
            indicator.verticalOffset = yOffset;
            indicator.SetHealth(1f);
            indicator.FollowTarget();
            return indicator;
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            FollowTarget();
        }

        public void SetHealth(float normalized)
        {
            if (fill == null) return;
            float value = Mathf.Clamp01(normalized);
            fill.fillAmount = value;
            RectTransform rect = fill.rectTransform;
            Vector2 maximum = rect.anchorMax;
            maximum.x = Mathf.Lerp(rect.anchorMin.x, FillMaximumAnchor, value);
            rect.anchorMax = maximum;
        }

        public void Dispose()
        {
            if (gameObject != null) Destroy(gameObject);
        }

        private void FollowTarget()
        {
            if (target == null) return;
            Transform root = transform;
            Vector3 position = target.transform.position +
                               new Vector3(0f, verticalOffset, -1.15f);
            root.position = position;
            Camera camera = Camera.main;
            root.rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
