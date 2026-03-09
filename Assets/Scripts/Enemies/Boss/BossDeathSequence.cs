using Combat;
using UnityEngine;

namespace Enemies.Boss
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossDeathSequence : MonoBehaviour
    {
        [Header("Kill Overlay")]
        [SerializeField] private Texture killOverlayTexture;
        [SerializeField] private AudioClip killOverlaySlamSound;
        [SerializeField, Min(0f)] private float overlayDelay = 0.5f;

        [Header("Screen Fade")]
        [SerializeField, Min(0f)] private float fadeDelay = 3f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 2f;
        [SerializeField] private string targetScene = "VanView";
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 1f;

        private HealthComponent health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDied += HandleBossDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied -= HandleBossDied;
            }
        }

        private void HandleBossDied()
        {
            if (killOverlayTexture != null)
            {
                DeathOverlay.Create(
                    killOverlayTexture,
                    overlayDelay,
                    killOverlaySlamSound,
                    peakIntensity: 15f,
                    bloomColor: Color.white
                );
            }

            ScreenFade.Create(fadeDelay, fadeDuration, targetScene, fadeOutDuration);
        }
    }
}
