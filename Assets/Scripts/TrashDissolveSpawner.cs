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

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
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
        if (dissolveSound != null)
            AudioSource.PlayClipAtPoint(dissolveSound, transform.position, dissolveSoundVolume);

        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            SetDissolve(1f - dissolveCurve.Evaluate(Mathf.Clamp01(t / dissolveDuration)));
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
            if (c != null) c.enabled = enabled;
    }
}
