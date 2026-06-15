using UnityEngine;
using System.Collections;

public class NoiseScaleAnimator : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private float minValue = 0.02f;
    [SerializeField] private float maxValue = 0.1f;
    [SerializeField] private float minChangeTime = 0.5f;
    [SerializeField] private float maxChangeTime = 2f;

    private static readonly int NoiseScaleID = Shader.PropertyToID("_Noise_Scale");
    private MaterialPropertyBlock propBlock;
    private Coroutine animCoroutine;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        animCoroutine = StartCoroutine(AnimateNoiseScale());
    }

    private void OnDisable()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
    }

    private IEnumerator AnimateNoiseScale()
    {
        float current = Random.Range(minValue, maxValue);
        SetNoiseScale(current);

        while (true)
        {
            float target = Random.Range(minValue, maxValue);
            float duration = Random.Range(minChangeTime, maxChangeTime);
            float t = 0f;
            float start = current;

            while (t < duration)
            {
                t += Time.deltaTime;
                current = Mathf.Lerp(start, target, t / duration);
                SetNoiseScale(current);
                yield return null;
            }

            current = target;
            SetNoiseScale(current);
        }
    }

    private void SetNoiseScale(float value)
    {
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(propBlock, materialIndex);
        propBlock.SetFloat(NoiseScaleID, value);
        targetRenderer.SetPropertyBlock(propBlock, materialIndex);
    }
}
