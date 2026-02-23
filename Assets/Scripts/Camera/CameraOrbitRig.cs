using UnityEngine;

public sealed class CameraOrbitRig
{
    private const float PlayerLookHeight = 1.5f;

    private readonly Transform cameraTransform;
    private readonly Vector3 offset;
    private readonly float rotationSpeed;
    private readonly float freeCameraSmoothing;
    private readonly float lockOnCameraSmoothing;
    private readonly float minVerticalAngle;
    private readonly float maxVerticalAngle;
    private readonly CameraCollisionSolver collisionSolver;
    private readonly LayerMask collisionMask;
    private readonly float collisionRadius;
    private readonly float collisionSafetyOffset;
    private readonly float collisionMinDistance;
    private readonly float collisionInSpeed;
    private readonly float collisionOutSpeed;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;

    public CameraOrbitRig(
        Transform cameraTransform,
        Vector3 offset,
        float rotationSpeed,
        float freeCameraSmoothing,
        float lockOnCameraSmoothing,
        float minVerticalAngle,
        float maxVerticalAngle,
        CameraCollisionSolver collisionSolver,
        LayerMask collisionMask,
        float collisionRadius,
        float collisionSafetyOffset,
        float collisionMinDistance,
        float collisionInSpeed,
        float collisionOutSpeed)
    {
        this.cameraTransform = cameraTransform;
        this.offset = offset;
        this.rotationSpeed = rotationSpeed;
        this.freeCameraSmoothing = Mathf.Max(0f, freeCameraSmoothing);
        this.lockOnCameraSmoothing = Mathf.Max(0f, lockOnCameraSmoothing);
        this.minVerticalAngle = minVerticalAngle;
        this.maxVerticalAngle = maxVerticalAngle;
        this.collisionSolver = collisionSolver ?? new CameraCollisionSolver();
        this.collisionMask = collisionMask;
        this.collisionRadius = collisionRadius;
        this.collisionSafetyOffset = collisionSafetyOffset;
        this.collisionMinDistance = collisionMinDistance;
        this.collisionInSpeed = collisionInSpeed;
        this.collisionOutSpeed = collisionOutSpeed;

        SyncFreeAnglesFromCurrent();
    }

    public void UpdateFree(Transform playerTransform, Vector2 lookInput, float deltaTime)
    {
        if (playerTransform == null)
        {
            return;
        }

        currentHorizontalAngle += lookInput.x * rotationSpeed;
        currentVerticalAngle -= lookInput.y * rotationSpeed;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f);
        Vector3 desiredPosition = playerTransform.position + rotation * offset;
        Vector3 playerAimPoint = ResolvePlayerAimPoint(playerTransform);
        Vector3 resolvedPosition = collisionSolver.ResolvePosition(
            playerAimPoint,
            desiredPosition,
            cameraTransform.position,
            collisionMask,
            collisionRadius,
            collisionSafetyOffset,
            collisionMinDistance,
            collisionInSpeed,
            collisionOutSpeed,
            deltaTime,
            playerTransform,
            smoothWhenClear: false,
            smoothWhenColliding: false);
        Quaternion desiredRotation = ResolveLookRotation(playerAimPoint - resolvedPosition, cameraTransform.rotation);

        float smoothingFactor = ComputeSmoothingFactor(freeCameraSmoothing, deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, resolvedPosition, smoothingFactor);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, smoothingFactor);
    }

    public void UpdateLockOn(Transform playerTransform, Vector3 lockedTargetPoint, float deltaTime)
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector3 vectorToTarget = lockedTargetPoint - playerTransform.position;
        if (vectorToTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        // Keep lock-on camera height stable by orbiting from planar (XZ) direction only.
        Vector3 planarDirectionToTarget = Vector3.ProjectOnPlane(vectorToTarget, Vector3.up);
        if (planarDirectionToTarget.sqrMagnitude <= 0.0001f)
        {
            planarDirectionToTarget = playerTransform.forward;
        }

        Vector3 directionToTarget = planarDirectionToTarget.normalized;
        float followDistance = Mathf.Max(0.1f, Mathf.Abs(offset.z));
        Vector3 right = Vector3.Cross(Vector3.up, directionToTarget).normalized;
        Vector3 desiredCameraPosition =
            playerTransform.position
            - directionToTarget * followDistance
            + right * offset.x
            + Vector3.up * offset.y;
        Vector3 playerAimPoint = ResolvePlayerAimPoint(playerTransform);
        Vector3 resolvedPosition = collisionSolver.ResolvePosition(
            playerAimPoint,
            desiredCameraPosition,
            cameraTransform.position,
            collisionMask,
            collisionRadius,
            collisionSafetyOffset,
            collisionMinDistance,
            collisionInSpeed,
            collisionOutSpeed,
            deltaTime,
            playerTransform,
            smoothWhenClear: false,
            smoothWhenColliding: false);

        Quaternion desiredCameraRotation = ResolveLookRotation(
            lockedTargetPoint - resolvedPosition,
            cameraTransform.rotation);

        float smoothingFactor = ComputeSmoothingFactor(lockOnCameraSmoothing, deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, resolvedPosition, smoothingFactor);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredCameraRotation, smoothingFactor);
    }

    public void SyncFreeAnglesFromCurrent()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        if (cameraForward != Vector3.zero)
        {
            currentHorizontalAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
        }

        Vector3 cameraForwardNormalized = cameraTransform.forward.normalized;
        currentVerticalAngle = -Mathf.Asin(cameraForwardNormalized.y) * Mathf.Rad2Deg;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
    }

    private static Vector3 ResolvePlayerAimPoint(Transform playerTransform)
    {
        return playerTransform.position + Vector3.up * PlayerLookHeight;
    }

    private static Quaternion ResolveLookRotation(Vector3 lookDirection, Quaternion fallbackRotation)
    {
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return fallbackRotation;
        }

        return Quaternion.LookRotation(lookDirection.normalized);
    }

    private static float ComputeSmoothingFactor(float smoothing, float deltaTime)
    {
        if (smoothing <= 0f)
        {
            return 1f;
        }

        float clampedDeltaTime = Mathf.Max(0f, deltaTime);
        return 1f - Mathf.Exp(-smoothing * clampedDeltaTime);
    }
}
