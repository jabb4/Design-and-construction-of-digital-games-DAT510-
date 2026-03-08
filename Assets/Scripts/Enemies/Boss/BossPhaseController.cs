using Combat;
using Enemies.AI;
using Enemies.StateMachine;
using UnityEngine;

namespace Enemies.Boss
{
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossPhaseController : MonoBehaviour
    {
        private const float MinPhaseGap = 0.01f;

        [Header("Phase Profiles")]
        [SerializeField] private EnemyCombatProfile phase1Profile;
        [SerializeField] private EnemyCombatProfile phase2Profile;
        [SerializeField] private EnemyCombatProfile phase3Profile;

        [Header("Thresholds (HP ratio)")]
        [SerializeField, Range(0f, 1f)] private float phase2Threshold = 0.6f;
        [SerializeField, Range(0f, 1f)] private float phase3Threshold = 0.3f;

        [Header("Transition SFX")]
        [SerializeField] private AudioClip phaseTransitionSound;

        private EnemyStateMachine stateMachine;
        private HealthComponent health;
        private BossPhaseVfxController aura;
        private AudioSource audioSource;
        private int currentPhase = 1;

        public int CurrentPhase => currentPhase;

        private void Awake()
        {
            NormalizeThresholds();
            stateMachine = GetComponent<EnemyStateMachine>();
            health = GetComponent<HealthComponent>();
            aura = GetComponent<BossPhaseVfxController>();
            audioSource = GetComponent<AudioSource>();
        }

        private void OnValidate()
        {
            NormalizeThresholds();
        }

        private void Start()
        {
            if (phase1Profile != null)
            {
                stateMachine.SetCombatProfile(phase1Profile);
            }

        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(float damage, float currentHp, float maxHp)
        {
            if (maxHp <= 0f || currentHp <= 0f)
            {
                return;
            }

            float ratio = currentHp / maxHp;
            int targetPhase = ratio <= phase3Threshold ? 3
                            : ratio <= phase2Threshold ? 2
                            : 1;

            if (targetPhase <= currentPhase) return;

            currentPhase = targetPhase;

            EnemyCombatProfile profile = currentPhase switch
            {
                2 => phase2Profile,
                3 => phase3Profile,
                _ => phase1Profile
            };

            if (profile != null)
            {
                stateMachine.SetCombatProfile(profile);
            }

            aura?.SetPhase(currentPhase);
            PlayTransitionSfx();
        }

        private void PlayTransitionSfx()
        {
            if (audioSource != null && phaseTransitionSound != null)
            {
                audioSource.PlayOneShot(phaseTransitionSound);
            }
        }

        private void NormalizeThresholds()
        {
            phase2Threshold = Mathf.Clamp01(phase2Threshold);
            phase3Threshold = Mathf.Clamp01(phase3Threshold);

            if (phase3Threshold >= phase2Threshold)
            {
                phase3Threshold = Mathf.Max(0f, phase2Threshold - MinPhaseGap);
            }
        }
    }
}
