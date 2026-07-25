using UnityEngine;

namespace NidoCero
{
    public sealed class BossRelay : MonoBehaviour
    {
        [SerializeField] private int health = 2;
        [SerializeField] private Renderer targetRenderer;
        public bool IsAlive => health > 0 && gameObject.activeSelf;

        public void Hit()
        {
            if (!IsAlive) return;
            health--;
            if (targetRenderer != null)
                targetRenderer.material.color = health > 0 ? Color.yellow : Color.gray;
            if (health <= 0)
            {
                gameObject.SetActive(false);
                BossEncounter.Instance?.RelayDestroyed();
            }
        }

        public void Configure(Renderer value)
        {
            targetRenderer = value;
        }
    }
}
