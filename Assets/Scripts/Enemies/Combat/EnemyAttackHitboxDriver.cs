namespace Enemies.Combat
{
    using Combat;
    using Enemies.StateMachine;
    using Player.Combat;
    using global::StateMachine.Core;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStateMachine))]
    public sealed class EnemyAttackHitboxDriver : MonoBehaviour
    {
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private Enemy combatant;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private string weaponBoneName = "katana_r";

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (combatant == null)
            {
                combatant = GetComponent<Enemy>();
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

            EndAttack();
        }

        public void OnAttackWindup()
        {
            if (hitbox == null || stateMachine == null)
            {
                return;
            }

            if (stateMachine.CurrentAttackStep.HasValue)
            {
                AttackData attack = AttackDataMapper.ToAttackData(stateMachine.CurrentAttackStep.Value);
                hitbox.BeginAttack(attack);
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
            EndAttack();
        }

        private void HandleStateChanged(IState previous, IState current)
        {
            if (current is Enemies.StateMachine.States.EnemyAttackTurnState)
            {
                return;
            }

            EndAttack();
        }

        private void EndAttack()
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

        private Hitbox TryCreateHitbox()
        {
            Transform weaponBone = FindChildByName(transform, weaponBoneName);
            if (weaponBone == null)
            {
                Debug.LogWarning($"[EnemyAttackHitboxDriver] Could not find bone '{weaponBoneName}'.", this);
                return null;
            }

            var hitboxObject = new GameObject("WeaponHitbox");
            hitboxObject.transform.SetParent(weaponBone, false);
            hitboxObject.transform.localPosition = Vector3.zero;
            hitboxObject.transform.localRotation = Quaternion.identity;

            var boxCollider = hitboxObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(0.1f, 0.5f, 0.1f);
            boxCollider.center = new Vector3(0f, 0.25f, 0f);

            Hitbox createdHitbox = hitboxObject.AddComponent<Hitbox>();

            int hitboxLayer = LayerMask.NameToLayer("Hitbox");
            if (hitboxLayer >= 0)
            {
                hitboxObject.layer = hitboxLayer;
            }

            Debug.LogWarning("[EnemyAttackHitboxDriver] Created WeaponHitbox. Tune collider size/position in prefab.", this);
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
