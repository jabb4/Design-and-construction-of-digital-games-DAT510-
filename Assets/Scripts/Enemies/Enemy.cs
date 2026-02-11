using UnityEngine;
using Combat;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(CombatFlagsComponent))]
public class Enemy : MonoBehaviour, ICombatant
{
    [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;
    [SerializeField] private bool disableGameObjectOnDeath = true;

    private HealthComponent health;
    private CombatFlagsComponent flags;

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

        if (health != null)
        {
            health.OnDied += HandleDied;
        }

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
}
