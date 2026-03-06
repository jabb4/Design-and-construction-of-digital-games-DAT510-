using System;
using UnityEngine;
using UnityEngine.Serialization;

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

    /// <summary>
    /// Event fired when lock-on target changes while lock remains active.
    /// Invoked for both manual and automatic re-targeting.
    /// </summary>
    public event Action<Transform, Transform> OnLockTargetChanged;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3.5f, -6f);
    [Range(0.1f, 10f)]
    [SerializeField] private float rotationSpeed = 1f;

    [Header("Camera Smoothing")]
    [SerializeField, Min(0f)] private float freeCameraSmoothing = 14f;
    [FormerlySerializedAs("cameraSmoothSpeed")]
    [SerializeField, Min(0f)] private float lockOnCameraSmoothing = 20f;

    [SerializeField] private float minVerticalAngle = -35f;
    [SerializeField] private float maxVerticalAngle = 50f;
    [SerializeField] private float fieldOfView = 60f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
    [SerializeField, Min(0f)] private float collisionSafetyOffset = 0.1f;
    [SerializeField, Min(0f)] private float collisionMinDistance = 1.2f;
    [SerializeField, Min(0f)] private float collisionInSpeed = 16f;
    [SerializeField, Min(0f)] private float collisionOutSpeed = 8f;

    [Header("Target Switching")]
    [Range(5f, 15f)]
    [SerializeField] private float targetSwitchSensitivity = 10f;
    [SerializeField, Min(0f)] private float targetSwitchCooldown = 0.2f;

    [Header("Lock-On Settings")]
    [SerializeField] private float lockOnRange = 30f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask lineOfSightBlockerLayer = ~0;
    [SerializeField, Min(0f)] private float lockBreakDistance = 35f;
    [SerializeField, Min(0f)] private float lineOfSightGraceSeconds = 0.5f;
    [SerializeField, Min(0f)] private float targetPointHeightOffset = 1.2f;
    [SerializeField] private string targetPointTransformName = "spine_03";
    [SerializeField] private bool retargetOnInvalid = true;
    [SerializeField] private bool enableLockOnDebugLogs;

    [Header("Lock-On Indicator")]
    [SerializeField, Min(0f)] private float indicatorPositionSmoothing = 18f;
    [SerializeField, Min(0f)] private float indicatorBloomIntensity = 3f;
    [SerializeField, Min(1f)] private float indicatorBloomScale = 2.25f;
    [SerializeField, Min(0f)] private float indicatorBloomPulseAmplitude = 0.1f;
    [SerializeField, Min(0f)] private float indicatorBloomPulseSpeed = 8f;

    private Camera cam;
    private CameraOrbitRig cameraOrbitRig;
    private LockOnInputRouter lockOnInputRouter;
    private LockOnTargetService lockOnTargetService;
    private LockedTargetBinding lockedTargetBinding;
    private LockOnSession lockOnSession;
    private LockOnIndicatorPresenter lockOnIndicatorPresenter;

    public bool IsLockedOn => lockOnSession != null && lockOnSession.IsLockedOn;

    /// <summary>
    /// When true, mouse/look input is ignored and the camera only follows
    /// the player's position. Use this during the pause menu.
    /// </summary>
    public bool IsRotationBlocked { get; set; }

    public Transform GetLockedTarget() => lockOnSession != null ? lockOnSession.LockedTarget : null;

    /// <summary>
    /// Switches the camera follow target (e.g. to a ragdoll bone on player death).
    /// Releases any active lock-on.
    /// </summary>
    public void SetFollowTarget(Transform newTarget, float lookHeightOffset = 0f)
    {
        if (lockOnSession != null && lockOnSession.IsLockedOn)
        {
            lockOnSession.UnlockManual();
        }

        playerTransform = newTarget;
        cameraOrbitRig?.SetLookHeight(lookHeightOffset);
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(targetPointTransformName))
        {
            targetPointTransformName = "spine_03";
        }

        cam = GetComponent<Camera>();
        cam.fieldOfView = fieldOfView;

        cameraCollisionMask = ResolveDefaultCameraCollisionMask(cameraCollisionMask);
        cameraOrbitRig = new CameraOrbitRig(
            transform,
            offset,
            rotationSpeed,
            freeCameraSmoothing,
            lockOnCameraSmoothing,
            minVerticalAngle,
            maxVerticalAngle,
            new CameraCollisionSolver(),
            cameraCollisionMask,
            collisionRadius,
            collisionSafetyOffset,
            collisionMinDistance,
            collisionInSpeed,
            collisionOutSpeed);
        lockOnInputRouter = new LockOnInputRouter(targetSwitchCooldown);
        lockOnInputRouter.ToggleLockRequested += HandleToggleLockRequested;

        lockOnIndicatorPresenter = new LockOnIndicatorPresenter(
            cam,
            indicatorPositionSmoothing,
            indicatorBloomIntensity,
            indicatorBloomScale,
            indicatorBloomPulseAmplitude,
            indicatorBloomPulseSpeed,
            enableLockOnDebugLogs);

        InitializeLockOnGraph();
    }

    private void OnEnable()
    {
        lockOnInputRouter?.Enable();
    }

    private void OnDisable()
    {
        lockOnInputRouter?.Disable();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned to CameraController!");
            return;
        }

        InitializeLockOnGraph();
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            return;
        }

        InitializeLockOnGraph();

        lockOnInputRouter.Tick(Time.deltaTime);
        Vector2 lookInput = IsRotationBlocked ? Vector2.zero : lockOnInputRouter.ReadLookInput();

        if (lockOnSession != null)
        {
            lockOnSession.Validate(Time.deltaTime);

            if (lockOnSession.IsLockedOn)
            {
                if (lockOnInputRouter.TryConsumeSwitchInput(lookInput, targetSwitchSensitivity, out Vector2 switchDirection))
                {
                    lockOnSession.TrySwitchTarget(switchDirection);
                }

                Transform lockedTarget = lockOnSession.LockedTarget;
                if (lockedTarget == null)
                {
                    lockOnIndicatorPresenter?.Hide();
                    return;
                }

                Vector3 lockOnTargetPoint = TargetPointResolver.ResolveTargetPoint(
                    lockedTarget,
                    targetPointHeightOffset,
                    targetPointTransformName);

                cameraOrbitRig.UpdateLockOn(playerTransform, lockOnTargetPoint, Time.deltaTime);
                lockOnIndicatorPresenter?.Update(lockOnTargetPoint, true, Time.deltaTime);
                return;
            }
        }

        cameraOrbitRig.UpdateFree(playerTransform, lookInput, Time.deltaTime);
        lockOnIndicatorPresenter?.Hide();
    }

    private void OnDestroy()
    {
        if (lockOnSession != null)
        {
            lockOnSession.OnLockOnStarted -= HandleSessionLockOnStarted;
            lockOnSession.OnLockOnEnded -= HandleSessionLockOnEnded;
            lockOnSession.OnLockTargetChanged -= HandleSessionLockTargetChanged;
            lockOnSession.Dispose();
            lockOnSession = null;
        }

        if (lockedTargetBinding != null)
        {
            lockedTargetBinding.Dispose();
            lockedTargetBinding = null;
        }

        if (lockOnInputRouter != null)
        {
            lockOnInputRouter.ToggleLockRequested -= HandleToggleLockRequested;
            lockOnInputRouter.Dispose();
            lockOnInputRouter = null;
        }

        lockOnIndicatorPresenter?.Dispose();
        lockOnIndicatorPresenter = null;
    }

    private void InitializeLockOnGraph()
    {
        if (lockOnSession != null || playerTransform == null)
        {
            return;
        }

        lockOnTargetService = new LockOnTargetService(
            playerTransform,
            cam,
            enemyLayer,
            lineOfSightBlockerLayer,
            lockOnRange,
            lockBreakDistance,
            lineOfSightGraceSeconds,
            targetPointHeightOffset,
            targetPointTransformName,
            enableLockOnDebugLogs);

        lockedTargetBinding = new LockedTargetBinding();

        lockOnSession = new LockOnSession(
            lockOnTargetService,
            lockedTargetBinding,
            retargetOnInvalid,
            enableLockOnDebugLogs);

        lockOnSession.OnLockOnStarted += HandleSessionLockOnStarted;
        lockOnSession.OnLockOnEnded += HandleSessionLockOnEnded;
        lockOnSession.OnLockTargetChanged += HandleSessionLockTargetChanged;
    }

    private void HandleToggleLockRequested()
    {
        if (playerTransform == null)
        {
            return;
        }

        InitializeLockOnGraph();
        if (lockOnSession == null)
        {
            return;
        }

        if (lockOnSession.IsLockedOn)
        {
            lockOnSession.UnlockManual();
        }
        else
        {
            lockOnSession.TryLockBestTarget();
        }
    }

    private void HandleSessionLockOnStarted()
    {
        OnLockOnStarted?.Invoke();
    }

    private void HandleSessionLockOnEnded()
    {
        cameraOrbitRig?.SyncFreeAnglesFromCurrent();
        lockOnIndicatorPresenter?.Hide();
        OnLockOnEnded?.Invoke();
    }

    private void HandleSessionLockTargetChanged(Transform previousTarget, Transform currentTarget)
    {
        OnLockTargetChanged?.Invoke(previousTarget, currentTarget);
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableLockOnDebugLogs || playerTransform == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, lockOnRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, lockBreakDistance);
    }

    private static LayerMask ResolveDefaultCameraCollisionMask(LayerMask configuredMask)
    {
        if (configuredMask.value != ~0)
        {
            return configuredMask;
        }

        int resolvedMask = ~0;
        ExcludeLayerByName(ref resolvedMask, "Enemy");
        ExcludeLayerByName(ref resolvedMask, "Hitbox");
        ExcludeLayerByName(ref resolvedMask, "Hurtbox");
        ExcludeLayerByName(ref resolvedMask, "UI");
        ExcludeLayerByName(ref resolvedMask, "Ignore Raycast");
        ExcludeLayerByName(ref resolvedMask, "Ragdoll");
        return resolvedMask;
    }

    private static void ExcludeLayerByName(ref int mask, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex >= 0)
        {
            mask &= ~(1 << layerIndex);
        }
    }
}
