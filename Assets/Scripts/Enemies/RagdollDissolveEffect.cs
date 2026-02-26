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

    public void Init(float settleDelay, float dissolveDuration, Shader dissolveShader)
    {
        this.settleDelay = settleDelay;
        this.dissolveDuration = dissolveDuration;
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

        // Ease-in: starts slow, accelerates.
        float dissolveValue = t * t;

        for (int i = 0; i < dissolveMaterials.Length; i++)
        {
            if (dissolveMaterials[i] != null)
            {
                dissolveMaterials[i].SetFloat(DissolveAmountID, dissolveValue);
            }
        }

        if (t >= 1f)
        {
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

        // Count mesh renderers (skip particle systems) and their materials.
        int meshRendererCount = 0;
        int totalMats = 0;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] is ParticleSystemRenderer) continue;
            meshRendererCount++;
            totalMats += allRenderers[i].sharedMaterials.Length;
        }

        dissolveMaterials = new Material[totalMats];
        int matIndex = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] is ParticleSystemRenderer) continue;

            Material[] mats = allRenderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                // Clone the original material to preserve all properties (color, textures, metallic, etc.),
                // then swap only the shader to add dissolve support.
                Material dissolveMat = new Material(mats[j]);
                dissolveMat.shader = dissolveShader;
                dissolveMat.SetFloat(DissolveAmountID, 0f);

                // Destroy the intermediate instance created by accessing .materials
                Destroy(mats[j]);

                mats[j] = dissolveMat;
                dissolveMaterials[matIndex++] = dissolveMat;
            }
            allRenderers[i].materials = mats;
        }
    }
}
