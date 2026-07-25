using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Floor Definition")]
    public sealed class FloorDefinition : ScriptableObject
    {
        public int floorIndex;
        public string displayName;
        public Color ambientColor;
        public ElementId dominantElement;
        [TextArea] public string objective;
    }
}
