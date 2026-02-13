using UnityEngine;

public sealed class CameraOrbitRig
{
    private readonly Transform cameraTransform;
    private readonly Vector3 offset;
    private readonly float rotationSpeed;
    private readonly float cameraSmoothSpeed;
    private readonly float minVerticalAngle;
    private readonly float maxVerticalAngle;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;

    public CameraOrbitRig(
        Transform cameraTransform,
        Vector3 offset,
        float rotationSpeed,
        float cameraSmoothSpeed,
        float minVerticalAngle,
        float maxVerticalAngle)
    {
        this.cameraTransform = cameraTransform;
        this.offset = offset;
        this.rotationSpeed = rotationSpeed;
        this.cameraSmoothSpeed = cameraSmoothSpeed;
        this.minVerticalAngle = minVerticalAngle;
        this.maxVerticalAngle = maxVerticalAngle;

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
        Quaternion desiredRotation = Quaternion.LookRotation(playerTransform.position + Vector3.up * 1.5f - desiredPosition);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, deltaTime * cameraSmoothSpeed);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, desiredRotation, deltaTime * cameraSmoothSpeed);
    }

    public void UpdateLockOn(Transform playerTransform, Transform lockedTarget, float deltaTime)
    {
        if (playerTransform == null || lockedTarget == null)
        {
            return;
        }

        Vector3 directionToTarget = (lockedTarget.position - playerTransform.position).normalized;
        Vector3 desiredCameraPosition = playerTransform.position - directionToTarget * offset.magnitude + Vector3.up * offset.y;
        Quaternion desiredCameraRotation = Quaternion.LookRotation(lockedTarget.position + Vector3.up - desiredCameraPosition);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredCameraPosition, deltaTime * cameraSmoothSpeed);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, desiredCameraRotation, deltaTime * cameraSmoothSpeed);
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
}