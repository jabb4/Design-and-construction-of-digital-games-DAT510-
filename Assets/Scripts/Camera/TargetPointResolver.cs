using UnityEngine;

public static class TargetPointResolver
{
    public static Vector3 ResolveTargetPoint(Transform target, float fallbackHeightOffset)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider == null)
        {
            targetCollider = target.GetComponentInChildren<Collider>();
        }

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return target.position + Vector3.up * fallbackHeightOffset;
    }
}
