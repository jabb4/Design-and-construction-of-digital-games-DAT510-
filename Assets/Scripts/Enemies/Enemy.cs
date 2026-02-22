using System;
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
    private global::Combat.CombatHorizontalImpulseDriver impulseDriver;
    private readonly List<global::Combat.ICombatAttackFeedbackHook> attackFeedbackHooks = new List<global::Combat.ICombatAttackFeedbackHook>(4);
    private readonly List<ICombatOutcomeFeedbackHook> outcomeFeedbackHooks = new List<ICombatOutcomeFeedbackHook>(4);
    private bool endParryOutcomeQueued;

    public event Action<AttackHitInfo, DamageResolution> OnDamageResolved;
    public event Action<AttackHitInfo> OnParriedAttack;

    public CombatTeam Team => CombatTeam.Enemy;
    public bool IsVulnerable => flags != null && flags.IsVulnerable;
    public bool IsAlive => health == null || !health.IsDead;
    public bool IsParryWindowActive => flags != null && flags.IsParryWindowActive;
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
        EnsureImpulseDriver();

        if (health != null)
        {
            health.OnDied += HandleDied;
        }

        CacheAttackFeedbackHooks();
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

        // Enemy defense is parry-only; blocking is never valid.
        if (flags != null)
        {
            flags.IsBlocking = false;
        }

        DamageResolution resolution = DamageResolver.ResolveDamage(hit.Damage, flags, blockDamageMultiplier);
        OnDamageResolved?.Invoke(hit, resolution);
        bool isEndParry = false;
        if (resolution.Outcome == DamageOutcome.Parried)
        {
            OnParriedAttack?.Invoke(hit);
            isEndParry = ConsumeQueuedEndParryOutcome();
        }
        else
        {
            endParryOutcomeQueued = false;
        }

        DispatchOutcomeFeedback(hit, resolution, isEndParry);

        if (resolution.Outcome == DamageOutcome.Ignored)
        {
            return;
        }

        health.ApplyDamage(resolution.AppliedDamage);
        SyncCombatFlags();
    }

    public void OpenParryWindow(float durationSeconds)
    {
        if (flags == null || !IsAlive)
        {
            return;
        }

        flags.IsBlocking = false;
        flags.OpenParryWindow(durationSeconds);
    }

    public void CloseParryWindow()
    {
        flags?.CloseParryWindow();
    }

    public void QueueEndParryOutcomeFeedback()
    {
        endParryOutcomeQueued = true;
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
        CloseParryWindow();
        endParryOutcomeQueued = false;
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
            flags.CloseParryWindow();
            endParryOutcomeQueued = false;
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

    private void EnsureImpulseDriver()
    {
        impulseDriver = GetComponent<global::Combat.CombatHorizontalImpulseDriver>();
        if (impulseDriver == null)
        {
            impulseDriver = gameObject.AddComponent<global::Combat.CombatHorizontalImpulseDriver>();
        }
    }

    private void CacheAttackFeedbackHooks()
    {
        attackFeedbackHooks.Clear();
        GetComponents(attackFeedbackHooks);
    }

    public void NotifyAttackPhase(
        global::Combat.CombatAttackPhase phase,
        global::Combat.AttackData? attack = null,
        Vector3? attackDirection = null)
    {
        if (attackFeedbackHooks.Count == 0)
        {
            return;
        }

        Vector3 direction = attackDirection ?? ResolveAttackDirection();
        var context = new global::Combat.CombatAttackFeedbackContext
        {
            Phase = phase,
            Attack = attack,
            Attacker = this,
            AttackDirection = direction
        };

        for (int i = 0; i < attackFeedbackHooks.Count; i++)
        {
            attackFeedbackHooks[i]?.OnCombatAttackPhase(context);
        }
    }

    private void CacheOutcomeFeedbackHooks()
    {
        outcomeFeedbackHooks.Clear();
        GetComponents(outcomeFeedbackHooks);
    }

    private void DispatchOutcomeFeedback(AttackHitInfo hit, DamageResolution resolution, bool isEndParry)
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
            HitPoint = hit.HitPoint,
            IsEndParry = isEndParry
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

    private Vector3 ResolveAttackDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            return forward.normalized;
        }

        return Vector3.forward;
    }

    private bool ConsumeQueuedEndParryOutcome()
    {
        bool queued = endParryOutcomeQueued;
        endParryOutcomeQueued = false;
        return queued;
    }
}
