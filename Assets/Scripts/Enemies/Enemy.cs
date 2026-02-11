using UnityEngine;
using Combat;

public class Enemy : MonoBehaviour, ICombatant
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    private bool isDead;

    public CombatTeam Team => CombatTeam.Enemy;
    public bool IsVulnerable => !isDead;
    public bool IsAttacking => false;

    private void Awake()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }

        EnsureHurtbox();
    }

    private void Start()
    {
        // Set layer to Enemy for camera lock-on detection (if not set already)
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    public void ReceiveHit(AttackHitInfo hit)
    {
        if (isDead)
        {
            return;
        }

        float damage = Mathf.Max(0f, hit.Damage);
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0f;
        gameObject.SetActive(false);
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
