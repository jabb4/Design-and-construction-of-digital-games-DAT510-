using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private GameObject ownerObject;

        private ICombatant owner;
        private readonly HashSet<Hurtbox> alreadyHit = new HashSet<Hurtbox>();
        private bool active;
        private AttackData? currentAttack;

        public ICombatant Owner => owner;
        public bool Active => active;
        public AttackData? CurrentAttack => currentAttack;

        private void Awake()
        {
            ResolveOwner();
            SetActive(false);
        }

        private void OnValidate()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                Debug.LogWarning("[Hitbox] Collider should be set to Is Trigger.", this);
            }
            ResolveOwner();
        }

        public void SetOwner(ICombatant combatant)
        {
            owner = combatant;
            var component = combatant as Component;
            ownerObject = component != null ? component.gameObject : null;
        }

        public void BeginAttack(AttackData attack)
        {
            currentAttack = attack;
            alreadyHit.Clear();
        }

        public void EndAttack()
        {
            currentAttack = null;
            alreadyHit.Clear();
            SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            active = isActive;
        }

        private void ResolveOwner()
        {
            if (ownerObject == null)
            {
                owner = null;
                return;
            }

            owner = ownerObject.GetComponent<ICombatant>();
            if (owner == null)
            {
                Debug.LogWarning($"[Hitbox] Owner on {name} does not implement ICombatant.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryHit(other);
        }

        private void TryHit(Collider other)
        {
            if (!active)
            {
                return;
            }

            if (owner == null || !owner.IsAttacking)
            {
                return;
            }

            Hurtbox hurtbox = other.GetComponentInParent<Hurtbox>();
            if (hurtbox == null)
            {
                return;
            }

            ICombatant hurtOwner = hurtbox.Owner;
            if (hurtOwner == null)
            {
                return;
            }

            if (ReferenceEquals(owner, hurtOwner))
            {
                return;
            }

            if (!hurtOwner.IsVulnerable)
            {
                return;
            }

            if (alreadyHit.Contains(hurtbox))
            {
                return;
            }

            alreadyHit.Add(hurtbox);

            float damage = currentAttack?.Damage ?? 0f;
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            if (hitNormal == Vector3.zero) hitNormal = Vector3.forward;
            var hit = new AttackHitInfo
            {
                Damage = damage,
                Attacker = owner,
                Attack = currentAttack,
                HitPoint = hitPoint,
                HitNormal = hitNormal
            };

            hurtOwner.ReceiveHit(hit);
        }

    }
}
