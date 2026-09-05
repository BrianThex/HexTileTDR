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

        [Header("Tile Palette")]
        [SerializeField] private List<GameObject> availableTilePrefabs = new List<GameObject>();
        private int currentTileIndex = 0;

        [Header("Ghost Preview Settings")]
        [SerializeField] private bool enableGhostPreview = true;
        [SerializeField] private Material ghostMaterial;

        // Internal State
        private readonly Dictionary<Vector3Int, GameObject> placedTiles = new Dictionary<Vector3Int, GameObject>();
        private GameObject ghostTileInstance;
        private Renderer[] ghostRenderers;
        private bool isGhostActive;
        private float currentYRotation = 0f;

        private void Start()
        {
            RebuildGhostInstance();
            RegisterPreplacedTiles();
        }

        private void Update()
        {
            // Right-click to cycle through available tile prefabs
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                CycleNextTile();
            }

            // Scroll wheel to rotate tile in 60-degree increments
            HandleRotationInput();

            // Block placement and hide preview if pointer is over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetGhostVisibility(false);
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
            {
                Vector3Int cellPos = hexGrid.WorldToCell(hit.point);
                Vector3 cellPosWorld = hexGrid.GetCellCenterWorld(cellPos);

                bool isValidPlacement = CanPlaceAt(cellPos);

                // Manage Ghost Preview
                if (enableGhostPreview && isValidPlacement)
                {
                    UpdateGhostTransform(cellPosWorld);
                }
                else
                {
                    SetGhostVisibility(false);
                }

                // Place Selected Hex Tile
                if (Mouse.current.leftButton.wasPressedThisFrame && isValidPlacement)
                {
                    GameObject currentPrefab = GetSelectedTilePrefab();
                    if (currentPrefab != null)
                    {
                        Quaternion spawnRotation = Quaternion.Euler(0f, currentYRotation, 0f);
                        GameObject newTile = Instantiate(currentPrefab, cellPosWorld, spawnRotation, hexGrid.transform);
                        placedTiles[cellPos] = newTile;

                        SetGhostVisibility(false);
                    }
                }
            }
            else
            {
                SetGhostVisibility(false);
            }
        }

        /// <summary>
        /// Clears dictionary and re-registers all current tile objects under hexGrid.
        /// </summary>
        public void ClearAndRegisterTiles()
        {
            placedTiles.Clear();
            RegisterPreplacedTiles();
        }

        /// <summary>
        /// Registers existing generated map tiles under the hexGrid transform into the tracking dictionary.
        /// </summary>
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
            // Cannot place over an existing tile
            if (IsCellOccupied(cellPos))
            {
                return false;
            }

            // If map is completely empty, allow placement anywhere
            if (!HasAnyTilesOnMap())
            {
                return true;
            }

            // Must be adjacent to at least one existing tile
            return HasAdjacentTile(cellPos);
        }

        private bool IsCellOccupied(Vector3Int cellPos)
        {
            // Dictionary check (prunes null destroyed references if any exist)
            if (placedTiles.TryGetValue(cellPos, out GameObject existingTile))
            {
                if (existingTile != null) return true;
                placedTiles.Remove(cellPos); // Clean stale key
            }

            // Physics overlap check
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
            // Clean dictionary of nulls
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
            Vector3Int[] neighbors = GetHexNeighbors(cellPos);
            foreach (var neighbor in neighbors)
            {
                if (IsCellOccupied(neighbor))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleRotationInput()
        {
            if (Keyboard.current == null) return;

            // Rotate left 60 degrees on "Q"
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                currentYRotation = (currentYRotation - 60f + 360f) % 360f;
            }

            // Rotate right 60 degrees on "E"
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentYRotation = (currentYRotation + 60f) % 360f;
            }
        }

        public void CycleNextTile()
        {
            if (availableTilePrefabs == null || availableTilePrefabs.Count <= 1) return;

            currentTileIndex = (currentTileIndex + 1) % availableTilePrefabs.Count;
            RebuildGhostInstance();
        }

        public GameObject GetSelectedTilePrefab()
        {
            if (availableTilePrefabs == null || availableTilePrefabs.Count == 0) return null;
            return availableTilePrefabs[currentTileIndex];
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
            if (ghostTileInstance != null)
            {
                Destroy(ghostTileInstance);
            }

            GameObject currentPrefab = GetSelectedTilePrefab();
            if (currentPrefab == null) return;

            ghostTileInstance = Instantiate(currentPrefab);
            ghostTileInstance.name = "HexGhostPreview";

            foreach (var col in ghostTileInstance.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            ghostRenderers = ghostTileInstance.GetComponentsInChildren<Renderer>();
            if (ghostMaterial != null)
            {
                foreach (var rend in ghostRenderers)
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

        public void ToggleGhostPreview(bool enable)
        {
            enableGhostPreview = enable;
            if (!enable)
            {
                SetGhostVisibility(false);
            }
        }
    }
}