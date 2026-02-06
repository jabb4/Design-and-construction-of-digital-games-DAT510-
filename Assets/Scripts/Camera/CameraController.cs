using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    
    /// <summary>
    /// Event fired when camera locks onto a target.
    /// Used by PlayerStateMachine for auto-equip weapon.
    /// </summary>
    public event Action OnLockOnStarted;
    
    /// <summary>
    /// Event fired when camera lock-on is released.
    /// Used by PlayerStateMachine for delayed auto-unequip weapon.
    /// </summary>
    public event Action OnLockOnEnded;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 3.5f, -6f);
    [Range(0.1f, 10.0f)]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float cameraSmoothSpeed = 20f;
    [SerializeField] private float minVerticalAngle = -35;
    [SerializeField] private float maxVerticalAngle = 50f;
    [SerializeField] private float fieldOfView = 60f;

    [Header("Target Switching")]
    [Range(5f, 15f)]
    [SerializeField] private float targetSwitchSensitivity = 10f;

    [Header("Lock-On Settings")]
    [SerializeField] private float lockOnRange = 30f;
    [SerializeField] private LayerMask enemyLayer;

    private Camera cam;
    private bool isLockedOn;
    private Transform lockedTarget;
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private InputSystem_Actions playerControls;
    private bool canSwitchTargets = true;
    private Canvas uiCanvas;
    private GameObject lockOnIndicator;
    private Image indicatorImage;

    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    private bool isTransitioningOutOfLock = false;

    private System.Collections.IEnumerator SwitchCooldown()
    {
        canSwitchTargets = false;
        yield return new WaitForSeconds(0.2f); // 200ms cooldown
        canSwitchTargets = true;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fieldOfView;

        // Create UI Canvas for lock-on indicator
        GameObject canvasGO = new GameObject("LockOnCanvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        uiCanvas.sortingOrder = 100; // Render on top

        // Create lock-on indicator UI element
        lockOnIndicator = new GameObject("LockOnIndicator");
        lockOnIndicator.transform.SetParent(uiCanvas.transform);
        indicatorImage = lockOnIndicator.AddComponent<Image>();
        indicatorImage.color = Color.white;
        indicatorImage.rectTransform.sizeDelta = new Vector2(10, 10);

        // Load the reticle texture and create sprite
        Texture2D reticleTexture = Resources.Load<Texture2D>("LockOnIndicator");
        indicatorImage.sprite = Sprite.Create(reticleTexture, new Rect(0, 0, reticleTexture.width, reticleTexture.height), new Vector2(0.5f, 0.5f));

        lockOnIndicator.SetActive(false);

        playerControls = new InputSystem_Actions();
        playerControls.Player.LockOn.performed += OnLockOnToggle;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned to CameraController!");
        }
    }

    private void LateUpdate()
    {
        Vector2 currentLookInput = playerControls.Player.Look.ReadValue<Vector2>();

        if (isLockedOn && lockedTarget != null)
        {
            HandleLockOnCamera();
            UpdateLockOnIndicator();
            // Handle target switching with mouse input
            if (currentLookInput.magnitude > targetSwitchSensitivity && canSwitchTargets)
            {
                SwitchTarget(currentLookInput);
                StartCoroutine(SwitchCooldown());
            }
        }
        else if (isTransitioningOutOfLock)
        {
            HandleTransitionOutOfLock();
        }
        else
        {
            HandleFreeCamera();
        }
    }

    private void HandleFreeCamera()
    {
        Vector2 currentLookInput = playerControls.Player.Look.ReadValue<Vector2>();

        currentHorizontalAngle += currentLookInput.x * rotationSpeed;
        currentVerticalAngle -= currentLookInput.y * rotationSpeed;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);

        Vector3 desiredPosition = playerTransform.position + rotation * offset;
        Quaternion desiredRotation = Quaternion.LookRotation(playerTransform.position + Vector3.up * 1.5f - desiredPosition);

        // Smooth camera movement and rotation
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * cameraSmoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * cameraSmoothSpeed);
    }

    private void HandleLockOnCamera()
    {
        // Calculate target position and rotation
        Vector3 directionToTarget = (lockedTarget.position - playerTransform.position).normalized;
        Vector3 desiredCameraPosition = playerTransform.position - directionToTarget * offset.magnitude + Vector3.up * offset.y;
        Quaternion desiredCameraRotation = Quaternion.LookRotation(lockedTarget.position + Vector3.up - desiredCameraPosition);

        transform.position = Vector3.Lerp(transform.position, desiredCameraPosition, Time.deltaTime * cameraSmoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredCameraRotation, Time.deltaTime * cameraSmoothSpeed);
    }


    private void OnLockOnToggle(InputAction.CallbackContext context)
    {
        if (isLockedOn)
        {
            UnlockTarget();
        }
        else
        {
            FindAndLockTarget();
        }
    }

    private void FindAndLockTarget()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(playerTransform.position, lockOnRange, enemyLayer);

        if (enemiesInRange.Length == 0) return;

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        // Get player's viewport position to determine sections top and bottom section
        Vector3 playerViewportPos = cam.WorldToViewportPoint(playerTransform.GetComponent<Collider>().bounds.center);

        foreach (Collider enemy in enemiesInRange)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(enemy.transform.position);
            if (viewportPos.z <= 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1)
                continue; // Not in view

            // Check top section first, then bottom (prioritize top)
            bool isInTopSection = viewportPos.y > playerViewportPos.y;
            float sectionPriority = isInTopSection ? 0 : 1;

            // Distance from center
            float centerDistance = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));

            // Distance from player
            float playerDistance = Vector3.Distance(playerTransform.position, enemy.transform.position);

            // Score: prioritize section (top first), then player distance, then center proximity
            float score = sectionPriority * 1000 + playerDistance * 100 + centerDistance;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            LockOnTarget(bestTarget);
        }
    }

    private void LockOnTarget(Transform target)
    {
        lockedTarget = target;
        isLockedOn = true;
        AddLockOnIndicator(target);
        UpdateTargetCameraTransform();
        OnLockOnStarted?.Invoke();
    }

    private void UpdateTargetCameraTransform()
    {
        if (lockedTarget != null)
        {
            Vector3 directionToTarget = (lockedTarget.position - playerTransform.position).normalized;
            targetCameraPosition = playerTransform.position - directionToTarget * offset.magnitude + Vector3.up * offset.y;
            targetCameraRotation = Quaternion.LookRotation(lockedTarget.position + Vector3.up * 1.5f - targetCameraPosition);
        }
    }

    private void UnlockTarget()
    {
        if (lockedTarget != null)
        {
            RemoveLockOnIndicator(lockedTarget);
        }
        isLockedOn = false;
        lockedTarget = null;

        UpdateFreeCameraAnglesFromCurrent();
        OnLockOnEnded?.Invoke();
    }

    private void UpdateFreeCameraAnglesFromCurrent()
    {
        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0;
        if (cameraForward != Vector3.zero)
        {
            currentHorizontalAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
        }

        Vector3 cameraForwardNormalized = transform.forward.normalized;
        currentVerticalAngle = -Mathf.Asin(cameraForwardNormalized.y) * Mathf.Rad2Deg;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
    }

    private void UpdateFreeCameraTarget()
    {
        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        targetCameraPosition = playerTransform.position + rotation * offset;
        targetCameraRotation = Quaternion.LookRotation(playerTransform.position + Vector3.up * 1.5f - targetCameraPosition);
    }

    private void HandleTransitionOutOfLock()
    {
        // Allow mouse input during transition for gradual control takeover
        Vector2 currentLookInput = playerControls.Player.Look.ReadValue<Vector2>();
        currentHorizontalAngle += currentLookInput.x * rotationSpeed;
        currentVerticalAngle -= currentLookInput.y * rotationSpeed;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        UpdateFreeCameraTarget();

        transform.position = Vector3.Lerp(transform.position, targetCameraPosition, Time.deltaTime * cameraSmoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetCameraRotation, Time.deltaTime * cameraSmoothSpeed);

        if (Vector3.Distance(transform.position, targetCameraPosition) < 0.5f &&
            Quaternion.Angle(transform.rotation, targetCameraRotation) < 5f)
        {
            isTransitioningOutOfLock = false;
        }
    }

    public bool IsLockedOn => isLockedOn;

    public Transform GetLockedTarget() => lockedTarget;

    private void UpdateLockOnIndicator()
    {
        Vector3 worldPos = lockedTarget.GetComponent<Collider>().bounds.center;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.GetComponent<RectTransform>(), screenPos, null, out localPos); // Use null for screen space overlay
        indicatorImage.rectTransform.localPosition = localPos;
        lockOnIndicator.SetActive(isLockedOn && lockedTarget != null); // Always show when locked, regardless of position
    }
    public void SwitchTarget(Vector2 inputDirection)
    {
        if (!isLockedOn || lockedTarget == null || inputDirection.magnitude < 0.1f) return;

        Collider[] enemiesInRange = Physics.OverlapSphere(playerTransform.position, lockOnRange, enemyLayer);
        Transform bestTarget = null;
        float bestAngle = float.MaxValue;

        // Get current target viewport position
        Vector3 currentTargetViewport = cam.WorldToViewportPoint(lockedTarget.position);

        // Normalize input direction (mouse delta)
        Vector2 normalizedInput = inputDirection.normalized;

        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.transform == lockedTarget) continue;

            Vector3 enemyViewport = cam.WorldToViewportPoint(enemy.transform.position);
            if (enemyViewport.z <= 0 || enemyViewport.x < 0 || enemyViewport.x > 1 || enemyViewport.y < 0 || enemyViewport.y > 1)
                continue;

            // Vector from current target to enemy in view space
            Vector2 viewSpaceDirection = new Vector2(enemyViewport.x - currentTargetViewport.x, enemyViewport.y - currentTargetViewport.y);

            // Calculate angle between input direction and view space direction
            float angle = Vector2.Angle(normalizedInput, viewSpaceDirection.normalized);

            // Consider distance as secondary factor (closer targets preferred)
            float distance = viewSpaceDirection.magnitude;
            angle += distance * 0.1f; // Small penalty for distance

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            RemoveLockOnIndicator(lockedTarget);
            lockedTarget = bestTarget;
            AddLockOnIndicator(bestTarget);
            UpdateTargetCameraTransform();
        }
    }

    private void AddLockOnIndicator(Transform target)
    {
        lockOnIndicator.SetActive(true);
    }

    private void RemoveLockOnIndicator(Transform target)
    {
        lockOnIndicator.SetActive(false);
    }
}