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
    }

    public interface IAttackPhaseListener
    {
        void OnAttackPhase(AttackPhase phase);
    }
}
