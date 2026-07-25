using UnityEngine;
using UnityEngine.EventSystems;

namespace NidoCero
{
    public sealed class CardButtonProxy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int index;
        [SerializeField] private CardChoiceController controller;

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.Choose(index);
        }

        public void Configure(int value, CardChoiceController target)
        {
            index = value;
            controller = target;
        }
    }
}
