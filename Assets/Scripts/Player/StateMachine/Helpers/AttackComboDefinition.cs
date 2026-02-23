namespace Player.StateMachine
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject container for attack combo data.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackCombo", menuName = "Combat/Attack Combo")]
    public class AttackComboAsset : ScriptableObject
    {
        [SerializeField] private AttackStep[] attacks;
        private bool hasLoggedMissingAttacks;

        public int Count => attacks?.Length ?? 0;

        public AttackStep[] Steps => attacks;

        public bool TryGetStep(int index, out AttackStep step)
        {
            if (attacks == null || attacks.Length == 0)
            {
                if (!hasLoggedMissingAttacks)
                {
                    hasLoggedMissingAttacks = true;
                    Debug.LogWarning($"[AttackComboAsset] '{name}' has no attack steps configured.", this);
                }
                step = default;
                return false;
            }

            if (index < 0 || index >= attacks.Length)
            {
                step = default;
                return false;
            }

            step = attacks[index];
            return true;
        }
    }
}
