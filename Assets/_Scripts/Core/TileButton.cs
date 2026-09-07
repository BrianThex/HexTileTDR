using UnityEngine;
using UnityEngine.UI;

namespace LP.HexTileTDR.Core
{
    [RequireComponent(typeof(Button))]
    public class TileButton : MonoBehaviour
    {
        [SerializeField] private GameObject tilePrefabToPlace;
        [SerializeField] private HexPlacementManager placementManager;

        private void Awake()
        {
            if (placementManager == null)
            {
                placementManager = FindAnyObjectByType<HexPlacementManager>();
            }

            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (placementManager != null && tilePrefabToPlace != null)
            {
                placementManager.SelectTilePrefab(tilePrefabToPlace);
            }
        }
    }
}