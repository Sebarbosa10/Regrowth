using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneja transiciones de dissolve para cambios de stage y el first grab.
/// TriggerDissolve() — todo desaparece (fin de stage)
/// TriggerAppear()  — todo aparece   (inicio de stage)
/// Ambos son coroutines awaitables desde StageManager.
/// </summary>
public class SceneDissolveEvent : MonoBehaviour
{
    [Header("Grupos de meshes")]
    [Tooltip("Renderers que desaparecen en Dissolve y aparecen en Appear")]
    [SerializeField] private GameObject[] dissolveRoots;

    [Tooltip("Renderers que aparecen en Dissolve y desaparecen en Appear (inverso)")]
    [SerializeField] private GameObject[] inverseRoots;

    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 1.2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio — Dissolve")]
    [SerializeField] private AudioClip dissolveSound;
    [SerializeField] private float dissolveSoundVolume = 1f;

    [Header("Audio — Fade In (solo en Appear)")]
    [SerializeField] private AudioSource fadeInAudioSource;
    [SerializeField] private float fadeInTargetVolume = 1f;
    [SerializeField] private float fadeInDuration = 1.2f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private MaterialPropertyBlock propBlock;

    // First grab — se dispara solo una vez
    private bool firstGrabTriggered = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (fadeInAudioSource != null)
            fadeInAudioSource.volume = 0f;
    }

    // ─────────────────────────────────────────
    //  PUBLIC — llamados desde StageManager
    // ─────────────────────────────────────────

    /// <summary>Todo desaparece — llamar al terminar un stage.</summary>
    public IEnumerator TriggerDissolve()
    {
        Renderer[] main = CollectRenderers(dissolveRoots);
        Renderer[] inverse = CollectRenderers(inverseRoots);

        if (dissolveSound != null)
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float curve = dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration));
            ApplyDissolve(main, curve);        // 0 → 1 (desaparece)
            ApplyDissolve(inverse, 1f - curve);   // 1 → 0 (aparece el inverso)
            yield return null;
        }
        ApplyDissolve(main, 1f);
        ApplyDissolve(inverse, 0f);
    }

    /// <summary>Todo aparece — llamar al iniciar un stage.</summary>
    public IEnumerator TriggerAppear()
    {
        Renderer[] main = CollectRenderers(dissolveRoots);
        Renderer[] inverse = CollectRenderers(inverseRoots);

        // Asegurarse de empezar desde el estado disuelto
        ApplyDissolve(main, 1f);
        ApplyDissolve(inverse, 0f);

        if (fadeInAudioSource != null)
            StartCoroutine(FadeInAudio());

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float curve = dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration));
            ApplyDissolve(main, 1f - curve);   // 1 → 0 (aparece)
            ApplyDissolve(inverse, curve);         // 0 → 1 (desaparece el inverso)
            yield return null;
        }
        ApplyDissolve(main, 0f);
        ApplyDissolve(inverse, 1f);
    }

    /// <summary>
    /// First grab — solo se ejecuta una vez, mantiene el comportamiento original.
    /// </summary>
    public void TriggerFirstGrab()
    {
        if (firstGrabTriggered) return;
        firstGrabTriggered = true;
        StartCoroutine(TriggerAppear());
    }

    // ─────────────────────────────────────────
    //  INTERNALS
    // ─────────────────────────────────────────

    private IEnumerator FadeInAudio()
    {
        fadeInAudioSource.volume = 0f;
        if (!fadeInAudioSource.isPlaying)
            fadeInAudioSource.Play();

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            fadeInAudioSource.volume = Mathf.Lerp(0f, fadeInTargetVolume, t / fadeInDuration);
            yield return null;
        }
        fadeInAudioSource.volume = fadeInTargetVolume;
    }

    private Renderer[] CollectRenderers(GameObject[] roots)
    {
        if (roots == null || roots.Length == 0) return System.Array.Empty<Renderer>();
        var list = new List<Renderer>();
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