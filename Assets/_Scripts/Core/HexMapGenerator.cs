using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LP.HexTileTDR.Core
{
    public enum TileType
    {
        Grass,
        Tree,
        Resource
    }

    public class HexMapGenerator : MonoBehaviour
    {
        [Header("Grid References")]
        [SerializeField] private Grid hexGrid;

        [Header("Tile Prefabs")]
        [SerializeField] private GameObject grassTilePrefab;
        [SerializeField] private GameObject treeTilePrefab;
        [SerializeField] private GameObject resourceTilePrefab;

        [Header("Generation Parameters")]
        [SerializeField] private int grassTileCount = 15;
        [SerializeField] private int treeTileCount = 8;
        [SerializeField] private int resourceTileCount = 4;

        [Header("Procedural Weights")]
        [Range(1, 10)]
        [SerializeField] private int treeClusterWeight = 5;

        private readonly Dictionary<Vector3Int, TileType> mapGrid = new Dictionary<Vector3Int, TileType>();

        private void Start()
        {
            if (!HasExistingMap())
            {
                GenerateMap();
            }
            else
            {
                RegisterExistingMap();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                GenerateMap();
            }
        }

        public bool HasExistingMap()
        {
            Transform container = hexGrid != null ? hexGrid.transform : transform;
            foreach (Transform child in container)
            {
                if (child.name.Contains("Ghost")) continue;
                return true;
            }
            return false;
        }

        private void RegisterExistingMap()
        {
            mapGrid.Clear();
            Transform container = hexGrid != null ? hexGrid.transform : transform;

            foreach (Transform child in container)
            {
                if (child.name.Contains("Ghost")) continue;

                Vector3Int cellPos = hexGrid.WorldToCell(child.position);
                if (!mapGrid.ContainsKey(cellPos))
                {
                    mapGrid.Add(cellPos, TileType.Grass);
                }
            }
        }

        [ContextMenu("Regenerate Map")]
        public void GenerateMap()
        {
            ClearMap();

            int totalTiles = grassTileCount + treeTileCount + resourceTileCount;
            if (totalTiles <= 0) return;

            int remainingGrass = grassTileCount;
            int remainingTrees = treeTileCount;
            int remainingResources = resourceTileCount;

            Vector3Int origin = Vector3Int.zero;
            TileType firstType = SelectInitialType(ref remainingGrass, ref remainingTrees, ref remainingResources);
            mapGrid.Add(origin, firstType);

            List<Vector3Int> frontier = GetEmptyNeighbors(origin);

            while (mapGrid.Count < totalTiles && frontier.Count > 0)
            {
                int frontierIndex = Random.Range(0, frontier.Count);
                Vector3Int targetCell = frontier[frontierIndex];
                frontier.RemoveAt(frontierIndex);

                List<TileType> validTypes = GetValidTypesForPosition(targetCell, remainingGrass, remainingTrees, remainingResources);

                if (validTypes.Count == 0) continue;

                TileType selectedType = SelectWeightedType(targetCell, validTypes, remainingGrass, remainingTrees, remainingResources);

                mapGrid.Add(targetCell, selectedType);
                DecrementTypeCount(selectedType, ref remainingGrass, ref remainingTrees, ref remainingResources);

                Vector3Int[] neighbors = GetHexNeighbors(targetCell);
                foreach (var n in neighbors)
                {
                    if (!mapGrid.ContainsKey(n) && !frontier.Contains(n))
                    {
                        frontier.Add(n);
                    }
                }
            }

            // Instantiate New Prefabs
            InstantiateMapTiles();

            // Re-sync Placement Manager's occupancy tracking
            HexPlacementManager placementManager = FindAnyObjectByType<HexPlacementManager>();
            if (placementManager != null)
            {
                placementManager.ClearAndRegisterTiles();
            }
        }

        private TileType SelectInitialType(ref int grass, ref int trees, ref int resources)
        {
            List<TileType> pool = new List<TileType>();
            if (grass > 0) pool.Add(TileType.Grass);
            if (trees > 0) pool.Add(TileType.Tree);
            if (resources > 0) pool.Add(TileType.Resource);

            TileType selected = pool[Random.Range(0, pool.Count)];
            DecrementTypeCount(selected, ref grass, ref trees, ref resources);
            return selected;
        }

        private List<TileType> GetValidTypesForPosition(Vector3Int cellPos, int grass, int trees, int resources)
        {
            List<TileType> valid = new List<TileType>();

            if (grass > 0) valid.Add(TileType.Grass);
            if (trees > 0) valid.Add(TileType.Tree);

            if (resources > 0 && !HasAdjacentType(cellPos, TileType.Resource))
            {
                valid.Add(TileType.Resource);
            }

            return valid;
        }

        private TileType SelectWeightedType(Vector3Int cellPos, List<TileType> validTypes, int grass, int trees, int resources)
        {
            int treeScore = 0;
            int grassScore = 0;
            int resourceScore = 0;

            if (validTypes.Contains(TileType.Tree))
            {
                int treeNeighbors = CountAdjacentType(cellPos, TileType.Tree);
                treeScore = trees * (1 + (treeNeighbors * treeClusterWeight));
            }

            if (validTypes.Contains(TileType.Grass))
            {
                grassScore = grass;
            }

            if (validTypes.Contains(TileType.Resource))
            {
                resourceScore = resources;
            }

            int totalWeight = treeScore + grassScore + resourceScore;
            int randomVal = Random.Range(0, totalWeight);

            if (randomVal < treeScore) return TileType.Tree;
            if (randomVal < treeScore + grassScore) return TileType.Grass;
            return TileType.Resource;
        }

        private bool HasAdjacentType(Vector3Int cellPos, TileType type)
        {
            return CountAdjacentType(cellPos, type) > 0;
        }

        private int CountAdjacentType(Vector3Int cellPos, TileType type)
        {
            int count = 0;
            foreach (var neighbor in GetHexNeighbors(cellPos))
            {
                if (mapGrid.TryGetValue(neighbor, out TileType neighborType) && neighborType == type)
                {
                    count++;
                }
            }
            return count;
        }

        private List<Vector3Int> GetEmptyNeighbors(Vector3Int cellPos)
        {
            List<Vector3Int> empty = new List<Vector3Int>();
            foreach (var neighbor in GetHexNeighbors(cellPos))
            {
                if (!mapGrid.ContainsKey(neighbor))
                {
                    empty.Add(neighbor);
                }
            }
            return empty;
        }

        private void DecrementTypeCount(TileType type, ref int grass, ref int trees, ref int resources)
        {
            switch (type)
            {
                case TileType.Grass: grass--; break;
                case TileType.Tree: trees--; break;
                case TileType.Resource: resources--; break;
            }
        }

        private void InstantiateMapTiles()
        {
            Transform parentTransform = hexGrid != null ? hexGrid.transform : transform;

            foreach (var kvp in mapGrid)
            {
                Vector3 worldPos = hexGrid.GetCellCenterWorld(kvp.Key);
                GameObject prefabToSpawn = GetPrefabForType(kvp.Value);

                if (prefabToSpawn != null)
                {
                    Instantiate(prefabToSpawn, worldPos, Quaternion.identity, parentTransform);
                }
            }
        }

        private GameObject GetPrefabForType(TileType type)
        {
            switch (type)
            {
                case TileType.Grass: return grassTilePrefab;
                case TileType.Tree: return treeTilePrefab;
                case TileType.Resource: return resourceTilePrefab;
                default: return null;
            }
        }

        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            mapGrid.Clear();
            Transform container = hexGrid != null ? hexGrid.transform : transform;

            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in container)
            {
                if (!child.name.Contains("Ghost"))
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject obj in childrenToDestroy)
            {
                obj.transform.SetParent(null);

                foreach (Collider c in obj.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }

                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }

        private Vector3Int[] GetHexNeighbors(Vector3Int cell)
        {
            bool isEvenRow = Mathf.Abs(cell.y) % 2 == 0;

            if (isEvenRow)
            {
                return new Vector3Int[]
                {
                    new Vector3Int(cell.x + 1, cell.y, 0),
                    new Vector3Int(cell.x - 1, cell.y, 0),
                    new Vector3Int(cell.x, cell.y + 1, 0),
                    new Vector3Int(cell.x, cell.y - 1, 0),
                    new Vector3Int(cell.x - 1, cell.y + 1, 0),
                    new Vector3Int(cell.x - 1, cell.y - 1, 0)
                };
            }
            else
            {
                return new Vector3Int[]
                {
                    new Vector3Int(cell.x + 1, cell.y, 0),
                    new Vector3Int(cell.x - 1, cell.y, 0),
                    new Vector3Int(cell.x, cell.y + 1, 0),
                    new Vector3Int(cell.x, cell.y - 1, 0),
                    new Vector3Int(cell.x + 1, cell.y + 1, 0),
                    new Vector3Int(cell.x + 1, cell.y - 1, 0)
                };
            }
        }
    }
}