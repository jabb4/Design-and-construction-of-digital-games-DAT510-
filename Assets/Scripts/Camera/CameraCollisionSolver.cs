using UnityEngine;

public sealed class CameraCollisionSolver
{
    private readonly RaycastHit[] sphereCastHits = new RaycastHit[16];

    public Vector3 ResolvePosition(
        Vector3 pivotPoint,
        Vector3 desiredCameraPosition,
        Vector3 currentCameraPosition,
        LayerMask collisionMask,
        float collisionRadius,
        float safetyOffset,
        float minDistance,
        float collisionInSpeed,
        float collisionOutSpeed,
        float deltaTime,
        Transform ignoredRoot = null,
        bool smoothWhenClear = true,
        bool smoothWhenColliding = true)
    {
        Vector3 desiredOffset = desiredCameraPosition - pivotPoint;
        float desiredDistance = desiredOffset.magnitude;
        if (desiredDistance <= Mathf.Epsilon)
        {
            return desiredCameraPosition;
        }

        Vector3 direction = desiredOffset / desiredDistance;
        float targetDistance = desiredDistance;
        float smoothingSpeed = Mathf.Max(0f, collisionOutSpeed);

        if (TryFindCollisionDistance(
                pivotPoint,
                direction,
                desiredDistance,
                collisionMask,
                collisionRadius,
                ignoredRoot,
                out float collisionDistance))
        {
            float safeOffset = Mathf.Max(0f, safetyOffset);
            float clampedMinDistance = Mathf.Max(0f, minDistance);
            float safeDistance = Mathf.Max(clampedMinDistance, collisionDistance - safeOffset);
            targetDistance = Mathf.Min(desiredDistance, safeDistance);

            if (!smoothWhenColliding)
            {
                return pivotPoint + direction * targetDistance;
            }

            smoothingSpeed = Mathf.Max(0f, collisionInSpeed);
        }
        else if (!smoothWhenClear)
        {
            return desiredCameraPosition;
        }

        Vector3 targetPosition = pivotPoint + direction * targetDistance;
        float smoothFactor = ComputeSmoothFactor(smoothingSpeed, deltaTime);
        return Vector3.Lerp(currentCameraPosition, targetPosition, smoothFactor);
    }

    private bool TryFindCollisionDistance(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        LayerMask collisionMask,
        float collisionRadius,
        Transform ignoredRoot,
        out float collisionDistance)
    {
        collisionDistance = 0f;
        float radius = Mathf.Max(0.01f, collisionRadius);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            direction,
            sphereCastHits,
            maxDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            return false;
        }

        SortHitsByDistance(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = sphereCastHits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            if (ignoredRoot != null && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
            {
                continue;
            }

            collisionDistance = sphereCastHits[i].distance;
            return true;
        }

        return false;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit key = sphereCastHits[i];
            float keyDistance = key.distance;
            int j = i - 1;
            while (j >= 0 && sphereCastHits[j].distance > keyDistance)
            {
                sphereCastHits[j + 1] = sphereCastHits[j];
                j--;
            }

            sphereCastHits[j + 1] = key;
        }
    }

    private static float ComputeSmoothFactor(float speed, float deltaTime)
    {
        if (speed <= 0f)
        {
            return 1f;
        }

        float clampedDeltaTime = Mathf.Max(0f, deltaTime);
        return 1f - Mathf.Exp(-speed * clampedDeltaTime);
    }
}
