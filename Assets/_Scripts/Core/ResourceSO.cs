using UnityEngine;
using UnityEngine.UI;

namespace LP.HexTileTDR.Core
{
    [CreateAssetMenu(fileName = "Resource", menuName = "HexTileTDR/Resource")]
    public class ResourceSO : ScriptableObject
    {
        public string resourceName;
        public Image resourceIcon;
        public GameObject resourcePrefab;
    }

    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceSO resourceType;
        public int resourceAmount;
    }
}
