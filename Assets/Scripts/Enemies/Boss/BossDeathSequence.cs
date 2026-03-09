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
        [Tooltip("How many seconds before the visual slam to start the sound (to sync the impact hit with the visual).")]
        [SerializeField, Min(0f)] private float slamSoundLeadTime = 0f;
        [SerializeField, Range(0f, 1f)] private float slamSoundVolume = 1f;

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
                    bloomColor: Color.white,
                    slamSoundLeadTime: slamSoundLeadTime,
                    slamSoundVolume: slamSoundVolume
                );
            }

            ScreenFade.Create(fadeDelay, fadeDuration, targetScene, fadeOutDuration);
        }
    }
}
