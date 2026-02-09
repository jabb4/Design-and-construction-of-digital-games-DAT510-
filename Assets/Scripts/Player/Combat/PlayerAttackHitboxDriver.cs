using UnityEngine;
using Combat;
using Player.StateMachine;

namespace Player.Combat
{
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerAttackHitboxDriver : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine stateMachine;
        [SerializeField] private Player combatant;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private string weaponBoneName = "katana_r";

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (combatant == null)
            {
                combatant = GetComponent<Player>();
            }

            if (combatant == null)
            {
                combatant = gameObject.AddComponent<Player>();
            }

            if (hitbox == null)
            {
                hitbox = GetComponentInChildren<Hitbox>(true);
            }

            if (hitbox == null)
            {
                hitbox = TryCreateHitbox();
            }

            if (hitbox != null && combatant != null)
            {
                hitbox.SetOwner(combatant);
                hitbox.SetActive(false);
            }
            else if (hitbox == null)
            {
                Debug.LogWarning("[PlayerAttackHitboxDriver] No Hitbox found or created.", this);
            }
        }

        private void OnEnable()
        {
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= HandleStateChanged;
            }

            if (hitbox != null)
            {
                hitbox.EndAttack();
            }

            if (combatant != null)
            {
                combatant.IsAttacking = false;
            }
        }

        public void OnAttackWindup()
        {
            if (hitbox == null || stateMachine == null)
            {
                return;
            }

            if (stateMachine.CurrentAttackStep.HasValue)
            {
                hitbox.BeginAttack(stateMachine.CurrentAttackStep.Value);
            }
            else
            {
                hitbox.EndAttack();
            }

            hitbox.SetActive(false);

            if (combatant != null)
            {
                combatant.IsAttacking = false;
            }

        }

        public void OnAttackSlash()
        {
            if (hitbox == null)
            {
                return;
            }

            if (combatant != null)
            {
                combatant.IsAttacking = true;
            }

            hitbox.SetActive(true);

        }

        public void OnAttackRecovery()
        {
            if (hitbox != null)
            {
                hitbox.EndAttack();
            }

            if (combatant != null)
            {
                combatant.IsAttacking = false;
            }

        }

        private void HandleStateChanged(IState previous, IState current)
        {
            if (current is global::Player.StateMachine.States.AttackState)
            {
                return;
            }

            if (hitbox != null)
            {
                hitbox.EndAttack();
            }

            if (combatant != null)
            {
                combatant.IsAttacking = false;
            }
        }

        private Hitbox TryCreateHitbox()
        {
            Transform weaponBone = FindChildByName(transform, weaponBoneName);
            if (weaponBone == null)
            {
                Debug.LogWarning($"[PlayerAttackHitboxDriver] Could not find bone '{weaponBoneName}'.", this);
                return null;
            }

            GameObject hitboxObject = new GameObject("WeaponHitbox");
            hitboxObject.transform.SetParent(weaponBone, false);
            hitboxObject.transform.localPosition = Vector3.zero;
            hitboxObject.transform.localRotation = Quaternion.identity;

            BoxCollider boxCollider = hitboxObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(0.1f, 0.5f, 0.1f);
            boxCollider.center = new Vector3(0f, 0.25f, 0f);

            Hitbox createdHitbox = hitboxObject.AddComponent<Hitbox>();

            int hitboxLayer = LayerMask.NameToLayer("Hitbox");
            if (hitboxLayer >= 0)
            {
                hitboxObject.layer = hitboxLayer;
            }

            Debug.LogWarning("[PlayerAttackHitboxDriver] Created WeaponHitbox. Tune collider size/position in prefab.", this);

            return createdHitbox;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
