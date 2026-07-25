using UnityEngine;

namespace NidoCero
{
    public sealed class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            other.GetComponentInParent<PlayerController>()?.Fall();
        }
    }
}
