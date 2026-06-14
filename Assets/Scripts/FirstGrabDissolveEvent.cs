using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstGrabDissolveEvent : MonoBehaviour
{
    [Header("Grupos de meshes")]
    [Tooltip("Sus Renderers van de 0 a 1 (desaparecen) en el primer trigger")]
    [SerializeField] private GameObject[] disappearRoots;
    [Tooltip("Sus Renderers van de 1 a 0 (aparecen) en el primer trigger")]
    [SerializeField] private GameObject[] appearRoots;

    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 1.2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio — Dissolve")]
    [SerializeField] private AudioClip dissolveSound;
    [SerializeField] private float dissolveSoundVolume = 1f;

    [Header("Audio — Fade In")]
    [Tooltip("El AudioSource cuyo volumen sube de 0 al target en el primer trigger")]
    [SerializeField] private AudioSource fadeInAudioSource;
    [SerializeField] private float fadeInTargetVolume = 1f;
    [SerializeField] private float fadeInDuration = 1.2f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private MaterialPropertyBlock propBlock;

    // false = estado inicial (disappearRoots visibles, appearRoots ocultos)
    // true  = estado invertido (disappearRoots ocultos, appearRoots visibles)
    private bool toggled = false;

    private bool isRunning = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (fadeInAudioSource != null)
            fadeInAudioSource.volume = 0f;
    }

    
    public void Trigger()
    {
        if (isRunning) return; // evita solapar mientras está corriendo

        bool goingToToggled = !toggled;
        toggled = goingToToggled;

        StartCoroutine(RunDissolves(goingToToggled));
    }

    private IEnumerator RunDissolves(bool toToggledState)
    {
        isRunning = true;

        Renderer[] toDisappear = CollectRenderers(disappearRoots);
        Renderer[] toAppear = CollectRenderers(appearRoots);

        if (dissolveSound != null)
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);

        if (fadeInAudioSource != null)
            StartCoroutine(FadeAudio(toToggledState));

        // Valores iniciales y finales según dirección
        float disappearFrom = toToggledState ? 0f : 2f;
        float disappearTo = toToggledState ? 2f : 0f;

        float appearFrom = toToggledState ? 2f : 0f;
        float appearTo = toToggledState ? 0f : 2f;

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float curve = dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration));

            ApplyDissolve(toDisappear, Mathf.Lerp(disappearFrom, disappearTo, curve));
            ApplyDissolve(toAppear, Mathf.Lerp(appearFrom, appearTo, curve));

            yield return null;
        }

        ApplyDissolve(toDisappear, disappearTo);
        ApplyDissolve(toAppear, appearTo);

        isRunning = false;
    }

    private IEnumerator FadeAudio(bool toToggledState)
    {
        // toToggledState true -> sube volumen, false -> baja volumen
        float from = fadeInAudioSource.volume;
        float to = toToggledState ? fadeInTargetVolume : 0f;

        if (toToggledState && !fadeInAudioSource.isPlaying)
            fadeInAudioSource.Play();

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            fadeInAudioSource.volume = Mathf.Lerp(from, to, t / fadeInDuration);
            yield return null;
        }

        fadeInAudioSource.volume = to;

        if (!toToggledState)
            fadeInAudioSource.Stop();
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