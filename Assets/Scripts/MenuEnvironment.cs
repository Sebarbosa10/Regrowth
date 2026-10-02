using UnityEngine;
using UnityEngine.Rendering;

// Entorno del menu principal: genera por codigo el mar (malla en rejilla) y el cielo (cubo)
// con los shaders Regrowth/MenuWater y Regrowth/MenuSky. El nivel del agua es la altura de este objeto.
public class MenuEnvironment : MonoBehaviour
{
    [Header("Shaders")]
    [SerializeField] private Shader waterShader;
    [SerializeField] private Shader skyShader;

    [Header("Mar")]
    [Tooltip("Lado del plano de agua en metros. Mas alla del far clip de la camara no se ve")]
    [SerializeField] private float waterSize = 90f;
    [Tooltip("Divisiones por lado. Mas divisiones = olas mas suaves y mas vertices")]
    [SerializeField, Range(8, 150)] private int waterResolution = 80;
    [SerializeField] private Color deepColor = new Color(0.01f, 0.03f, 0.07f, 1f);
    [SerializeField] private Color glintColor = new Color(0.7f, 0.9f, 1f, 1f);
    [SerializeField] private float waveHeight = 0.12f;
    [SerializeField] private float waveLength = 9f;
    [SerializeField] private float waveSpeed = 0.6f;

    [Header("Cielo")]
    [Tooltip("Altura del centro del cielo sobre el agua (altura de los ojos del jugador)")]
    [SerializeField] private float skyCenterHeight = 3f;
    [Tooltip("Mitad del lado del cubo del cielo. Sus esquinas deben quedar dentro del far clip")]
    [SerializeField] private float skyHalfSize = 20f;
    [SerializeField] private Color zenithColor = new Color(0.005f, 0.01f, 0.03f, 1f);
    [Tooltip("Color compartido: horizonte del cielo y agua a lo lejos, para que no se vea el corte")]
    [SerializeField] private Color horizonColor = new Color(0.05f, 0.10f, 0.21f, 1f);
    [SerializeField] private Color starColor = new Color(0.85f, 0.92f, 1f, 1f);
    [SerializeField] private Color moonColor = new Color(0.9f, 0.95f, 1f, 1f);
    [Tooltip("Hacia donde esta la luna (tambien orienta el brillo sobre el agua)")]
    [SerializeField] private Vector3 moonDirection = new Vector3(0.2f, 0.35f, 1f);

    private static readonly int DeepColorID = Shader.PropertyToID("_DeepColor");
    private static readonly int HorizonColorID = Shader.PropertyToID("_HorizonColor");
    private static readonly int GlintColorID = Shader.PropertyToID("_GlintColor");
    private static readonly int WaveHeightID = Shader.PropertyToID("_WaveHeight");
    private static readonly int WaveLengthID = Shader.PropertyToID("_WaveLength");
    private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
    private static readonly int ZenithColorID = Shader.PropertyToID("_ZenithColor");
    private static readonly int StarColorID = Shader.PropertyToID("_StarColor");
    private static readonly int MoonColorID = Shader.PropertyToID("_MoonColor");
    private static readonly int MoonDirID = Shader.PropertyToID("_MoonDir");

    private Mesh waterMesh;
    private Mesh skyMesh;
    private Material waterMaterial;
    private Material skyMaterial;

    private void Reset()
    {
        waterShader = Shader.Find("Regrowth/MenuWater");
        skyShader = Shader.Find("Regrowth/MenuSky");
    }

    private void Awake()
    {
        if (waterShader == null || skyShader == null)
        {
            Debug.LogError("[MenuEnvironment] Faltan los shaders Regrowth/MenuWater o Regrowth/MenuSky.");
            return;
        }

        Vector4 moonDir = moonDirection.sqrMagnitude > 0.0001f ? moonDirection.normalized : Vector3.up;

        waterMaterial = new Material(waterShader);
        waterMaterial.SetColor(DeepColorID, deepColor);
        waterMaterial.SetColor(HorizonColorID, horizonColor);
        waterMaterial.SetColor(GlintColorID, glintColor);
        waterMaterial.SetFloat(WaveHeightID, waveHeight);
        waterMaterial.SetFloat(WaveLengthID, waveLength);
        waterMaterial.SetFloat(WaveSpeedID, waveSpeed);
        waterMaterial.SetVector(MoonDirID, moonDir);

        skyMaterial = new Material(skyShader);
        skyMaterial.SetColor(ZenithColorID, zenithColor);
        skyMaterial.SetColor(HorizonColorID, horizonColor);
        skyMaterial.SetColor(StarColorID, starColor);
        skyMaterial.SetColor(MoonColorID, moonColor);
        skyMaterial.SetVector(MoonDirID, moonDir);

        waterMesh = BuildWaterMesh();
        skyMesh = BuildSkyMesh();

        CreateChild("MenuWater", Vector3.zero, waterMesh, waterMaterial);
        CreateChild("MenuSky", new Vector3(0f, skyCenterHeight, 0f), skyMesh, skyMaterial);
    }

    private void OnDestroy()
    {
        if (waterMesh != null) Destroy(waterMesh);
        if (skyMesh != null) Destroy(skyMesh);
        if (waterMaterial != null) Destroy(waterMaterial);
        if (skyMaterial != null) Destroy(skyMaterial);
    }

    private void CreateChild(string childName, Vector3 localPosition, Mesh mesh, Material material)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = localPosition;

        child.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private Mesh BuildWaterMesh()
    {
        int res = waterResolution;
        int side = res + 1;
        float half = waterSize * 0.5f;
        float step = waterSize / res;

        var vertices = new Vector3[side * side];
        for (int z = 0; z < side; z++)
        {
            for (int x = 0; x < side; x++)
                vertices[z * side + x] = new Vector3(-half + x * step, 0f, -half + z * step);
        }

        var triangles = new int[res * res * 6];
        int t = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = z * side + x;
                triangles[t++] = i;
                triangles[t++] = i + side;
                triangles[t++] = i + side + 1;
                triangles[t++] = i;
                triangles[t++] = i + side + 1;
                triangles[t++] = i + 1;
            }
        }

        var mesh = new Mesh { name = "MenuWater" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        // Margen vertical para que las olas (movidas en el shader) no hagan que se descarte la malla
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(waterSize, 4f, waterSize));
        return mesh;
    }

    private Mesh BuildSkyMesh()
    {
        float s = skyHalfSize;
        var vertices = new Vector3[]
        {
            new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
            new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, s, s), new Vector3(-s, s, s),
        };

        // El shader no hace culling, asi que el sentido de los triangulos da igual
        var triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            3, 6, 2, 3, 7, 6,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5,
        };

        var mesh = new Mesh { name = "MenuSky" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(waterSize, 0f, waterSize));
    }
}
