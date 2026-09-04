using UnityEngine;
using System.Collections.Generic;

namespace LP.HexTileTDR.Core
{
    [CreateAssetMenu(fileName = "Tile", menuName = "HexTileTDR/Tile")]
    public class TileSO : ScriptableObject
    {
        public string tileName;
        public enum TileCategory { Environment, Resource, Utility, Weapon};
        public TileCategory tileCategory;
        public int powerCost;
        public List<ResourceCost> resourceCosts;
        public GameObject tilePrefab;
    }
}
