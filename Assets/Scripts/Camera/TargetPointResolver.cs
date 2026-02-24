using System;
using System.Collections.Generic;
using UnityEngine;

public static class TargetPointResolver
{
    private readonly struct TargetPointLookupKey : IEquatable<TargetPointLookupKey>
    {
        public readonly int RootInstanceId;
        public readonly string PointName;

        public TargetPointLookupKey(int rootInstanceId, string pointName)
        {
            RootInstanceId = rootInstanceId;
            PointName = pointName;
        }

        public bool Equals(TargetPointLookupKey other)
        {
            return RootInstanceId == other.RootInstanceId &&
                   string.Equals(PointName, other.PointName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TargetPointLookupKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RootInstanceId * 397) ^ (PointName != null ? PointName.GetHashCode() : 0);
            }
        }
    }

    private static readonly Dictionary<TargetPointLookupKey, Transform> NamedTargetPointCache = new Dictionary<TargetPointLookupKey, Transform>(32);

    public static Vector3 ResolveTargetPoint(
        Transform target,
        float fallbackHeightOffset,
        string preferredTargetPointName = null)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        if (TryResolveNamedTargetPoint(target, preferredTargetPointName, out Transform namedTargetPoint))
        {
            return namedTargetPoint.position;
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

    private static bool TryResolveNamedTargetPoint(Transform target, string preferredTargetPointName, out Transform namedTargetPoint)
    {
        namedTargetPoint = null;
        if (string.IsNullOrWhiteSpace(preferredTargetPointName))
        {
            return false;
        }

        Transform searchRoot = ResolveSearchRoot(target);
        if (searchRoot == null)
        {
            return false;
        }

        var cacheKey = new TargetPointLookupKey(searchRoot.GetInstanceID(), preferredTargetPointName);
        if (NamedTargetPointCache.TryGetValue(cacheKey, out Transform cachedTargetPoint))
        {
            if (cachedTargetPoint != null)
            {
                namedTargetPoint = cachedTargetPoint;
                return true;
            }

            NamedTargetPointCache.Remove(cacheKey);
        }

        Transform resolvedTargetPoint = FindNamedDescendant(searchRoot, preferredTargetPointName);
        if (resolvedTargetPoint == null)
        {
            return false;
        }

        NamedTargetPointCache[cacheKey] = resolvedTargetPoint;
        namedTargetPoint = resolvedTargetPoint;
        return true;
    }

    private static Transform ResolveSearchRoot(Transform target)
    {
        Enemy enemy = target.GetComponentInParent<Enemy>();
        return enemy != null ? enemy.transform : target;
    }

    private static Transform FindNamedDescendant(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (string.Equals(root.name, name, StringComparison.Ordinal))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedDescendant(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
