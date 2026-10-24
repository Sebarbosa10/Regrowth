using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Herramienta de editor: optimiza los prefabs de basura (menu Regrowth > Optimizar prefabs de basura).
// - Rigidbody kinematic: TrashFloat mueve la basura cada frame, y mover un collider sin Rigidbody
//   (collider "estatico") obliga a PhysX a reconstruir su estructura de colliders estaticos. Con 60 basuras
//   en la ronda 3 eso cuesta frames. Con Rigidbody kinematic el movimiento es barato.
// - Sin sombras: la basura no necesita proyectar ni recibir sombra.
// - Material opaco: crea una copia URP/Lit opaca de cada material Dissolve (mismo color, metal y suavidad)
//   y la asigna a TrashDissolveSpawner, que la pone al terminar la aparicion.
// Se puede ejecutar varias veces.
public static class TrashPrefabOptimizer
{
    private const string TrashFolder = "Assets/Graphics/Prefabs/TRASH";
    private const string MaterialsParent = "Assets/Graphics/Materials";
    private const string OpaqueFolderName = "TrashOpaque";

    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int MetallicStrengthID = Shader.PropertyToID("_MetallicStrength");
    private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");

    [MenuItem("Regrowth/Optimizar prefabs de basura")]
    private static void Apply()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            EditorUtility.DisplayDialog("Optimizar basura", "No se encontro el shader Universal Render Pipeline/Lit.", "OK");
            return;
        }

        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TrashFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);

            if (contents.GetComponent<TrashObject>() != null)
            {
                Rigidbody body = contents.GetComponent<Rigidbody>();
                if (body == null) body = contents.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.None;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;

                Material dissolveMaterial = null;
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    if (dissolveMaterial == null) dissolveMaterial = renderer.sharedMaterial;
                }

                TrashDissolveSpawner spawner = contents.GetComponent<TrashDissolveSpawner>();
                if (spawner != null && dissolveMaterial != null)
                {
                    var serialized = new SerializedObject(spawner);
                    serialized.FindProperty("opaqueMaterial").objectReferenceValue = GetOrCreateOpaque(dissolveMaterial, lit);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                changed++;
                Debug.Log("[TrashPrefabOptimizer] Optimizado " + path);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[TrashPrefabOptimizer] Prefabs optimizados: " + changed);
    }

    // Copia opaca de un material Dissolve. Los Emissive* no usan texturas: color + metal + suavidad.
    private static Material GetOrCreateOpaque(Material source, Shader lit)
    {
        string folder = MaterialsParent + "/" + OpaqueFolderName;
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(MaterialsParent, OpaqueFolderName);

        string path = folder + "/" + source.name + "_Opaque.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(lit);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", source.HasProperty(ColorID) ? source.GetColor(ColorID) : Color.white);
        material.SetFloat("_Metallic", source.HasProperty(MetallicStrengthID) ? source.GetFloat(MetallicStrengthID) : 0f);
        material.SetFloat("_Smoothness", source.HasProperty(SmoothnessID) ? source.GetFloat(SmoothnessID) : 0.5f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }
}
