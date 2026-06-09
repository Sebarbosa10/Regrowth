using System.Collections;
using UnityEngine;

/// <summary>
/// Se activa cuando el jugador agarra el arma por primera vez (Beat 1).
/// Llama a NarrativeBeatManager.OnFirstGrab() y anima dissolve en dos grupos:
///   - disappearRoots: sus Renderers van de 0 → 1 (desaparecen)
///   - appearRoots:    sus Renderers van de 1 → 0 (aparecen)
/// Ponelo en cualquier GO de la escena y conectalo al holster o llamalo manual.
/// </summary>
public class FirstGrabDissolveEvent : MonoBehaviour
{
    [Header("Grupos de meshes")]
    [Tooltip("Raices cuyos Renderers van de 0 a 1 (desaparecen)")]
    [SerializeField] private GameObject[] disappearRoots;

    [Tooltip("Raices cuyos Renderers van de 1 a 0 (aparecen)")]
    [SerializeField] private GameObject[] appearRoots;

    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 1.2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private MaterialPropertyBlock propBlock;
    private bool triggered = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Llamar desde NarrativeBeatManager.OnFirstGrab() o desde el WeaponHolster.
    /// Solo ejecuta una vez.
    /// </summary>
    public void Trigger()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RunDissolves());
    }

    private IEnumerator RunDissolves()
    {
        // Cachear todos los renderers de cada grupo una sola vez
        Renderer[] toDisappear = CollectRenderers(disappearRoots);
        Renderer[] toAppear = CollectRenderers(appearRoots);

        float t = 0f;

        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / dissolveDuration);
            float curve = dissolveCurve.Evaluate(progress);

            // disappear: 0 → 1
            ApplyDissolve(toDisappear, curve);

            // appear: 1 → 0
            ApplyDissolve(toAppear, 1f - curve);

            yield return null;
        }

        // Valores finales exactos
        ApplyDissolve(toDisappear, 1f);
        ApplyDissolve(toAppear, 0f);
    }

    private Renderer[] CollectRenderers(GameObject[] roots)
    {
        if (roots == null || roots.Length == 0) return System.Array.Empty<Renderer>();

        var list = new System.Collections.Generic.List<Renderer>();
        foreach (GameObject root in roots)
        {
            if (root == null) continue;
            list.AddRange(root.GetComponentsInChildren<Renderer>());
        }
        return list.ToArray();
    }

    private void ApplyDissolve(Renderer[] renderers, float value)
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(DissolveID, value);
            r.SetPropertyBlock(propBlock);
        }
    }
}