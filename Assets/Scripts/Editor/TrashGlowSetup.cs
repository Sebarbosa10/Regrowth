using UnityEditor;
using UnityEngine;

// Herramienta de editor: anade TrashGlow a los prefabs de basura (menu Regrowth > Anadir glow a la basura).
// Edita los prefabs con la API de Unity; ejecutarla otra vez no duplica el componente.
public static class TrashGlowSetup
{
    private const string TrashFolder = "Assets/Graphics/Prefabs/TRASH";

    [MenuItem("Regrowth/Añadir glow a la basura")]
    private static void Apply()
    {
        Shader shader = Shader.Find("Regrowth/TrashGlow");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Glow de la basura", "No se encontro el shader Regrowth/TrashGlow.", "OK");
            return;
        }

        int added = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TrashFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);

            // Solo la basura real (la que avisa a StageManager al destruirse)
            if (contents.GetComponent<TrashObject>() != null && contents.GetComponent<TrashGlow>() == null)
            {
                TrashGlow glow = contents.AddComponent<TrashGlow>();
                var serialized = new SerializedObject(glow);
                serialized.FindProperty("glowShader").objectReferenceValue = shader;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                added++;
                Debug.Log("[TrashGlowSetup] Glow anadido a " + path);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }

        Debug.Log("[TrashGlowSetup] Prefabs modificados: " + added);
    }
}
