using UnityEngine;

namespace Pete.Data
{
    [CreateAssetMenu(fileName = "NewChant", menuName = "Pete/Data/Ghost Chant")]
    public class GhostChantSO : ScriptableObject
    {
        public string chantId;
        public string chantName;
        [TextArea(2, 5)]
        public string chantDescription;
        public GhostType countersGhostType;
    }
}
