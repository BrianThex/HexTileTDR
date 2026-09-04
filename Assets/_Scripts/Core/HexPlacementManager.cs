using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LP.HexTileTDR.Core
{
    public class HexPlacementManager : MonoBehaviour
    {
        [Header("Grid References")]
        [SerializeField] private Grid hexGrid;
        [SerializeField] private Camera mainCamera;

        [Header("Active Build Selection")]
        [SerializeField] private TileSO selectedTileData;

        // Track occupied grid positions
        private readonly Dictionary<Vector3Int, HexTile> placedTiles = new Dictionary<Vector3Int, HexTile>();

        private void Update()
        {
            // Prevent placing tiles when clicking on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Uses New Input System
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlaceTile();
            }
        }

        public void SetSelectedTile(TileSO tileData)
        {
            selectedTileData = tileData;
        }

        private void TryPlaceTile()
        {
            if (selectedTileData == null) return;

            Vector3Int cellPosition = GetMouseGridPosition();

            // Check if cell is already occupied
            if (placedTiles.ContainsKey(cellPosition))
            {
                Debug.LogWarning($"Cell {cellPosition} is already occupied!");
                return;
            }

            // Convert grid cell coordinates to world center position
            Vector3 worldPos = hexGrid.GetCellCenterWorld(cellPosition);

            // Instantiate tile prefab and record position
            GameObject spawnedObj = Instantiate(selectedTileData.tilePrefab, worldPos, Quaternion.identity, transform);
            HexTile hexTile = spawnedObj.GetComponent<HexTile>() ?? spawnedObj.AddComponent<HexTile>();
            hexTile.Initialize(selectedTileData, cellPosition);

            placedTiles.Add(cellPosition, hexTile);
            Debug.Log($"Placed {selectedTileData.tileName} at grid position {cellPosition}");
        }

        public Vector3Int GetMouseGridPosition()
        {
            if (Mouse.current == null || mainCamera == null) return Vector3Int.zero;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero); // Assumes hex grid lies on XZ plane

            if (gridPlane.Raycast(ray, out float enterDistance))
            {
                Vector3 worldHitPoint = ray.GetPoint(enterDistance);
                return hexGrid.WorldToCell(worldHitPoint);
            }

            return Vector3Int.zero;
        }

        // Helper: Check if target cell has an adjacent placed tile
        public bool HasAdjacentTile(Vector3Int cellPos)
        {
            Vector3Int[] neighbors = GetHexNeighbors(cellPos);
            foreach (var neighbor in neighbors)
            {
                if (placedTiles.ContainsKey(neighbor))
                    return true;
            }
            return false;
        }

        // Returns standard hex offsets for Pointed-Top layout (Odd-R coordinate system)
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