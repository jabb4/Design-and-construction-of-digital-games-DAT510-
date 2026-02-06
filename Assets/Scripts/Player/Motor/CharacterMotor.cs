namespace Player.StateMachine
{
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Handles physics-based movement, rotation, and ground detection.
    /// Extracted from PlayerMovement.cs for use with the state machine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [FormerlySerializedAs("rotationSpeed")]
        [SerializeField] private float freeCameraRotationSpeed = 4f;
        [SerializeField] private float lockOnRotationSpeed = 540f;
        [SerializeField, Range(0f, 50f)] private float acceleration = 12f;
        [SerializeField, Range(0f, 50f)] private float deceleration = 4f;
        [SerializeField, Range(0f, 50f)] private float lateralDamping = 12f;
        [SerializeField] private float airControlFactor = 0.3f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float extraGravityMultiplier = 2f;

        [Header("Landing Settings")]
        [SerializeField, Range(0f, 2f)] private float landingMoveBlendTime = 1.5f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 1f;
        [SerializeField] private float groundCheckOffset = 0.05f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Current velocity of the rigidbody.
        /// </summary>
        public Vector3 Velocity => rb.linearVelocity;

        /// <summary>
        /// Walk speed value.
        /// </summary>
        public float WalkSpeed => walkSpeed;

        /// <summary>
        /// Sprint speed value.
        /// </summary>
        public float SprintSpeed => sprintSpeed;

        /// <summary>
        /// Time to blend movement back in after landing.
        /// </summary>
        public float LandingMoveBlendTime => landingMoveBlendTime;

        /// <summary>
        /// Check if camera is locked on to a target.
        /// </summary>
        public bool IsLockedOn => cameraController != null && cameraController.IsLockedOn;

        #endregion

        #region Private Fields

        private Rigidbody rb;
        private CameraController cameraController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get required components
            rb = GetComponent<Rigidbody>();

            // Configure rigidbody
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Find camera transform if not assigned
            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                }
                else
                {
                    Debug.LogWarning("CharacterMotor: No camera assigned and no main camera found.");
                }
            }

            // Find CameraController
            cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("CharacterMotor: CameraController not found in scene.");
            }
        }

        private void FixedUpdate()
        {
            CheckGrounded();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply movement based on input. Call in FixedUpdate.
        /// </summary>
        /// <param name="moveInput">2D movement input (WASD)</param>
        /// <param name="useSprint">Whether to use sprint speed</param>
        public void Move(Vector2 moveInput, bool useSprint, float speedScale = 1f)
        {
            // Get camera-relative move direction
            Vector3 moveDirection = GetMoveDirection(moveInput);

            Vector3 desiredDirection = moveDirection.sqrMagnitude > 0.0001f
                ? moveDirection.normalized
                : Vector3.zero;

            // Determine current speed
            float clampedScale = Mathf.Clamp01(speedScale);
            float targetSpeed = moveInput.sqrMagnitude > 0.0001f
                ? (useSprint ? sprintSpeed : walkSpeed)
                : 0f;
            targetSpeed *= clampedScale;

            // Calculate desired horizontal velocity
            Vector3 desiredHorizontal = desiredDirection * targetSpeed;

            // Apply air control factor if not grounded
            if (!IsGrounded)
            {
                desiredHorizontal *= airControlFactor;
            }

            // Smoothly interpolate to desired velocity
            Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 smoothed;

            if (desiredDirection != Vector3.zero)
            {
                float currentForwardSpeed = Vector3.Dot(currentHorizontal, desiredDirection);
                float targetForwardSpeed = Vector3.Dot(desiredHorizontal, desiredDirection);

                Vector3 currentForward = desiredDirection * currentForwardSpeed;
                Vector3 currentLateral = currentHorizontal - currentForward;

                float rate = targetForwardSpeed > currentForwardSpeed ? acceleration : deceleration;
                float forwardT = rate <= 0f ? 1f : 1f - Mathf.Exp(-rate * Time.fixedDeltaTime);
                float newForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetForwardSpeed, forwardT);
                Vector3 newForward = desiredDirection * newForwardSpeed;

                float lateralT = lateralDamping <= 0f ? 1f : 1f - Mathf.Exp(-lateralDamping * Time.fixedDeltaTime);
                Vector3 newLateral = Vector3.Lerp(currentLateral, Vector3.zero, lateralT);

                smoothed = newForward + newLateral;
            }
            else
            {
                float currentSpeed = currentHorizontal.magnitude;
                float rate = targetSpeed > currentSpeed ? acceleration : deceleration;
                float t = rate <= 0f ? 1f : 1f - Mathf.Exp(-rate * Time.fixedDeltaTime);
                smoothed = Vector3.Lerp(currentHorizontal, desiredHorizontal, t);
            }

            // Apply final velocity (preserve vertical velocity)
            Vector3 finalVelocity = new Vector3(smoothed.x, rb.linearVelocity.y, smoothed.z);
            rb.linearVelocity = finalVelocity;
        }

        /// <summary>
        /// Apply rotation towards movement direction (free camera mode).
        /// </summary>
        /// <param name="moveInput">2D movement input</param>
        public void RotateTowardsMovement(Vector2 moveInput)
        {
            // Only rotate if there's input
            if (moveInput.sqrMagnitude < 0.01f)
                return;

            // Get camera-relative move direction
            Vector3 moveDirection = GetMoveDirection(moveInput);

            if (moveDirection.sqrMagnitude < 0.01f)
                return;

            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Smoothly rotate towards target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                freeCameraRotationSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>
        /// Apply rotation towards lock-on target.
        /// </summary>
        public void RotateTowardsLockOnTarget()
        {
            Transform lockOnTarget = GetLockOnTarget();
            if (lockOnTarget == null)
                return;

            // Calculate direction to target
            Vector3 directionToTarget = lockOnTarget.position - transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude < 0.01f)
                return;

            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Smoothly rotate towards target (faster than free rotation)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lockOnRotationSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>
        /// Apply jump force.
        /// </summary>
        public void Jump()
        {
            if (!IsGrounded)
                return;

            // Apply upward force
            Vector3 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Apply extra gravity when falling.
        /// </summary>
        public void ApplyFallGravity()
        {
            // Only apply extra gravity when falling
            if (rb.linearVelocity.y < 0f && !IsGrounded)
            {
                rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// Get camera-relative move direction from input.
        /// </summary>
        /// <param name="moveInput">2D movement input (x = horizontal, y = forward/back)</param>
        /// <returns>World-space movement direction</returns>
        public Vector3 GetMoveDirection(Vector2 moveInput)
        {
            if (cameraTransform == null)
                return Vector3.zero;

            // Get camera forward and right vectors (flattened)
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            // Calculate movement direction relative to camera
            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x);

            return moveDirection;
        }

        /// <summary>
        /// Get the current lock-on target, if any.
        /// </summary>
        /// <returns>Lock-on target transform, or null if not locked on</returns>
        public Transform GetLockOnTarget()
        {
            if (cameraController == null || !cameraController.IsLockedOn)
                return null;

            return cameraController.GetLockedTarget();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check if the character is grounded using a sphere cast.
        /// </summary>
        private void CheckGrounded()
        {
            Vector3 spherePosition = transform.position + Vector3.up * groundCheckOffset;
            IsGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            // Visualize ground check sphere
            Vector3 spherePosition = transform.position + Vector3.up * groundCheckOffset;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
        }

        #endregion
    }
}
