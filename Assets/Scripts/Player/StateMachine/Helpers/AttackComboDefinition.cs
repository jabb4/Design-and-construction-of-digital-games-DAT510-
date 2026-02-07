namespace Player.StateMachine
{
    /// <summary>
    ///  Defines a 5 attack combo for the player.
    /// Time definitions are only fallbacks, timings are handled with WeaponAnimationEvents in the animation clips.
    /// </summary>
    public static class AttackComboDefinition
    {
        public static readonly AttackStep[] Attacks =
        {
            new() {
                AnimationStateName = "Stand_Attack_01 1",
                StartPose = AttackPoseDirection.RightUp,
                EndPose = AttackPoseDirection.LeftDown,
                SlashStartTime = 0.1f,
                RecoveryStartTime = 0.3f,
                ComboWindowStart = 0.3f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "Stand_Attack_03 2",
                StartPose = AttackPoseDirection.LeftDown,
                EndPose = AttackPoseDirection.RightUp,
                SlashStartTime = 0.1f,
                RecoveryStartTime = 0.3f,
                ComboWindowStart = 0.3f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "Stand_Attack_01 4",
                StartPose = AttackPoseDirection.RightUp,
                EndPose = AttackPoseDirection.RightDown,
                SlashStartTime = 0.2f,
                RecoveryStartTime = 0.4f,
                ComboWindowStart = 0.4f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "LightAttack01",
                StartPose = AttackPoseDirection.RightDown,
                EndPose = AttackPoseDirection.LeftUp,
                SlashStartTime = 0.1f,
                RecoveryStartTime = 0.3f,
                ComboWindowStart = 0.3f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "LightAttack06",
                StartPose = AttackPoseDirection.LeftUp,
                EndPose = AttackPoseDirection.LeftDown,
                SlashStartTime = 0.2f,
                RecoveryStartTime = 0.4f,
                ComboWindowStart = 0.4f,
                ExitTime = 0.95f
            }
        };
    }
}
