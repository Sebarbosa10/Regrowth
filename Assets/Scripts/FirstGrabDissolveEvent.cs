using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstGrabDissolveEvent : MonoBehaviour
{
    [Header("Grupos de meshes")]
    [Tooltip("Sus Renderers van de 0 a 1 (desaparecen)")]
    [SerializeField] private GameObject[] disappearRoots;

    [Tooltip("Sus Renderers van de 1 a 0 (aparecen)")]
    [SerializeField] private GameObject[] appearRoots;

    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 1.2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio — Dissolve")]
    [SerializeField] private AudioClip dissolveSound;
    [SerializeField] private float dissolveSoundVolume = 1f;

    [Header("Audio — Fade In")]
    [Tooltip("El AudioSource cuyo volumen sube de 0 al target al disparar el evento")]
    [SerializeField] private AudioSource fadeInAudioSource;
    [SerializeField] private float fadeInTargetVolume = 1f;
    [SerializeField] private float fadeInDuration = 1.2f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private MaterialPropertyBlock propBlock;
    private bool triggered = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        // Asegurarse de que el AudioSource arranca en silencio
        if (fadeInAudioSource != null)
            fadeInAudioSource.volume = 0f;
    }

    public void Trigger()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RunDissolves());
    }

    private IEnumerator RunDissolves()
    {
        Renderer[] toDisappear = CollectRenderers(disappearRoots);
        Renderer[] toAppear = CollectRenderers(appearRoots);

        // Sonido del dissolve
        if (dissolveSound != null)
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);

        // Fade in del AudioSource en paralelo
        if (fadeInAudioSource != null)
            StartCoroutine(FadeInAudio());

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float curve = dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration));

            ApplyDissolve(toDisappear, curve);
            ApplyDissolve(toAppear, 1f - curve);

            yield return null;
        }

        ApplyDissolve(toDisappear, 1f);
        ApplyDissolve(toAppear, 0f);
    }

    private IEnumerator FadeInAudio()
    {
        fadeInAudioSource.volume = 0f;

        // Si no estaba reproduciendose, arrancarlo
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