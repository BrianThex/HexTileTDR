using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LP.HexTileTDR.Core
{
    public class HexPlacementManager : MonoBehaviour
    {
        [Header("Grid Setup")]
        [SerializeField] private Grid hexGrid;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private LayerMask tileLayerMask;

        [Header("Tile Selection")]
        [SerializeField] private GameObject selectedTilePrefab;

        [Header("Ghost Preview Settings")]
        [SerializeField] private bool enableGhostPreview = true;
        [SerializeField] private Material ghostMaterial;

        private readonly Dictionary<Vector3Int, GameObject> placedTiles = new Dictionary<Vector3Int, GameObject>();
        private GameObject ghostTileInstance;
        private bool isGhostActive;
        private float currentYRotation = 0f;

        private void Start()
        {
            RegisterPreplacedTiles();
        }

        private void Update()
        {
            if (selectedTilePrefab == null)
            {
                SetGhostVisibility(false);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetGhostVisibility(false);
                return;
            }

            Vector2 pointerPosition = GetCurrentPointerPosition();
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
            {
                Vector3Int cellPos = hexGrid.WorldToCell(hit.point);
                Vector3 cellPosWorld = hexGrid.GetCellCenterWorld(cellPos);

                if (enableGhostPreview && CanPlaceAt(cellPos))
                {
                    UpdateGhostTransform(cellPosWorld);
                }
                else
                {
                    SetGhostVisibility(false);
                }
            }
            else
            {
                SetGhostVisibility(false);
            }
        }

        public void SelectTilePrefab(GameObject prefab)
        {
            selectedTilePrefab = prefab;
            RebuildGhostInstance();
        }

        public void CancelPlacement()
        {
            if (selectedTilePrefab == null && ghostTileInstance == null) return;

            selectedTilePrefab = null;
            if (ghostTileInstance != null)
            {
                Destroy(ghostTileInstance);
                ghostTileInstance = null;
            }
            SetGhostVisibility(false);
        }

        public void TryPlaceSelectedTile()
        {
            if (selectedTilePrefab == null) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 pointerPosition = GetCurrentPointerPosition();
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
            {
                Vector3Int cellPos = hexGrid.WorldToCell(hit.point);

                if (!CanPlaceAt(cellPos)) return;

                Vector3 cellPosWorld = hexGrid.GetCellCenterWorld(cellPos);
                Quaternion spawnRotation = Quaternion.Euler(0f, currentYRotation, 0f);
                GameObject newTile = Instantiate(selectedTilePrefab, cellPosWorld, spawnRotation, hexGrid.transform);
                placedTiles[cellPos] = newTile;

                CancelPlacement();
            }
        }

        private Vector2 GetCurrentPointerPosition()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
            return new Vector2(Screen.width / 2f, Screen.height / 2f);
        }

        public void RotateLeft()
        {
            currentYRotation = (currentYRotation - 60f + 360f) % 360f;
        }

        public void RotateRight()
        {
            currentYRotation = (currentYRotation + 60f) % 360f;
        }

        public void ClearAndRegisterTiles()
        {
            placedTiles.Clear();
            RegisterPreplacedTiles();
        }

        public void RegisterPreplacedTiles()
        {
            if (hexGrid == null) return;

            foreach (Transform child in hexGrid.transform)
            {
                if (child.gameObject == ghostTileInstance || child.name.Contains("Ghost")) continue;

                Vector3Int cellPos = hexGrid.WorldToCell(child.position);
                if (!placedTiles.ContainsKey(cellPos))
                {
                    placedTiles.Add(cellPos, child.gameObject);
                }
            }
        }

        public bool CanPlaceAt(Vector3Int cellPos)
        {
            if (IsCellOccupied(cellPos)) return false;
            if (!HasAnyTilesOnMap()) return true;
            return HasAdjacentTile(cellPos);
        }

        private bool IsCellOccupied(Vector3Int cellPos)
        {
            if (placedTiles.TryGetValue(cellPos, out GameObject existingTile))
            {
                if (existingTile != null) return true;
                placedTiles.Remove(cellPos);
            }

            Vector3 worldCenter = hexGrid.GetCellCenterWorld(cellPos);
            Collider[] colliders = Physics.OverlapSphere(worldCenter, 0.2f, tileLayerMask);

            foreach (var col in colliders)
            {
                if (col != null && col.enabled && !col.name.Contains("Ghost"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyTilesOnMap()
        {
            List<Vector3Int> staleKeys = new List<Vector3Int>();
            foreach (var kvp in placedTiles)
            {
                if (kvp.Value == null) staleKeys.Add(kvp.Key);
            }
            foreach (var key in staleKeys) placedTiles.Remove(key);

            if (placedTiles.Count > 0) return true;

            foreach (Transform child in hexGrid.transform)
            {
                if (child.gameObject != ghostTileInstance && !child.name.Contains("Ghost")) return true;
            }

            return false;
        }

        private bool HasAdjacentTile(Vector3Int cellPos)
        {
            foreach (var neighbor in GetHexNeighbors(cellPos))
            {
                if (IsCellOccupied(neighbor)) return true;
            }
            return false;
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

        private void RebuildGhostInstance()
        {
            if (ghostTileInstance != null) Destroy(ghostTileInstance);
            if (selectedTilePrefab == null) return;

            ghostTileInstance = Instantiate(selectedTilePrefab);
            ghostTileInstance.name = "HexGhostPreview";

            foreach (var col in ghostTileInstance.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            if (ghostMaterial != null)
            {
                foreach (var rend in ghostTileInstance.GetComponentsInChildren<Renderer>())
                {
                    rend.material = ghostMaterial;
                }
            }

            SetGhostVisibility(false);
        }

        private void UpdateGhostTransform(Vector3 position)
        {
            if (ghostTileInstance == null) return;

            ghostTileInstance.transform.position = position;
            ghostTileInstance.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
            SetGhostVisibility(true);
        }

        private void SetGhostVisibility(bool visible)
        {
            if (ghostTileInstance == null) return;

            if (isGhostActive != visible)
            {
                ghostTileInstance.SetActive(visible);
                isGhostActive = visible;
            }
        }
    }
}