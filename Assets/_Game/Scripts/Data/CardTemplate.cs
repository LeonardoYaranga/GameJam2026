using UnityEngine;

namespace NidoCero
{
    [CreateAssetMenu(menuName = "Nido Cero/Data/Card Template")]
    public sealed class CardTemplate : ScriptableObject
    {
        public string cardId;
        public string title;
        [TextArea] public string description;
        public StatId gainStat;
        public int gainAmount = 1;
        public StatId lossStat;
        public int lossAmount = 1;
        public ElementId element;
        public int elementAmount = 1;
        public Color accent = Color.white;
    }
}
