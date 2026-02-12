using UnityEngine;
using System.Collections.Generic;
using Combat;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(CombatFlagsComponent))]
public class Enemy : MonoBehaviour, ICombatant
{
    [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;
    [SerializeField] private bool disableGameObjectOnDeath = true;

    private HealthComponent health;
    private CombatFlagsComponent flags;
    private Rigidbody body;
    private readonly List<ICombatOutcomeFeedbackHook> outcomeFeedbackHooks = new List<ICombatOutcomeFeedbackHook>(4);

    public CombatTeam Team => CombatTeam.Enemy;
    public bool IsVulnerable => flags != null && flags.IsVulnerable;
    public bool IsAttacking
    {
        get => flags != null && flags.IsAttacking;
        set
        {
            if (flags != null)
            {
                flags.IsAttacking = value;
            }
        }
    }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        flags = GetComponent<CombatFlagsComponent>();
        if (health == null)
        {
            health = gameObject.AddComponent<HealthComponent>();
        }

        if (flags == null)
        {
            flags = gameObject.AddComponent<CombatFlagsComponent>();
        }

        EnsureRigidbody();

        if (health != null)
        {
            health.OnDied += HandleDied;
        }

        CacheOutcomeFeedbackHooks();
        SyncCombatFlags();
        EnsureHurtbox();
    }

    private void Start()
    {
        // Set layer to Enemy for camera lock-on detection (if not set already)
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    public void ReceiveHit(AttackHitInfo hit)
    {
        if (health == null)
        {
            return;
        }

        DamageResolution resolution = DamageResolver.ResolveDamage(hit.Damage, flags, blockDamageMultiplier);
        DispatchOutcomeFeedback(hit, resolution);

        if (resolution.Outcome == DamageOutcome.Ignored)
        {
            return;
        }

        health.ApplyDamage(resolution.AppliedDamage);
        SyncCombatFlags();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }
    }

    private void HandleDied()
    {
        SyncCombatFlags();

        if (disableGameObjectOnDeath)
        {
            gameObject.SetActive(false);
        }
    }

    private void SyncCombatFlags()
    {
        if (flags == null)
        {
            return;
        }

        bool isAlive = health == null || !health.IsDead;
        flags.IsVulnerable = isAlive;
        flags.IsBlocking = false;

        if (!isAlive)
        {
            flags.IsAttacking = false;
        }
    }

    private void EnsureHurtbox()
    {
        Hurtbox existing = GetComponentInChildren<Hurtbox>(true);
        if (existing != null)
        {
            existing.SetOwner(this);
            ApplyHurtboxLayer(existing.gameObject);
            return;
        }

        CapsuleCollider bodyCollider = GetComponent<CapsuleCollider>();
        if (bodyCollider == null)
        {
            UnityEngine.Debug.LogWarning("[Enemy] No CapsuleCollider found to size hurtbox.", this);
            return;
        }

        GameObject hurtboxObject = new GameObject("Hurtbox");
        hurtboxObject.transform.SetParent(transform, false);
        hurtboxObject.transform.localPosition = Vector3.zero;
        hurtboxObject.transform.localRotation = Quaternion.identity;

        CapsuleCollider hurtCollider = hurtboxObject.AddComponent<CapsuleCollider>();
        hurtCollider.isTrigger = true;
        hurtCollider.radius = bodyCollider.radius;
        hurtCollider.height = bodyCollider.height;
        hurtCollider.direction = bodyCollider.direction;
        hurtCollider.center = bodyCollider.center;

        Hurtbox hurtbox = hurtboxObject.AddComponent<Hurtbox>();
        hurtbox.SetOwner(this);
        ApplyHurtboxLayer(hurtboxObject);
    }

    private static void ApplyHurtboxLayer(GameObject hurtboxObject)
    {
        int hurtboxLayer = LayerMask.NameToLayer("Hurtbox");
        if (hurtboxLayer >= 0)
        {
            hurtboxObject.layer = hurtboxLayer;
        }
    }

    private void EnsureRigidbody()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.linearDamping = 0f;
        body.angularDamping = 0.05f;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void CacheOutcomeFeedbackHooks()
    {
        outcomeFeedbackHooks.Clear();
        GetComponents(outcomeFeedbackHooks);
    }

    private void DispatchOutcomeFeedback(AttackHitInfo hit, DamageResolution resolution)
    {
        if (outcomeFeedbackHooks.Count == 0)
        {
            return;
        }

        Vector3 pushDirection = ResolveDefenderPushDirection(hit.Attacker);
        var context = new CombatOutcomeFeedbackContext
        {
            Hit = hit,
            Resolution = resolution,
            Defender = this,
            DefenderPushDirection = pushDirection,
            HitPoint = hit.HitPoint
        };

        for (int i = 0; i < outcomeFeedbackHooks.Count; i++)
        {
            outcomeFeedbackHooks[i]?.OnCombatOutcome(context);
        }
    }

    private Vector3 ResolveDefenderPushDirection(ICombatant attacker)
    {
        if (attacker is Component attackerComponent)
        {
            Vector3 fromAttacker = transform.position - attackerComponent.transform.position;
            fromAttacker.y = 0f;
            if (fromAttacker.sqrMagnitude > 0.0001f)
            {
                return fromAttacker.normalized;
            }
        }

        Vector3 fallback = -transform.forward;
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.back;
    }
}
