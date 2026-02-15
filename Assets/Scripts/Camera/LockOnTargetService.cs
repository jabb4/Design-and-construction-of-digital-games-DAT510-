using Combat;
using UnityEngine;

public sealed class LockOnTargetService
{
    private readonly Transform playerTransform;
    private readonly Camera camera;
    private readonly LayerMask enemyLayer;
    private readonly LayerMask lineOfSightBlockerLayer;
    private readonly float lockOnRange;
    private readonly float lockBreakDistance;
    private readonly float lineOfSightGraceSeconds;
    private readonly float targetPointHeightOffset;
    private readonly bool enableDebugLogs;

    private readonly RaycastHit[] lineOfSightHits = new RaycastHit[16];
    private float occlusionTimer;

    public LockOnTargetService(
        Transform playerTransform,
        Camera camera,
        LayerMask enemyLayer,
        LayerMask lineOfSightBlockerLayer,
        float lockOnRange,
        float lockBreakDistance,
        float lineOfSightGraceSeconds,
        float targetPointHeightOffset,
        bool enableDebugLogs)
    {
        this.playerTransform = playerTransform;
        this.camera = camera;
        this.enemyLayer = enemyLayer;
        this.lineOfSightBlockerLayer = lineOfSightBlockerLayer;
        this.lockOnRange = Mathf.Max(0f, lockOnRange);
        this.lockBreakDistance = Mathf.Max(0f, lockBreakDistance);
        this.lineOfSightGraceSeconds = Mathf.Max(0f, lineOfSightGraceSeconds);
        this.targetPointHeightOffset = targetPointHeightOffset;
        this.enableDebugLogs = enableDebugLogs;
    }

    public void NotifyLockedTargetChanged()
    {
        occlusionTimer = 0f;
    }

    public void ResetState()
    {
        occlusionTimer = 0f;
    }

