using UnityEngine;
using UnityEngine.InputSystem;

namespace LP.HexTileTDR.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float shiftMultiplier = 2f;
        [SerializeField] private float smoothTime = 0.1f;

        [Header("Edge Scrolling (Optional)")]
        [SerializeField] private bool enableEdgeScrolling = false;
        [SerializeField] private float edgeBorderThickness = 10f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoomY = 5f;
        [SerializeField] private float maxZoomY = 40f;

        [Header("Map Boundaries")]
        [SerializeField] private bool useBoundaries = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f); // X, Z min
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);   // X, Z max

        private Vector3 targetPosition;
        private Vector3 moveVelocity;

        private void Start()
        {
            targetPosition = transform.position;
        }

        private void Update()
        {
            HandleMovementInput();
            HandleZoomInput();
            ApplyMovement();
        }

        private void HandleMovementInput()
        {
            Vector3 inputDirection = Vector3.zero;

            // WASD Movement
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDirection.z += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDirection.z -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDirection.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDirection.x += 1f;
            }

            // Mouse Edge Scrolling
            if (enableEdgeScrolling && Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();

                if (mousePos.x >= Screen.width - edgeBorderThickness) inputDirection.x += 1f;
                if (mousePos.x <= edgeBorderThickness) inputDirection.x -= 1f;
                if (mousePos.y >= Screen.height - edgeBorderThickness) inputDirection.z += 1f;
                if (mousePos.y <= edgeBorderThickness) inputDirection.z -= 1f;
            }

            inputDirection.Normalize();

            // Sprint Speed modifier
            float currentSpeed = moveSpeed;
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            {
                currentSpeed *= shiftMultiplier;
            }

            // Move relative to camera direction on X/Z plane
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredMove = (forward * inputDirection.z + right * inputDirection.x) * currentSpeed * Time.deltaTime;
            targetPosition += desiredMove;

            // Apply Boundary Constraints
            if (useBoundaries)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
            }
        }

        private void HandleZoomInput()
        {
            if (Mouse.current == null) return;

            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                // Scroll up moves closer (down Y), scroll down zooms out (up Y)
                float zoomAmount = -Mathf.Sign(scrollDelta) * zoomSpeed;
                targetPosition.y = Mathf.Clamp(targetPosition.y + zoomAmount, minZoomY, maxZoomY);
            }
        }

        private void ApplyMovement()
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref moveVelocity, smoothTime);
        }
    }
}