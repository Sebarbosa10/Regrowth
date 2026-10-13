using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramienta de editor: deja el oceano optimizado (menu Regrowth > Optimizar plantas del oceano).
// - Las DissolveCount plantas mas cercanas al jugador van a OCEAN/DissolvePlants con el material Dissolve.
// - El resto va a OCEAN/StaticPlants con el material opaco "... 1" (Simple Lit).
// - DissolveEvent: appearRoots = DissolvePlants, toggleRoots = StaticPlants.
// Se puede ejecutar varias veces (reparte de nuevo). Todo con Undo.
public static class OceanPlantsSetup
{
    private const int DissolveCount = 12;
    private const string OpaqueSuffix = " 1";

    [MenuItem("Regrowth/Optimizar plantas del océano")]
    private static void Apply()
    {
        FirstGrabDissolveEvent dissolveEvent = Object.FindObjectOfType<FirstGrabDissolveEvent>(true);
        GameObject ocean = GameObject.Find("OCEAN");
        if (dissolveEvent == null || ocean == null)
        {
            EditorUtility.DisplayDialog("Optimizar plantas", "Abre SampleScene (faltan OCEAN o el DissolveEvent).", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Optimizar plantas del oceano");
        int undoGroup = Undo.GetCurrentGroup();

        OVRCameraRig rig = Object.FindObjectOfType<OVRCameraRig>(true);
        Vector3 player = rig != null ? rig.transform.root.position : Vector3.zero;

        var plants = new List<MeshRenderer>(ocean.GetComponentsInChildren<MeshRenderer>(true));
        if (plants.Count == 0) return;

        // Las mas cercanas (en horizontal) son las que mas se ven: esas hacen el dissolve
        plants.Sort((a, b) => FlatDistance(a.transform.position, player).CompareTo(FlatDistance(b.transform.position, player)));

        Transform dissolveGroup = GetOrCreateGroup(ocean.transform, "DissolvePlants", plants[0].gameObject);
        Transform staticGroup = GetOrCreateGroup(ocean.transform, "StaticPlants", plants[0].gameObject);

        for (int i = 0; i < plants.Count; i++)
        {
            bool dissolve = i < DissolveCount;
            Undo.SetTransformParent(plants[i].transform, dissolve ? dissolveGroup : staticGroup, "Agrupar planta");
            SwapMaterials(plants[i], dissolve);
        }

        var serialized = new SerializedObject(dissolveEvent);
        SetSingleElement(serialized.FindProperty("appearRoots"), dissolveGroup.gameObject);
        SetSingleElement(serialized.FindProperty("toggleRoots"), staticGroup.gameObject);
        serialized.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(ocean.scene);
        Debug.Log($"[OceanPlantsSetup] {Mathf.Min(DissolveCount, plants.Count)} plantas con dissolve, " +
                  $"{Mathf.Max(0, plants.Count - DissolveCount)} opacas. Revisa y guarda con Ctrl+S.");
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Transform GetOrCreateGroup(Transform ocean, string groupName, GameObject flagsSource)
    {
        Transform group = ocean.Find(groupName);
        if (group != null) return group;

        var go = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(go, "Crear grupo de plantas");
        go.transform.SetParent(ocean, false);
        GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(flagsSource));
        return go.transform;
    }

    private static void SwapMaterials(Renderer renderer, bool dissolve)
    {
        Material[] materials = renderer.sharedMaterials;
        bool changed = false;

        for (int i = 0; i < materials.Length; i++)
        {
            Material target = dissolve ? DissolveVersion(materials[i]) : OpaqueVersion(materials[i]);
            if (target != null && target != materials[i])
            {
                materials[i] = target;
                changed = true;
            }
        }

        if (!changed) return;
        Undo.RecordObject(renderer, "Cambiar material de planta");
        renderer.sharedMaterials = materials;
    }

    // SeaPlants_MAT -> SeaPlants_MAT 1 (Simple Lit opaco)
    private static Material OpaqueVersion(Material material)
    {
        if (material == null || material.name.EndsWith(OpaqueSuffix)) return material;
        return LoadSibling(material, material.name + OpaqueSuffix);
    }

    // SeaPlants_MAT 1 -> SeaPlants_MAT (shader Dissolve)
    private static Material DissolveVersion(Material material)
    {
        if (material == null || !material.name.EndsWith(OpaqueSuffix)) return material;
        return LoadSibling(material, material.name.Substring(0, material.name.Length - OpaqueSuffix.Length));
    }

    private static Material LoadSibling(Material material, string siblingName)
    {
        string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(material)).Replace('\\', '/');
        Material sibling = AssetDatabase.LoadAssetAtPath<Material>(folder + "/" + siblingName + ".mat");
        if (sibling == null) Debug.LogWarning("[OceanPlantsSetup] No existe el material " + folder + "/" + siblingName + ".mat");
        return sibling;
    }

    private static void SetSingleElement(SerializedProperty array, Object value)
    {
        array.arraySize = 1;
        array.GetArrayElementAtIndex(0).objectReferenceValue = value;
    }
}
