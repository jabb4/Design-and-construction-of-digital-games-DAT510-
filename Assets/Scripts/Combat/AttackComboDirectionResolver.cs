using System;
using System.Collections.Generic;
using Player.StateMachine;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Resolves attack direction hints from one or more combo assets by attack animation id.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackComboDirectionResolver : MonoBehaviour
    {
        [SerializeField] private AttackComboAsset[] comboSources;

        private readonly Dictionary<string, AttackDirectionHint> directionByAttackId =
            new Dictionary<string, AttackDirectionHint>(StringComparer.Ordinal);

        private void Awake()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public bool TryResolve(string attackId, out AttackDirectionHint directionHint)
        {
            if (string.IsNullOrWhiteSpace(attackId))
            {
                directionHint = AttackDirectionHint.None;
                return false;
            }

            if (directionByAttackId.Count == 0)
            {
                RebuildLookup();
            }

            return directionByAttackId.TryGetValue(attackId, out directionHint);
        }

        public void SetComboSources(IReadOnlyList<AttackComboAsset> combos)
        {
            if (combos == null || combos.Count == 0)
            {
                comboSources = Array.Empty<AttackComboAsset>();
                RebuildLookup();
                return;
            }

            comboSources = new AttackComboAsset[combos.Count];
            for (int i = 0; i < combos.Count; i++)
            {
                comboSources[i] = combos[i];
            }

            RebuildLookup();
        }

        public static bool TryResolveFromCombo(AttackComboAsset combo, string attackId, out AttackDirectionHint hint)
        {
            hint = AttackDirectionHint.None;
            if (combo == null || string.IsNullOrWhiteSpace(attackId))
            {
                return false;
            }

            AttackStep[] steps = combo.Steps;
            if (steps == null || steps.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < steps.Length; i++)
            {
                AttackStep step = steps[i];
                if (!string.Equals(step.AnimationStateName, attackId, StringComparison.Ordinal))
                {
                    continue;
                }

                hint = MapFromPoseDirection(step.EndPose);
                return hint != AttackDirectionHint.None;
            }

            return false;
        }

        public static AttackDirectionHint MapFromPoseDirection(AttackPoseDirection direction)
        {
            switch (direction)
            {
                case AttackPoseDirection.LeftUp:
                    return AttackDirectionHint.LeftUp;
                case AttackPoseDirection.LeftDown:
                    return AttackDirectionHint.LeftDown;
                case AttackPoseDirection.RightUp:
                    return AttackDirectionHint.RightUp;
                case AttackPoseDirection.RightDown:
                    return AttackDirectionHint.RightDown;
                default:
                    return AttackDirectionHint.None;
            }
        }

        private void RebuildLookup()
        {
            directionByAttackId.Clear();
            if (comboSources == null)
            {
                return;
            }

            for (int comboIndex = 0; comboIndex < comboSources.Length; comboIndex++)
            {
                AttackComboAsset combo = comboSources[comboIndex];
                if (combo == null)
                {
                    continue;
                }

                AttackStep[] steps = combo.Steps;
                if (steps == null || steps.Length == 0)
                {
                    continue;
                }

                for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
                {
                    AttackStep step = steps[stepIndex];
                    if (string.IsNullOrWhiteSpace(step.AnimationStateName))
                    {
                        continue;
                    }

                    AttackDirectionHint hint = MapFromPoseDirection(step.EndPose);
                    if (hint == AttackDirectionHint.None)
                    {
                        continue;
                    }

                    directionByAttackId[step.AnimationStateName] = hint;
                }
            }
        }
    }
}
