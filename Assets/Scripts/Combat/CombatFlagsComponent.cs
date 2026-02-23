using UnityEngine;

namespace Combat
{
    public class CombatFlagsComponent : MonoBehaviour
    {
        [SerializeField] private bool isVulnerable = true;
        [SerializeField] private bool isAttacking;
        [SerializeField] private bool isBlocking;

        private float parryWindowUntilTime = -1f;

        public bool IsVulnerable
        {
            get => isVulnerable;
            set => isVulnerable = value;
        }

        public bool IsAttacking
        {
            get => isAttacking;
            set => isAttacking = value;
        }

        public bool IsBlocking
        {
            get => isBlocking;
            set => isBlocking = value;
        }

        public bool IsParryWindowActive => Time.time <= parryWindowUntilTime;

        public void OpenParryWindow(float durationSeconds)
        {
            float duration = Mathf.Max(0f, durationSeconds);
            parryWindowUntilTime = Time.time + duration;
        }

        public void CloseParryWindow()
        {
            parryWindowUntilTime = -1f;
        }
    }
}
