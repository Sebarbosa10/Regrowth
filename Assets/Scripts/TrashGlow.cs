using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Halo suave detras de cada basura, del color de su material (el mismo que el modo del arma que la destruye).
// Crea un quad hijo con el shader Regrowth/TrashGlow, que lo orienta solo hacia la camara.
// No tiene collider, asi que no interfiere con el hitscan.
public class TrashGlow : MonoBehaviour
{
    [SerializeField] private Shader glowShader;
    [Tooltip("Tamano del halo respecto al tamano de la basura")]
    [SerializeField] private float sizeMultiplier = 1.6f;
    [SerializeField, Range(0f, 2f)] private float intensity = 0.45f;
    [Tooltip("Tiempo en aparecer (acompana al dissolve de aparicion)")]
    [SerializeField] private float fadeInTime = 1f;

    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    // Un material por color, compartido entre todas las basuras del mismo tipo (y entre rondas)
    private static readonly Dictionary<Color, Material> sharedMaterials = new Dictionary<Color, Material>();
    private static Mesh quadMesh;

    private void Reset()
    {
        glowShader = Shader.Find("Regrowth/TrashGlow");
    }

    // En Start y no en Awake: TrashDissolveSpawner recoge los renderers en su Awake y este halo
    // no debe recibir el MaterialPropertyBlock del dissolve.
    private void Start()
    {
        Renderer source = GetComponentInChildren<Renderer>();
        if (glowShader == null || source == null) return;

        Material sourceMaterial = source.sharedMaterial;
        Color color = sourceMaterial != null && sourceMaterial.HasProperty(ColorID)
            ? sourceMaterial.GetColor(ColorID)
            : Color.white;

        Bounds bounds = source.bounds;
        float worldSize = bounds.extents.magnitude * 2f * sizeMultiplier;

        var glow = new GameObject("Glow");
        glow.transform.SetParent(transform, false);
        glow.transform.position = bounds.center;
        Vector3 targetScale = Vector3.one * (worldSize / Mathf.Max(transform.lossyScale.x, 0.0001f));
        glow.transform.localScale = fadeInTime > 0f ? Vector3.zero : targetScale;

        glow.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
        MeshRenderer glowRenderer = glow.AddComponent<MeshRenderer>();
        glowRenderer.sharedMaterial = GetMaterial(color * intensity);
        glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;
        glowRenderer.lightProbeUsage = LightProbeUsage.Off;
        glowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        if (fadeInTime > 0f)
            StartCoroutine(GrowIn(glow.transform, targetScale));
    }

    private IEnumerator GrowIn(Transform glow, Vector3 targetScale)
    {
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            glow.localScale = targetScale * Mathf.SmoothStep(0f, 1f, t / fadeInTime);
            yield return null;
        }
        glow.localScale = targetScale;
    }

    private Material GetMaterial(Color color)
    {
        color.a = 1f;
        if (sharedMaterials.TryGetValue(color, out Material material) && material != null)
            return material;

        material = new Material(glowShader) { name = "TrashGlow (runtime)" };
        material.SetColor(GlowColorID, color);
        sharedMaterials[color] = material;
        return material;
    }

    private static Mesh GetQuadMesh()
    {
        if (quadMesh != null) return quadMesh;

        quadMesh = new Mesh { name = "TrashGlowQuad" };
        quadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
        };
        quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        // El shader gira el quad hacia la camara: bounds cubicos para que nunca se descarte por error
        quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        return quadMesh;
    }
}
