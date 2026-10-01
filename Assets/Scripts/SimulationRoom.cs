using UnityEngine;
using UnityEngine.Rendering;

// Genera la sala de simulacion (un cubo visto desde dentro) al arrancar.
// El dissolve lo maneja FirstGrabDissolveEvent: este objeto va en sus disappearRoots.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimulationRoom : MonoBehaviour
{
    [Header("Sala")]
    [Tooltip("Ancho (X), alto (Y) y fondo (Z) en metros. El suelo queda a la altura de este objeto")]
    [SerializeField] private Vector3 roomSize = new Vector3(6f, 3.5f, 6f);
    [Tooltip("Sube el suelo de la sala para que no parpadee con otro suelo a la misma altura")]
    [SerializeField] private float floorOffset = 0.01f;

    [Header("Rejilla")]
    [SerializeField] private Shader gridShader;
    [SerializeField] private Color baseColor = new Color(0.01f, 0.02f, 0.05f, 1f);
    [SerializeField] private Color lineColor = new Color(0.1f, 0.9f, 1f, 1f);
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField, Range(0.001f, 0.2f)] private float lineWidth = 0.02f;
    [SerializeField] private Color dissolveEdgeColor = Color.white;
    [SerializeField, Range(0f, 0.3f)] private float dissolveEdgeWidth = 0.08f;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int LineColorID = Shader.PropertyToID("_LineColor");
    private static readonly int CellSizeID = Shader.PropertyToID("_CellSize");
    private static readonly int LineWidthID = Shader.PropertyToID("_LineWidth");
    private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");

    // Direccion hacia fuera de cada pared y sus dos ejes sobre el plano
    private static readonly Vector3[] faceOut = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
    private static readonly Vector3[] faceU = { Vector3.forward, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.right };
    private static readonly Vector3[] faceV = { Vector3.up, Vector3.up, Vector3.forward, Vector3.forward, Vector3.up, Vector3.up };

    private Mesh roomMesh;
    private Material roomMaterial;

    private void Reset()
    {
        gridShader = Shader.Find("Regrowth/SimulationGrid");
    }

    private void Awake()
    {
        if (gridShader == null)
        {
            Debug.LogError("[SimulationRoom] Falta asignar el shader Regrowth/SimulationGrid.");
            return;
        }

        roomMesh = BuildMesh();
        GetComponent<MeshFilter>().sharedMesh = roomMesh;

        roomMaterial = new Material(gridShader);
        roomMaterial.SetColor(BaseColorID, baseColor);
        roomMaterial.SetColor(LineColorID, lineColor);
        roomMaterial.SetFloat(CellSizeID, cellSize);
        roomMaterial.SetFloat(LineWidthID, lineWidth);
        roomMaterial.SetColor(EdgeColorID, dissolveEdgeColor);
        roomMaterial.SetFloat(EdgeWidthID, dissolveEdgeWidth);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = roomMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void OnDestroy()
    {
        if (roomMesh != null) Destroy(roomMesh);
        if (roomMaterial != null) Destroy(roomMaterial);
    }

    private Mesh BuildMesh()
    {
        Vector3 half = roomSize * 0.5f;
        Vector3 center = new Vector3(0f, half.y + floorOffset, 0f);

        var vertices = new Vector3[24];
        var normals = new Vector3[24];
        var triangles = new int[36];

        for (int face = 0; face < 6; face++)
        {
            Vector3 o = faceOut[face];
            Vector3 u = faceU[face];
            Vector3 v = faceV[face];
            Vector3 inward = -o;

            Vector3 wallCenter = center + Vector3.Scale(o, half);
            Vector3 uExtent = u * Mathf.Abs(Vector3.Dot(u, half));
            Vector3 vExtent = v * Mathf.Abs(Vector3.Dot(v, half));

            int i = face * 4;
            vertices[i] = wallCenter - uExtent - vExtent;
            vertices[i + 1] = wallCenter - uExtent + vExtent;
            vertices[i + 2] = wallCenter + uExtent + vExtent;
            vertices[i + 3] = wallCenter + uExtent - vExtent;
            for (int k = 0; k < 4; k++) normals[i + k] = inward;

            // La cara visible tiene que mirar hacia dentro de la sala
            bool facesInward = Vector3.Dot(Vector3.Cross(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]), inward) > 0f;
            int b = facesInward ? 1 : 2;
            int c = facesInward ? 2 : 1;
            int d = facesInward ? 3 : 2;
            int e = facesInward ? 2 : 3;

            int t = face * 6;
            triangles[t] = i;
            triangles[t + 1] = i + b;
            triangles[t + 2] = i + c;
            triangles[t + 3] = i;
            triangles[t + 4] = i + e;
            triangles[t + 5] = i + d;
        }

        var mesh = new Mesh { name = "SimulationRoom" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0f, roomSize.y * 0.5f + floorOffset, 0f), roomSize);
    }
}
