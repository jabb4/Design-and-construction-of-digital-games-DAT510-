using System;
using UnityEngine;

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
    [SerializeField] private float cameraSmoothSpeed = 20f;
    [SerializeField] private float minVerticalAngle = -35f;
    [SerializeField] private float maxVerticalAngle = 50f;
    [SerializeField] private float fieldOfView = 60f;

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
    [SerializeField] private bool retargetOnInvalid = true;
    [SerializeField] private bool enableLockOnDebugLogs;

    private Camera cam;
    private CameraOrbitRig cameraOrbitRig;
    private LockOnInputRouter lockOnInputRouter;
    private LockOnTargetService lockOnTargetService;
    private LockedTargetBinding lockedTargetBinding;
    private LockOnSession lockOnSession;
    private LockOnIndicatorPresenter lockOnIndicatorPresenter;

    public bool IsLockedOn => lockOnSession != null && lockOnSession.IsLockedOn;

    public Transform GetLockedTarget() => lockOnSession != null ? lockOnSession.LockedTarget : null;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fieldOfView;

        cameraOrbitRig = new CameraOrbitRig(transform, offset, rotationSpeed, cameraSmoothSpeed, minVerticalAngle, maxVerticalAngle);
        lockOnInputRouter = new LockOnInputRouter(targetSwitchCooldown);
        lockOnInputRouter.ToggleLockRequested += HandleToggleLockRequested;

        lockOnIndicatorPresenter = new LockOnIndicatorPresenter(cam, targetPointHeightOffset, enableLockOnDebugLogs);

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
        Vector2 lookInput = lockOnInputRouter.ReadLookInput();

        if (lockOnSession != null)
        {
            lockOnSession.Validate(Time.deltaTime);

            if (lockOnSession.IsLockedOn)
            {
                if (lockOnInputRouter.TryConsumeSwitchInput(lookInput, targetSwitchSensitivity, out Vector2 switchDirection))
                {
                    lockOnSession.TrySwitchTarget(switchDirection);
                }

                cameraOrbitRig.UpdateLockOn(playerTransform, lockOnSession.LockedTarget, Time.deltaTime);
                lockOnIndicatorPresenter?.Update(lockOnSession.LockedTarget, true);
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
}