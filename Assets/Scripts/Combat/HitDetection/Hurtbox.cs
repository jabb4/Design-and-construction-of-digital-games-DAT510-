using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private GameObject ownerObject;

        private ICombatant owner;
        private Collider hurtCollider;

        public ICombatant Owner => owner;

        private void Awake()
        {
            hurtCollider = GetComponent<Collider>();
            if (hurtCollider != null && !hurtCollider.isTrigger)
            {
                hurtCollider.isTrigger = true;
            }

            ResolveOwner();
        }

        private void OnValidate()
        {
            if (hurtCollider == null)
            {
                hurtCollider = GetComponent<Collider>();
            }

            if (hurtCollider != null && !hurtCollider.isTrigger)
            {
                hurtCollider.isTrigger = true;
            }

            ResolveOwner();
        }

        public void SetOwner(ICombatant combatant)
        {
            owner = combatant;
            var component = combatant as Component;
            ownerObject = component != null ? component.gameObject : null;
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
                Debug.LogWarning($"[Hurtbox] Owner on {name} does not implement ICombatant.", this);
            }
        }
    }
}
