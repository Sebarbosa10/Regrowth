using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PollutionVolumeController : MonoBehaviour
{
    [SerializeField] private Volume boxVolume;

    [System.Serializable]
    public class StageTint
    {
        [Tooltip("X,Y,Z = color, W = intensidad/peso")]
        public Vector4 shadows = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 midtones = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 highlights = new Vector4(1f, 1f, 1f, 0f);
    }

    [Header("Un tint por stage (0 = inicio, va contaminándose más)")]
    [SerializeField] private StageTint[] stageTints = new StageTint[3];

    private ShadowsMidtonesHighlights smh;

    private void Awake()
    {
        if (boxVolume != null && boxVolume.profile.TryGet(out smh))
        {
            // listo, smh referencia el override del profile
        }
        else
        {
            Debug.LogError("[PollutionVolume] No se encontró ShadowsMidtonesHighlights en el profile.");
        }
    }

    /// <summary>
    /// Interpola hacia el tint del stage indicado durante 'duration' segundos.
    /// </summary>
    public IEnumerator TransitionToStage(int stageIndex, float duration)
    {
        if (smh == null) yield break;
        if (stageIndex < 0 || stageIndex >= stageTints.Length) yield break;

        Vector4 startShadows = smh.shadows.value;
        Vector4 startMid = smh.midtones.value;
        Vector4 startHigh = smh.highlights.value;

        StageTint target = stageTints[stageIndex];

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            smh.shadows.value = Vector4.Lerp(startShadows, target.shadows, p);
            smh.midtones.value = Vector4.Lerp(startMid, target.midtones, p);
            smh.highlights.value = Vector4.Lerp(startHigh, target.highlights, p);

            yield return null;
        }

        smh.shadows.value = target.shadows;
        smh.midtones.value = target.midtones;
        smh.highlights.value = target.highlights;
    }
}