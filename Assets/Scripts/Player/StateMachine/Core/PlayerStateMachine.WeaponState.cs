namespace Player.StateMachine
{
    using UnityEngine;
    using Player.StateMachine.States;

    public partial class PlayerStateMachine
    {
        [Header("Initial State")]
        [SerializeField] private bool startEquipped = false;

        [Header("Weapon Settings")]
        [SerializeField] private float unequipDelay = 10f;
        [SerializeField, Min(0f)] private float blockRequestBufferAfterPress = 0.6f;

        [SerializeField]
        [Tooltip("Optional fallback timeout (seconds) if transition animation never completes. Set to 0 to disable.")]
        private float weaponTransitionTimeout = 0f;

        public bool IsEquipped { get; private set; }
        public bool IsTransitioningWeapon { get; private set; }

        private float unequipTimer = -1f;
        private float bufferedBlockRequestUntil = float.NegativeInfinity;
        private bool hasPendingUnequipRequest;
        private bool requestedEquipWhilePending;
        private bool pendingEquipRequest;
        private bool pendingUnequipRequest;
        private WeaponTransitionType currentWeaponTransition = WeaponTransitionType.None;
        private float weaponTransitionStartTime = -1f;

        private static readonly int EquipToUnequip01Hash = Animator.StringToHash("Equip To Unequip 01");
        private static readonly int EquipToUnequip02Hash = Animator.StringToHash("Equip To Unequip 02");
        private static readonly int EquipToUnequip03Hash = Animator.StringToHash("Equip To Unequip 03");
        private static readonly int EquipToUnequip04Hash = Animator.StringToHash("Equip To Unequip 04");
        private static readonly int EquipToUnequip05Hash = Animator.StringToHash("Equip To Unequip 05");
        private static readonly int UnequipToEquipQuickHash = Animator.StringToHash("Unequip To Equip Quick");

        public void RequestEquip()
        {
            CancelUnequipRequest();

            if (IsEquipped || IsTransitioningWeapon)
            {
                if (IsTransitioningWeapon)
                {
                    pendingEquipRequest = true;
                }
                return;
            }

            BeginEquipTransition();
        }

        public void RequestUnequip()
        {
            if (!IsEquipped || IsTransitioningWeapon)
            {
                if (IsTransitioningWeapon)
                {
                    pendingUnequipRequest = true;
                }
                return;
            }

            hasPendingUnequipRequest = true;
            unequipTimer = unequipDelay;
            requestedEquipWhilePending = false;

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerStateMachine] Unequip requested. Will unequip in {unequipDelay}s");
            }
        }

        public void CancelUnequipRequest()
        {
            if (!hasPendingUnequipRequest)
            {
                return;
            }

            hasPendingUnequipRequest = false;
            unequipTimer = -1f;
            requestedEquipWhilePending = false;

            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] Unequip request cancelled");
            }
        }

        public void NotifyEquipAnimationComplete()
        {
            if (!IsTransitioningWeapon)
            {
                return;
            }

            IsEquipped = true;
            Animator?.SetBool("IsEquipped", true);
            OnWeaponStateChanged?.Invoke(true);

            IsTransitioningWeapon = false;
            currentWeaponTransition = WeaponTransitionType.None;
            Animator?.SetBool(IsTransitioningWeaponHash, false);

            if (WantsGuard() && Motor != null && Motor.IsGrounded)
            {
                ChangeState(GetState<BlockingState>());
            }
            else if (HasMoveIntent)
            {
                ChangeState(SprintHeld ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ChangeState(GetState<IdleState>());
            }

            if (!pendingUnequipRequest)
            {
                return;
            }

            pendingUnequipRequest = false;
            RequestUnequip();
        }

        public void NotifyUnequipAnimationComplete()
        {
            if (!IsTransitioningWeapon)
            {
                return;
            }

            IsEquipped = false;
            Animator?.SetBool("IsEquipped", false);
            OnWeaponStateChanged?.Invoke(false);

            IsTransitioningWeapon = false;
            currentWeaponTransition = WeaponTransitionType.None;
            Animator?.SetBool(IsTransitioningWeaponHash, false);

            if (HasMoveIntent)
            {
                ChangeState(SprintHeld ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ChangeState(GetState<IdleState>());
            }

            if (!pendingEquipRequest)
            {
                return;
            }

            pendingEquipRequest = false;
            RequestEquip();
        }

        private void UpdateWeaponState()
        {
            bool isLockedOn = CameraController != null && CameraController.IsLockedOn;
            bool isGrounded = Motor != null && Motor.IsGrounded;
            bool canBlock = isGrounded;
            bool isAttacking = CurrentState is AttackState;
            if (IntentSource != null && IntentSource.BlockPressed && canBlock)
            {
                bufferedBlockRequestUntil = Time.time + blockRequestBufferAfterPress;
            }

            bool wantsGuard = WantsGuard() && canBlock;
            bool wantsEquip = isLockedOn || wantsGuard || isAttacking;
            bool isSprinting = SprintHeld;
            bool isLanding = CurrentState is JumpEndState;
            bool canUnequipNow = Motor != null && Motor.IsGrounded && !isSprinting && !isLanding;

            if (wantsEquip && !IsEquipped && !IsTransitioningWeapon)
            {
                if (isGrounded)
                {
                    RequestEquip();
                }
            }
            else if (!wantsEquip && IsEquipped && !hasPendingUnequipRequest)
            {
                RequestUnequip();
            }

            if (hasPendingUnequipRequest)
            {
                if (wantsEquip)
                {
                    requestedEquipWhilePending = true;
                    unequipTimer = unequipDelay;
                }
                else
                {
                    requestedEquipWhilePending = false;
                }
            }

            if (hasPendingUnequipRequest && !wantsEquip)
            {
                if (!canUnequipNow)
                {
                    unequipTimer = unequipDelay;
                }
                else
                {
                    if (unequipTimer > 0f)
                    {
                        unequipTimer -= Time.deltaTime;
                    }

                    if (unequipTimer <= 0f)
                    {
                        if (!requestedEquipWhilePending)
                        {
                            BeginUnequipTransition();
                        }
                        hasPendingUnequipRequest = false;
                        requestedEquipWhilePending = false;
                    }
                }
            }

            CheckWeaponTransitionCompletion();
        }

        private bool WantsGuard()
        {
            return BlockHeld || Time.time <= bufferedBlockRequestUntil;
        }

        private void BeginEquipTransition()
        {
            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] Equipping weapon (transition)");
            }

            IsTransitioningWeapon = true;
            currentWeaponTransition = WeaponTransitionType.Equipping;
            weaponTransitionStartTime = Time.time;
            Animator?.SetTrigger("Equip");
            Animator?.SetBool(IsTransitioningWeaponHash, true);
        }

        private void BeginUnequipTransition()
        {
            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] Unequipping weapon (transition)");
            }

            IsTransitioningWeapon = true;
            currentWeaponTransition = WeaponTransitionType.Unequipping;
            weaponTransitionStartTime = Time.time;
            SetUnequipVariant();
            Animator?.SetTrigger("Unequip");
            Animator?.SetBool(IsTransitioningWeaponHash, true);
        }

        private void SetUnequipVariant()
        {
            if (Animator == null)
            {
                return;
            }

            int variant = Random.Range(0, 5);
            Animator.SetInteger(UnequipVariantHash, variant);
        }

        private void CheckWeaponTransitionCompletion()
        {
            if (!IsTransitioningWeapon || Animator == null)
            {
                return;
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            int shortHash = stateInfo.shortNameHash;

            bool isEquipTransitionState = shortHash == UnequipToEquipQuickHash;
            bool isUnequipTransitionState = shortHash == EquipToUnequip01Hash || shortHash == EquipToUnequip02Hash ||
                                            shortHash == EquipToUnequip03Hash || shortHash == EquipToUnequip04Hash ||
                                            shortHash == EquipToUnequip05Hash;

            if (currentWeaponTransition == WeaponTransitionType.Equipping && isEquipTransitionState &&
                stateInfo.normalizedTime >= 0.95f)
            {
                NotifyEquipAnimationComplete();
                return;
            }

            if (currentWeaponTransition == WeaponTransitionType.Unequipping && isUnequipTransitionState &&
                stateInfo.normalizedTime >= 0.95f)
            {
                NotifyUnequipAnimationComplete();
                return;
            }

            if (weaponTransitionTimeout > 0f && weaponTransitionStartTime > 0f &&
                Time.time - weaponTransitionStartTime >= weaponTransitionTimeout)
            {
                if (currentWeaponTransition == WeaponTransitionType.Equipping)
                {
                    NotifyEquipAnimationComplete();
                }
                else if (currentWeaponTransition == WeaponTransitionType.Unequipping)
                {
                    NotifyUnequipAnimationComplete();
                }
            }
        }

        private enum WeaponTransitionType
        {
            None,
            Equipping,
            Unequipping
        }
    }
}
