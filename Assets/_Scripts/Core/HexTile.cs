using UnityEngine;

namespace LP.HexTileTDR.Core
{
    public class HexTile : MonoBehaviour
    {
        public TileSO Data { get; private set; }
        public Vector3Int GridPosition { get; private set; }

        public void Initialize(TileSO data, Vector3Int gridPos)
        {
            Data = data;
            GridPosition = gridPos;
        }
    }
}
