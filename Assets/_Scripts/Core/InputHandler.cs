using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LP.HexTileTDR.Core
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private HexPlacementManager placementManager;

        [Header("Input Asset")]
        [SerializeField] private InputActionAsset inputActionAsset;

        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction zoomAction;
        private InputAction rotateLeftAction;
        private InputAction rotateRightAction;
        private InputAction placeTileAction;
        private InputAction cancelPlacementAction;

        private void Awake()
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("InputHandler: 'Input Action Asset' reference is missing in the Inspector!");
                return;
            }

            moveAction = inputActionAsset.FindAction("Player/Move");
            sprintAction = inputActionAsset.FindAction("Player/Sprint");
            zoomAction = inputActionAsset.FindAction("Player/Zoom");
            rotateLeftAction = inputActionAsset.FindAction("Player/RotateLeft");
            rotateRightAction = inputActionAsset.FindAction("Player/RotateRight");
            placeTileAction = inputActionAsset.FindAction("Player/PlaceTile");
            cancelPlacementAction = inputActionAsset.FindAction("Player/CancelPlacement");
        }

        private void OnEnable()
        {
            if (inputActionAsset != null)
            {
                inputActionAsset.Enable();
            }
        }

        private void OnDisable()
        {
            if (inputActionAsset != null)
            {
                inputActionAsset.Disable();
            }
        }

        private void Update()
        {
            HandleCameraMovement();
            HandleCameraZoom();
            HandlePlacementInputs();
        }

        private void HandleCameraMovement()
        {
            if (cameraController == null || moveAction == null) return;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool isSprinting = sprintAction != null && sprintAction.IsPressed();

            cameraController.ProcessMovement(moveInput, isSprinting);
        }

        private void HandleCameraZoom()
        {
            if (cameraController == null || zoomAction == null) return;

            // Read input as Vector2 instead of float to match Mouse Scroll DeltaControl
            Vector2 zoomInput = zoomAction.ReadValue<Vector2>();

            if (Mathf.Abs(zoomInput.y) > 0.01f)
            {
                cameraController.ProcessZoom(zoomInput.y);
            }
        }

        private void HandlePlacementInputs()
        {
            if (placementManager == null) return;

            if (rotateLeftAction != null && rotateLeftAction.WasPressedThisFrame())
            {
                placementManager.RotateLeft();
            }

            if (rotateRightAction != null && rotateRightAction.WasPressedThisFrame())
            {
                placementManager.RotateRight();
            }

            if (placeTileAction != null && placeTileAction.WasPressedThisFrame())
            {
                // Do not trigger tile placement if the click originated on a UI element
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                placementManager.TryPlaceSelectedTile();
            }

            if (cancelPlacementAction != null && cancelPlacementAction.WasPressedThisFrame())
            {
                placementManager.CancelPlacement();
            }
        }
    }
}