using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstGrabDissolveEvent : MonoBehaviour
{
    [SerializeField] private GameObject[] disappearRoots;
    [SerializeField] private GameObject[] appearRoots;
    [Tooltip("Raices sin dissolve (material opaco). Se activan/desactivan junto con appearRoots")]
    [SerializeField] private GameObject[] toggleRoots;
    [Tooltip("Punto del dissolve (0-1) en el que se activan/desactivan las toggleRoots")]
    [SerializeField, Range(0f, 1f)] private float toggleRootsPoint = 0.5f;
    [SerializeField] private float dissolveDuration = 1.2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AudioClip dissolveSound;
    [SerializeField] private float dissolveSoundVolume = 1f;
    [SerializeField] private AudioSource fadeInAudioSource;
    [SerializeField] private float fadeInTargetVolume = 1f;
    [SerializeField] private float fadeInDuration = 1.2f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private MaterialPropertyBlock propBlock;
    private Renderer[] disappearRenderers;
    private Renderer[] appearRenderers;
    private bool toggled = false;
    private bool isRunning = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (fadeInAudioSource != null) fadeInAudioSource.volume = 0f;

        // Primero se apagan las toggleRoots: asi sus renderers no entran en la lista
        // de dissolve aunque esten colgadas de una appearRoot (no reciben MPB).
        SetRootsActive(toggleRoots, false);

        disappearRenderers = CollectRenderers(disappearRoots);
        appearRenderers = CollectRenderers(appearRoots);

        // Estado inicial: lo que aparece esta oculto, no hace falta dibujarlo.
        SetRenderersEnabled(appearRenderers, false);
    }

    public void Trigger()
    {
        if (isRunning) return;
        toggled = !toggled;
        StartCoroutine(RunDissolves(toggled));
    }

    private IEnumerator RunDissolves(bool toToggledState)
    {
        isRunning = true;

        if (dissolveSound != null)
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);

        if (fadeInAudioSource != null)
            StartCoroutine(FadeAudio(toToggledState));

        float disappearFrom = toToggledState ? 0f : 2f;
        float disappearTo   = toToggledState ? 2f : 0f;
        float appearFrom    = toToggledState ? 2f : 0f;
        float appearTo      = toToggledState ? 0f : 2f;

        // Los que vuelven a verse se encienden ya con su valor inicial para que no parpadeen.
        Renderer[] becomingVisible = toToggledState ? appearRenderers : disappearRenderers;
        Renderer[] becomingHidden = toToggledState ? disappearRenderers : appearRenderers;
        ApplyDissolve(becomingVisible, 2f);
        SetRenderersEnabled(becomingVisible, true);

        float toggleTime = dissolveDuration * toggleRootsPoint;
        bool rootsToggled = false;

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float curve = dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration));
            ApplyDissolve(disappearRenderers, Mathf.Lerp(disappearFrom, disappearTo, curve));
            ApplyDissolve(appearRenderers, Mathf.Lerp(appearFrom, appearTo, curve));

            if (!rootsToggled && t >= toggleTime)
            {
                SetRootsActive(toggleRoots, toToggledState);
                rootsToggled = true;
            }

            yield return null;
        }

        ApplyDissolve(disappearRenderers, disappearTo);
        ApplyDissolve(appearRenderers, appearTo);

        if (!rootsToggled) SetRootsActive(toggleRoots, toToggledState);

        // Lo que quedo totalmente disuelto deja de dibujarse.
        SetRenderersEnabled(becomingHidden, false);

        isRunning = false;
    }

    private IEnumerator FadeAudio(bool toToggledState)
    {
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
        if (!toToggledState) fadeInAudioSource.Stop();
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

    private void SetRenderersEnabled(Renderer[] renderers, bool state)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = state;
    }

    private void SetRootsActive(GameObject[] roots, bool state)
    {
        if (roots == null) return;
        foreach (GameObject root in roots)
            if (root != null) root.SetActive(state);
    }
}
