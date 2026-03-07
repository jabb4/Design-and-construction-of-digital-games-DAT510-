using UnityEngine;

/// <summary>
/// Attach to a ragdoll instance to dissolve it away after a settle delay.
/// Swaps all renderer materials to a dissolve shader and animates the effect.
/// </summary>
public class RagdollDissolveEffect : MonoBehaviour
{
    private float settleDelay;
    private float dissolveDuration;
    private Shader dissolveShader;

    private float timer;
    private bool isDissolving;
    private Material[] dissolveMaterials;

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveSeedID = Shader.PropertyToID("_DissolveSeed");
    private static readonly int DissolveHeightMinID = Shader.PropertyToID("_DissolveHeightMin");
    private static readonly int DissolveHeightInvRangeID = Shader.PropertyToID("_DissolveHeightInvRange");

    public void Init(float settleDelay, float dissolveDuration, Shader dissolveShader)
    {
        this.settleDelay = settleDelay;
        this.dissolveDuration = Mathf.Max(0.01f, dissolveDuration);
        this.dissolveShader = dissolveShader;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!isDissolving)
        {
            if (timer >= settleDelay)
            {
                BeginDissolve();
            }
            return;
        }

        float elapsed = timer - settleDelay;
        float t = Mathf.Clamp01(elapsed / dissolveDuration);

        // Ease-in: starts slow, accelerates. Overshoot slightly past 1.0
        // so the shader threshold (which uses the raw value) fully clips everything.
        float dissolveValue = t * t * 1.02f;

        for (int i = 0; i < dissolveMaterials.Length; i++)
        {
            if (dissolveMaterials[i] != null)
            {
                dissolveMaterials[i].SetFloat(DissolveAmountID, dissolveValue);
            }
        }


        if (t >= 1f)
        {
            EnemyDeathHandler.StopThreadedComponents(gameObject);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (dissolveMaterials == null) return;

        for (int i = 0; i < dissolveMaterials.Length; i++)
        {
            if (dissolveMaterials[i] != null)
            {
                Destroy(dissolveMaterials[i]);
            }
        }
    }

    private void BeginDissolve()
    {
        isDissolving = true;

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        // Count renderers (skip particle systems), materials, and world-space height range.
        int totalMats = 0;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] is ParticleSystemRenderer) continue;

            totalMats += allRenderers[i].sharedMaterials.Length;

            Bounds bounds = allRenderers[i].bounds;
            minY = Mathf.Min(minY, bounds.min.y);
            maxY = Mathf.Max(maxY, bounds.max.y);
        }

        float heightRange = Mathf.Max(0.01f, maxY - minY);

        dissolveMaterials = new Material[totalMats];
        int matIndex = 0;
        float dissolveSeed = Random.Range(0f, 1000f);
        float invHeightRange = 1f / heightRange;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] is ParticleSystemRenderer) continue;

            // Use sharedMaterials to avoid allocating throwaway material instances.
            Material[] sharedMats = allRenderers[i].sharedMaterials;
            Material[] newMats = new Material[sharedMats.Length];
            for (int j = 0; j < sharedMats.Length; j++)
            {
                if (sharedMats[j] == null)
                {
                    newMats[j] = null;
                    dissolveMaterials[matIndex++] = null;
                    continue;
                }

                Material dissolveMat = new Material(sharedMats[j]);
                dissolveMat.shader = dissolveShader;
                dissolveMat.SetFloat(DissolveAmountID, 0f);
                dissolveMat.SetFloat(DissolveSeedID, dissolveSeed);
                dissolveMat.SetFloat(DissolveHeightMinID, minY);
                dissolveMat.SetFloat(DissolveHeightInvRangeID, invHeightRange);

                newMats[j] = dissolveMat;
                dissolveMaterials[matIndex++] = dissolveMat;
            }
            allRenderers[i].materials = newMats;
        }

    }
}
