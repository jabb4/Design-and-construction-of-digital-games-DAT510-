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
        private float weaponTransitionTimeout = 3f;

        [Header("Weapon Transition SFX")]
        [SerializeField] private AudioSource weaponTransitionAudioSource;

        [Header("Unsheathe SFX")]
        [SerializeField] private AudioClip unsheatheSfx;
        [SerializeField, Range(0f, 2f)] private float unsheatheSfxVolume = 1f;
        [SerializeField, Range(0.5f, 2f)] private float unsheatheSfxMinPitch = 0.96f;
        [SerializeField, Range(0.5f, 2f)] private float unsheatheSfxMaxPitch = 1.04f;

        [Header("Sheathe SFX")]
        [SerializeField] private AudioClip sheatheSfx;
        [SerializeField, Range(0f, 2f)] private float sheatheSfxVolume = 1f;
        [SerializeField, Range(0.5f, 2f)] private float sheatheSfxMinPitch = 0.96f;
        [SerializeField, Range(0.5f, 2f)] private float sheatheSfxMaxPitch = 1.04f;

        [Tooltip("Seconds before the end of the sheathe animation to play the sheathe sound.")]
        [SerializeField, Range(0.1f, 2f)] private float sheatheSfxSecondsBeforeEnd = 0.4f;

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
        private bool sheatheSfxPlayed;

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
                ForceChangeState(GetState<BlockingState>());
            }
            else if (HasMoveIntent)
            {
                ForceChangeState(SprintHeld ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ForceChangeState(GetState<IdleState>());
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
                ForceChangeState(SprintHeld ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ForceChangeState(GetState<IdleState>());
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
            PlayWeaponTransitionSfx(unsheatheSfx, unsheatheSfxVolume, unsheatheSfxMinPitch, unsheatheSfxMaxPitch);
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
            sheatheSfxPlayed = false;
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

            if (currentWeaponTransition == WeaponTransitionType.Unequipping && isUnequipTransitionState)
            {
                if (!sheatheSfxPlayed)
                {
                    float clipLength = stateInfo.length;
                    float elapsed = stateInfo.normalizedTime * clipLength;
                    float remaining = clipLength - elapsed;
                    if (remaining <= sheatheSfxSecondsBeforeEnd)
                    {
                        sheatheSfxPlayed = true;
                        PlayWeaponTransitionSfx(sheatheSfx, sheatheSfxVolume, sheatheSfxMinPitch, sheatheSfxMaxPitch);
                    }
                }

                if (stateInfo.normalizedTime >= 0.95f)
                {
                    NotifyUnequipAnimationComplete();
                    return;
                }
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

        private void PlayWeaponTransitionSfx(AudioClip clip, float volume, float minPitch, float maxPitch)
        {
            if (clip == null || weaponTransitionAudioSource == null)
            {
                return;
            }

            float pitch = Random.Range(
                Mathf.Clamp(minPitch, 0.5f, 2f),
                Mathf.Clamp(maxPitch, 0.5f, 2f));

            weaponTransitionAudioSource.pitch = pitch;
            weaponTransitionAudioSource.PlayOneShot(clip, Mathf.Clamp(volume, 0f, 2f));
        }

        private enum WeaponTransitionType
        {
            None,
            Equipping,
            Unequipping
        }
    }
}
