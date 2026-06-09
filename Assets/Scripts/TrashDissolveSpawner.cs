using System.Collections;
using UnityEngine;

/// <summary>
/// Ponelo en cada prefab de basura.
/// Al hacer Awake, anima _Dissolve de 1 a 0 usando MaterialPropertyBlock (sin allocs).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TrashDissolveSpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Collider")]
    [Tooltip("Si está activado, el collider se desactiva durante el dissolve para que la basura no sea interactuable hasta que aparezca")]
    [SerializeField] private bool disableColliderDuringDissolve = true;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Collider[] colliders;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colliders = GetComponentsInChildren<Collider>();

        // Empezar completamente disuelto
        SetDissolve(1f);

        if (disableColliderDuringDissolve)
            SetCollidersEnabled(false);

        StartCoroutine(DissolveIn());
    }

    private IEnumerator DissolveIn()
    {
        float t = 0f;

        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / dissolveDuration);
            // Curva va de 0 a 1 (progreso de aparicion), dissolve va de 1 a 0
            float dissolveValue = 1f - dissolveCurve.Evaluate(progress);
            SetDissolve(dissolveValue);
            yield return null;
        }

        SetDissolve(0f);

        if (disableColliderDuringDissolve)
            SetCollidersEnabled(true);
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
        {
            if (c != null) c.enabled = enabled;
        }
    }
}