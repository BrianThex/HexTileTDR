using UnityEngine;

namespace LP.HexTileTDR.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float shiftMultiplier = 2f;
        [SerializeField] private float smoothTime = 0.1f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoomY = 5f;
        [SerializeField] private float maxZoomY = 40f;

        [Header("Map Boundaries")]
        [SerializeField] private bool useBoundaries = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);

        private Vector3 targetPosition;
        private Vector3 moveVelocity;

        private void Start()
        {
            targetPosition = transform.position;
        }

        private void Update()
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref moveVelocity, smoothTime);
        }

        public void ProcessMovement(Vector2 inputDirection, bool isSprinting)
        {
            if (inputDirection == Vector2.zero) return;

            inputDirection.Normalize();
            float currentSpeed = moveSpeed * (isSprinting ? shiftMultiplier : 1f);

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredMove = (forward * inputDirection.y + right * inputDirection.x) * currentSpeed * Time.deltaTime;
            targetPosition += desiredMove;

            if (useBoundaries)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
            }
        }

        public void ProcessZoom(float scrollDelta)
        {
            float zoomAmount = -Mathf.Sign(scrollDelta) * zoomSpeed;
            targetPosition.y = Mathf.Clamp(targetPosition.y + zoomAmount, minZoomY, maxZoomY);
        }
    }
}