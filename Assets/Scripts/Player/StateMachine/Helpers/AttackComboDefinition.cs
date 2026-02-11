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
                EndPose = AttackPoseDirection.LeftDown,
                Damage = 10f,
                SlashStartTime = 0.3f,
                RecoveryStartTime = 0.4f,
                ComboWindowStart = 0.4f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "Stand_Attack_03 2",
                EndPose = AttackPoseDirection.RightUp,
                Damage = 12f,
                SlashStartTime = 0.3f,
                RecoveryStartTime = 0.4f,
                ComboWindowStart = 0.4f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "Stand_Attack_01 4",
                EndPose = AttackPoseDirection.RightDown,
                Damage = 14f,
                SlashStartTime = 0.4f,
                RecoveryStartTime = 0.5f,
                ComboWindowStart = 0.5f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "LightAttack01",
                EndPose = AttackPoseDirection.LeftUp,
                Damage = 12f,
                SlashStartTime = 0.3f,
                RecoveryStartTime = 0.4f,
                ComboWindowStart = 0.4f,
                ExitTime = 0.95f
            },
            new() {
                AnimationStateName = "LightAttack06",
                EndPose = AttackPoseDirection.LeftDown,
                Damage = 15f,
                SlashStartTime = 0.4f,
                RecoveryStartTime = 0.5f,
                ComboWindowStart = 0.5f,
                ExitTime = 0.95f
            }
        };
    }
}
