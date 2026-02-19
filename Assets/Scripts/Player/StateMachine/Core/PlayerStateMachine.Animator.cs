namespace Player.StateMachine
{
    using UnityEngine;

    public partial class PlayerStateMachine
    {
        private static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
        private static readonly int IsTransitioningWeaponHash = Animator.StringToHash("IsTransitioningWeapon");
        private static readonly int UnequipVariantHash = Animator.StringToHash("UnequipVariant");

        private void UpdateAnimatorParameters()
        {
            if (Animator == null)
            {
                return;
            }

            Animator.SetBool("IsEquipped", IsEquipped);
            Animator.SetBool(IsTransitioningWeaponHash, IsTransitioningWeapon);

            bool isLockedOn = CameraController != null && CameraController.IsLockedOn;
            bool isBlockingByInput = BlockHeld && IsEquipped && Motor != null && Motor.IsGrounded;
            bool isBlockingForAnimator = isBlockingByInput || IsDefenseReactionActive;
            Animator.SetBool("IsLockedOn", isLockedOn);
            Animator.SetBool(IsBlockingHash, isBlockingForAnimator);

            if (!IsTransitioningWeapon)
            {
                return;
            }

            Animator.SetBool(IsMovingHash, false);
            Animator.SetBool(IsSprintingHash, false);
            Animator.SetFloat(VelocityXHash, 0f);
            Animator.SetFloat(VelocityZHash, 0f);
            Animator.SetFloat(SpeedHash, 0f);
        }
    }
}
