using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Renders a full-blade-width slash trail by sampling the world positions of
    /// two transforms (BladeRoot near the guard, BladeTip at the end) each frame
    /// and building a fading quad-strip mesh from them.
    ///
    /// The trail is driven entirely by animation events via ICombatAttackFeedbackHook:
    ///   Windup   → clear immediately
    ///   Slash    → start recording
    ///   Recovery → stop recording, fade out over fadeOutDuration seconds
    ///
    /// This means the trail length automatically matches each individual attack clip.
    /// Add to the Player or Enemy root — discovered automatically via
    /// GetComponents&lt;ICombatAttackFeedbackHook&gt;().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwordSlashTrail : MonoBehaviour, ICombatAttackFeedbackHook
    {
        [Header("Blade References")]
        [SerializeField] private Transform bladeRoot;  // near the guard
        [SerializeField] private Transform bladeTip;   // at the blade end

        [Header("Trail Settings")]
        [SerializeField] private Material  trailMaterial;
        [SerializeField, Range(8, 64)]  private int   maxSamples      = 40;
        [SerializeField, Min(0f)]       private float fadeOutDuration = 0.2f;   // how fast trail dissolves on Recovery

        [Header("Opacity")]
        [SerializeField, Range(0f, 1f)] private float peakAlpha         = 0.15f;
        [SerializeField, Min(0.1f)]     private float fadeCurve         = 4f;   // higher = tail disappears faster
        [SerializeField, Range(0f, 1f)] private float rootAlphaFraction = 0f;   // guard end relative to tip

        // -----------------------------------------------------------------------

        private struct Sample
        {
            public float   Time;
            public Vector3 Root;
            public Vector3 Tip;
        }

        private readonly List<Sample> samples = new List<Sample>(64);
        private Mesh  mesh;
        private bool  emitting;

        private bool  fadingOut;
        private float fadeStartTime;

        // Pre-allocated mesh arrays — resized only when sample count changes
        private Vector3[] verts;
        private Vector2[] uvs;
        private Color[]   cols;
        private int[]     tris;
        private int       lastSampleCount;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            mesh = new Mesh { name = "SwordTrail" };
            mesh.MarkDynamic();
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        // -----------------------------------------------------------------------
        // ICombatAttackFeedbackHook
        // -----------------------------------------------------------------------

        public void OnCombatAttackPhase(CombatAttackFeedbackContext context)
        {
            switch (context.Phase)
            {
                case CombatAttackPhase.Windup:
                    emitting    = false;
                    fadingOut   = false;
                    samples.Clear();
                    mesh.Clear();
                    break;

                case CombatAttackPhase.Slash:
                    fadingOut = false;
                    samples.Clear();
                    emitting  = true;
                    break;

                case CombatAttackPhase.Recovery:
                    emitting      = false;
                    fadingOut     = samples.Count > 0;
                    fadeStartTime = Time.time;
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // Trail update
        // -----------------------------------------------------------------------

        private void LateUpdate()
        {
            if (bladeRoot == null || bladeTip == null || trailMaterial == null) return;

            float now = Time.time;

            if (emitting)
            {
                samples.Add(new Sample
                {
                    Time = now,
                    Root = bladeRoot.position,
                    Tip  = bladeTip.position,
                });

                if (samples.Count > maxSamples)
                    samples.RemoveAt(0);
            }

            if (fadingOut && now - fadeStartTime >= fadeOutDuration)
            {
                fadingOut = false;
                samples.Clear();
            }

            if (samples.Count < 2)
            {
                mesh.Clear();
                return;
            }

            // Compute a 1→0 fade multiplier: 1 while slashing, ramps to 0 during Recovery
            float fadeMultiplier = 1f;
            if (fadingOut && fadeOutDuration > 0f)
                fadeMultiplier = 1f - Mathf.Clamp01((now - fadeStartTime) / fadeOutDuration);

            RebuildMesh(now, fadeMultiplier);
            Graphics.DrawMesh(mesh, Matrix4x4.identity, trailMaterial, gameObject.layer);
        }

        // -----------------------------------------------------------------------
        // Mesh construction
        // -----------------------------------------------------------------------

        private void RebuildMesh(float now, float fadeMultiplier)
        {
            int n = samples.Count;

            if (n != lastSampleCount)
            {
                verts           = new Vector3[n * 2];
                uvs             = new Vector2[n * 2];
                cols            = new Color[n * 2];
                tris            = new int[(n - 1) * 12]; // double-sided: 4 tris per segment
                lastSampleCount = n;
            }

            float oldest = samples[0].Time;
            float newest = samples[n - 1].Time;
            float span   = Mathf.Max(newest - oldest, 0.0001f);

            var boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < n; i++)
            {
                Sample s = samples[i];

                // t = 0 → oldest (tail), t = 1 → newest (leading edge)
                float t         = (s.Time - oldest) / span;
                float baseAlpha = Mathf.Pow(t, fadeCurve) * peakAlpha * fadeMultiplier;

                float rootAlpha = baseAlpha * rootAlphaFraction;
                float tipAlpha  = baseAlpha;

                float warm  = Mathf.Lerp(0.85f, 1.00f, t);
                var   cRoot = new Color(warm, warm * 0.97f + 0.02f, 1.00f, rootAlpha);
                var   cTip  = new Color(warm, warm * 0.97f + 0.02f, 1.00f, tipAlpha);

                verts[i * 2]     = s.Root;
                verts[i * 2 + 1] = s.Tip;
                uvs[i * 2]       = new Vector2(0f, t);
                uvs[i * 2 + 1]   = new Vector2(1f, t);
                cols[i * 2]      = cRoot;
                cols[i * 2 + 1]  = cTip;

                boundsMin = Vector3.Min(boundsMin, s.Root);
                boundsMin = Vector3.Min(boundsMin, s.Tip);
                boundsMax = Vector3.Max(boundsMax, s.Root);
                boundsMax = Vector3.Max(boundsMax, s.Tip);
            }

            for (int i = 0; i < n - 1; i++)
            {
                int b = i * 2;
                int o = i * 12;

                // Front face (CCW)
                tris[o]     = b;
                tris[o + 1] = b + 1;
                tris[o + 2] = b + 2;

                tris[o + 3] = b + 1;
                tris[o + 4] = b + 3;
                tris[o + 5] = b + 2;

                // Back face (CW)
                tris[o + 6]  = b + 2;
                tris[o + 7]  = b + 1;
                tris[o + 8]  = b;

                tris[o + 9]  = b + 2;
                tris[o + 10] = b + 3;
                tris[o + 11] = b + 1;
            }

            mesh.Clear(keepVertexLayout: false);
            mesh.vertices  = verts;
            mesh.uv        = uvs;
            mesh.colors    = cols;
            mesh.triangles = tris;
            mesh.bounds    = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin);
        }
    }
}
