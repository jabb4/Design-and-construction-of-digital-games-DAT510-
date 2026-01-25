using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField, Range(0f, 50f)] private float acceleration = 20f;
    [SerializeField, Range(0f, 1f)] private float airControlFactor = 0.5f;
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckOffset = 0.15f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isSprinting;
    private bool isGrounded;
    private Vector3 velocitySmooth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void Update()
    {
        CheckGrounded();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if (cameraTransform == null || rb == null) return;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 desiredHorizontal = moveDirection.normalized * currentSpeed;

        if (!isGrounded)
        {
            desiredHorizontal *= airControlFactor;
        }

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float t = Mathf.Clamp01(acceleration * Time.fixedDeltaTime);
        Vector3 smoothed = Vector3.Lerp(currentHorizontal, desiredHorizontal, t);

        Vector3 finalVelocity = new Vector3(smoothed.x, rb.linearVelocity.y, smoothed.z);
        rb.linearVelocity = finalVelocity;
    }

    private void HandleRotation()
    {
        if (!isGrounded || moveInput.magnitude < 0.1f || cameraTransform == null) return;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 cameraMoveDir = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        if (cameraMoveDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(cameraMoveDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }
}