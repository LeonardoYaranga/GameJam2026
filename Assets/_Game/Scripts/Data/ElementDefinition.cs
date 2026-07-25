using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Element Definition")]
    public sealed class ElementDefinition : ScriptableObject
    {
        public ElementId id;
        public string displayName;
        public Color color = Color.white;
        [TextArea] public string description;
    }
}
