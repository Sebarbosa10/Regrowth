using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TrashDissolveSpawner : MonoBehaviour
{
    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool disableColliderDuringDissolve = true;
    [SerializeField] private AudioClip dissolveSound;
    [SerializeField] private float dissolveSoundVolume = 1f;
    [Tooltip("Material opaco barato que sustituye al del dissolve cuando la basura termina de aparecer (lo crea Regrowth > Optimizar prefabs de basura)")]
    [SerializeField] private Material opaqueMaterial;
    [Tooltip("Separacion minima entre sonidos de aparicion: al spawnear muchas basuras a la vez solo suenan algunas")]
    [SerializeField] private float minSoundInterval = 0.15f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static float lastSoundTime = -999f;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Collider[] colliders;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colliders = GetComponentsInChildren<Collider>();

        SetDissolve(1f);

        if (disableColliderDuringDissolve)
            SetCollidersEnabled(false);

        StartCoroutine(DissolveIn());
    }

    private IEnumerator DissolveIn()
    {
        // En la ronda 3 aparecen 60 a la vez: sin limite serian 60 voces en el mismo frame
        if (dissolveSound != null && Time.time - lastSoundTime >= minSoundInterval)
        {
            lastSoundTime = Time.time;
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);
        }

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            SetDissolve(1f - dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration)));
            yield return null;
        }

        SetDissolve(0f);

        // El shader Dissolve (Lit + alpha clip + doble cara + ruido) solo hace falta mientras aparece
        if (opaqueMaterial != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
                r.sharedMaterial = opaqueMaterial;
            }
        }
        else
        {
            ClearDissolveBlock();
        }

        if (disableColliderDuringDissolve)
            SetCollidersEnabled(true);
    }

    // Un renderer con MaterialPropertyBlock queda fuera del SRP Batcher. Al terminar de aparecer se quita,
    // salvo que el material no este visible por defecto (_Dissolve distinto de 0), porque entonces desapareceria.
    private void ClearDissolveBlock()
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Material material = r.sharedMaterial;
            if (material != null && material.HasProperty(DissolveID) && material.GetFloat(DissolveID) > 0.001f) continue;
            r.SetPropertyBlock(null);
        }
    }

    private void SetDissolve(float value)
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(DissolveID, value);
            r.SetPropertyBlock(propBlock);
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider c in colliders)
            if (c != null) c.enabled = enabled;
    }
}
