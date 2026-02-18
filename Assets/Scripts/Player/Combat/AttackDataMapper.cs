using Combat;
using Player.StateMachine;

namespace Player.Combat
{
    public static class AttackDataMapper
    {
        public static AttackData ToAttackData(AttackStep step)
        {
            return new AttackData
            {
                AttackId = step.AnimationStateName,
                Damage = step.Damage,
                DirectionHint = AttackComboDirectionResolver.MapFromPoseDirection(step.EndPose)
            };
        }
    }
}
