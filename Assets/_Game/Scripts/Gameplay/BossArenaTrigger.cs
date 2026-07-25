using UnityEngine;

namespace NidoCero
{
    public sealed class BossArenaTrigger : MonoBehaviour
    {
        [SerializeField] private BossEncounter encounter;
        private bool activated;

        private void OnTriggerEnter(Collider other)
        {
            if (activated || other.GetComponentInParent<PlayerController>() == null) return;
            activated = true;
            encounter?.Begin();
        }

        public void Configure(BossEncounter value)
        {
            encounter = value;
        }
    }
}