    public Transform FindBestTarget(Transform excludeTarget = null)
    {
        if (playerTransform == null || camera == null)
        {
            return null;
        }

        Collider[] enemiesInRange = Physics.OverlapSphere(playerTransform.position, lockOnRange, enemyLayer);
        if (enemiesInRange.Length == 0)
        {
            return null;
        }

        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        Vector3 playerViewportPos = camera.WorldToViewportPoint(TargetPointResolver.ResolveTargetPoint(playerTransform, targetPointHeightOffset));

        foreach (Collider enemy in enemiesInRange)
        {
            Transform candidate = enemy.transform;
            if (candidate == null || candidate == excludeTarget)
            {
                continue;
            }

            if (!IsCandidateValid(candidate))
            {
                continue;
            }

            Vector3 viewportPos = camera.WorldToViewportPoint(TargetPointResolver.ResolveTargetPoint(candidate, targetPointHeightOffset));
            if (!IsInViewport(viewportPos))
            {
                continue;
            }

            bool isInTopSection = viewportPos.y > playerViewportPos.y;
            float sectionPriority = isInTopSection ? 0f : 1f;
            float centerDistance = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
            float playerDistance = Vector3.Distance(playerTransform.position, candidate.position);

            float score = sectionPriority * 1000f + playerDistance * 100f + centerDistance;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    public Transform FindSwitchTarget(Transform currentTarget, Vector2 inputDirection)
    {
        if (playerTransform == null || camera == null || currentTarget == null || inputDirection.magnitude < 0.1f)
        {
            return null;
        }

        Collider[] enemiesInRange = Physics.OverlapSphere(playerTransform.position, lockOnRange, enemyLayer);
        if (enemiesInRange.Length == 0)
        {
            return null;
        }

        Vector3 currentTargetViewport = camera.WorldToViewportPoint(TargetPointResolver.ResolveTargetPoint(currentTarget, targetPointHeightOffset));
        Vector2 normalizedInput = inputDirection.normalized;

        Transform bestTarget = null;
        float bestAngle = float.MaxValue;

        foreach (Collider enemy in enemiesInRange)
        {
            Transform candidate = enemy.transform;
            if (candidate == null || candidate == currentTarget)
            {
                continue;
            }

            if (!IsCandidateValid(candidate))
            {
                continue;
            }

            Vector3 viewportPos = camera.WorldToViewportPoint(TargetPointResolver.ResolveTargetPoint(candidate, targetPointHeightOffset));
            if (!IsInViewport(viewportPos))
            {
                continue;
            }

            Vector2 viewSpaceDirection = new Vector2(viewportPos.x - currentTargetViewport.x, viewportPos.y - currentTargetViewport.y);
            if (viewSpaceDirection.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float angle = Vector2.Angle(normalizedInput, viewSpaceDirection.normalized);
            float distancePenalty = viewSpaceDirection.magnitude * 0.1f;
            float weightedAngle = angle + distancePenalty;

            if (weightedAngle < bestAngle)
            {
                bestAngle = weightedAngle;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    public bool TryGetInvalidReason(Transform target, float deltaTime, out LockInvalidReason invalidReason)
    {
        invalidReason = LockInvalidReason.Manual;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            invalidReason = LockInvalidReason.InactiveOrDestroyed;
            return true;
        }

        if (IsDead(target))
        {
            invalidReason = LockInvalidReason.Dead;
            return true;
        }

        float sqrDistance = (target.position - playerTransform.position).sqrMagnitude;
        if (sqrDistance > lockBreakDistance * lockBreakDistance)
        {
            invalidReason = LockInvalidReason.OutOfRange;
            return true;
        }

        if (!HasLineOfSight(target))
        {
            occlusionTimer += Mathf.Max(0f, deltaTime);
            if (lineOfSightGraceSeconds <= 0f || occlusionTimer >= lineOfSightGraceSeconds)
            {
                invalidReason = LockInvalidReason.Occluded;
                return true;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[LockOnTargetService] Target '{target.name}' occluded ({occlusionTimer:F2}s/{lineOfSightGraceSeconds:F2}s grace).");
            }

            return false;
        }

        occlusionTimer = 0f;
        return false;
    }

    private bool IsCandidateValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (target == playerTransform || target.IsChildOf(playerTransform))
        {
            return false;
        }

        if (IsDead(target))
        {
            return false;
        }

        float sqrDistance = (target.position - playerTransform.position).sqrMagnitude;
        if (sqrDistance > lockOnRange * lockOnRange)
        {
            return false;
        }

        return HasLineOfSight(target);
    }

    private bool IsDead(Transform target)
    {
        HealthComponent health = target.GetComponent<HealthComponent>();
        if (health == null)
        {
            health = target.GetComponentInChildren<HealthComponent>();
        }

        if (health == null)
        {
            health = target.GetComponentInParent<HealthComponent>();
        }

        return health != null && health.IsDead;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 origin = camera.transform.position;
        Vector3 aimPoint = TargetPointResolver.ResolveTargetPoint(target, targetPointHeightOffset);
        Vector3 direction = aimPoint - origin;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        direction /= distance;

        int hitCount = Physics.RaycastNonAlloc(origin, direction, lineOfSightHits, distance, lineOfSightBlockerLayer, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
        {
            return true;
        }

        SortHitsByDistance(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = lineOfSightHits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            if (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform))
            {
                continue;
            }

            return hitTransform == target || hitTransform.IsChildOf(target);
        }

        return true;
    }

    private static bool IsInViewport(Vector3 viewportPos)
    {
        return viewportPos.z > 0f &&
               viewportPos.x >= 0f && viewportPos.x <= 1f &&
               viewportPos.y >= 0f && viewportPos.y <= 1f;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit key = lineOfSightHits[i];
            float keyDistance = key.distance;
            int j = i - 1;
            while (j >= 0 && lineOfSightHits[j].distance > keyDistance)
            {
                lineOfSightHits[j + 1] = lineOfSightHits[j];
                j--;
            }

            lineOfSightHits[j + 1] = key;
        }
    }
}
