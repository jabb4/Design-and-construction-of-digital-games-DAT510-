namespace Enemies.StateMachine
{
    using Enemies.AI;
    using global::StateMachine.Core;
    using UnityEngine;

    public abstract class EnemyStateBase : IState
    {
        protected EnemyStateMachine Owner { get; private set; }
        protected Enemy Enemy => Owner != null ? Owner.Enemy : null;
        protected Animator Animator => Owner != null ? Owner.Animator : null;
        protected EnemyIntentSource Intent => Owner != null ? Owner.IntentSource : null;

        public virtual string StateName => GetType().Name;

        public void Initialize(EnemyStateMachine owner)
        {
            Owner = owner;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnExit()
        {
        }

        public abstract TransitionDecision EvaluateTransition();

        protected void FaceTarget(float turnSpeedDegrees = 540f)
        {
            if (Owner == null || Owner.CurrentTarget == null)
            {
                return;
            }

            Vector3 toTarget = Owner.CurrentTarget.position - Owner.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toTarget);
            Owner.transform.rotation = Quaternion.RotateTowards(
                Owner.transform.rotation,
                targetRotation,
                turnSpeedDegrees * Time.deltaTime);
        }
    }
}
