using UnityEngine;

namespace Enemies.Boss
{
    public sealed class BossPhaseVfxController : MonoBehaviour
    {
        [Header("Phase VFX")]
        [SerializeField] private GameObject phase2Vfx;
        [SerializeField] private GameObject phase3Vfx;

        [Header("Transition Burst")]
        [SerializeField] private Transform vfxOrigin;
        [SerializeField] private GameObject transitionBurstPrefab;

        public void SetPhase(int phase)
        {
            if (phase >= 2) Activate(phase2Vfx);
            if (phase >= 3) Activate(phase3Vfx);

            PlayTransitionBurst();
        }

        private static void Activate(GameObject go)
        {
            if (go == null) return;

            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!ps.isPlaying) ps.Play(true);
            }
        }

        private void PlayTransitionBurst()
        {
            if (transitionBurstPrefab == null) return;

            var origin = vfxOrigin != null ? vfxOrigin : transform;
            var go = Instantiate(transitionBurstPrefab, origin.position, Quaternion.identity);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(go, 3f);
            }
        }
    }
}
