namespace Player.StateMachine
{
    using System;

    public enum AttackPoseDirection
    {
        None,
        LeftUp,
        RightUp,
        LeftDown,
        RightDown
    }

    public enum AttackPhase
    {
        Windup,
        Slash,
        Recovery
    }

    [Serializable]
    public struct AttackStep
    {
        public string AnimationStateName;
        public AttackPoseDirection EndPose;
        public float Damage;
        public float SlashStartTime;
        public float RecoveryStartTime;
        public float ComboWindowStart;
        public float ExitTime;
    }

    public interface IAttackPhaseListener
    {
        void OnAttackPhase(AttackPhase phase);
    }
}
