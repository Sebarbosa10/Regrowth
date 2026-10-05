using Oculus.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramienta de editor: configura el agarre con pose fija del arma (menu Regrowth > Configurar agarre del arma).
// Anade GunGrabTransformer al MainGrip, lo asigna como One Grab Transformer del Grabbable y desactiva
// ForwardGrip (un segundo Grabbable sobre el mismo arma que pelearia con el). Todo con Undo.
public static class GunGrabSetup
{
    [MenuItem("Regrowth/Configurar agarre del arma")]
    private static void Apply()
    {
        TrashGun gun = Object.FindObjectOfType<TrashGun>(true);
        if (gun == null)
        {
            EditorUtility.DisplayDialog("Configurar agarre", "Abre la escena del juego (no hay ningun TrashGun).", "OK");
            return;
        }

        var gunSerialized = new SerializedObject(gun);
        var grabbable = gunSerialized.FindProperty("grabbable").objectReferenceValue as Grabbable;
        var twoHandedGrip = gunSerialized.FindProperty("twoHandedGrip").objectReferenceValue as TwoHandedGunGrip;
        if (grabbable == null)
        {
            EditorUtility.DisplayDialog("Configurar agarre", "El TrashGun no tiene asignado su Grabbable.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Configurar agarre del arma");
        int undoGroup = Undo.GetCurrentGroup();

        GunGrabTransformer transformer = grabbable.GetComponent<GunGrabTransformer>();
        if (transformer == null) transformer = Undo.AddComponent<GunGrabTransformer>(grabbable.gameObject);

        var transformerSerialized = new SerializedObject(transformer);
        transformerSerialized.FindProperty("gripAnchor").objectReferenceValue = FindChild(gun.transform, "MainAnchor");
        transformerSerialized.FindProperty("twoHandedGrip").objectReferenceValue = twoHandedGrip;

        // Si el angulo sigue en 0 (apunta como el laser del mando) se pone el de agarre natural.
        // Un valor ya ajustado a mano se respeta.
        SerializedProperty rotationOffset = transformerSerialized.FindProperty("gripRotationOffset");
        if (rotationOffset.vector3Value == Vector3.zero)
            rotationOffset.vector3Value = new Vector3(GunGrabTransformer.DefaultGripPitch, 0f, 0f);

        OVRCameraRig rig = Object.FindObjectOfType<OVRCameraRig>(true);
        if (rig != null)
            transformerSerialized.FindProperty("trackingSpace").objectReferenceValue = FindChild(rig.transform, "TrackingSpace");
        transformerSerialized.ApplyModifiedProperties();

        var grabbableSerialized = new SerializedObject(grabbable);
        grabbableSerialized.FindProperty("_oneGrabTransformer").objectReferenceValue = transformer;
        grabbableSerialized.ApplyModifiedProperties();

        Transform forwardGrip = FindChild(gun.transform, "ForwardGrip");
        if (forwardGrip != null)
        {
            Undo.RecordObject(forwardGrip.gameObject, "Desactivar ForwardGrip");
            forwardGrip.gameObject.SetActive(false);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(gun.gameObject.scene);
        Selection.activeObject = transformer;
        Debug.Log("[GunGrabSetup] Agarre configurado en " + grabbable.name + ". Revisa y guarda con Ctrl+S.");
    }

    // Busca por nombre en toda la jerarquia, ignorando espacios sobrantes (MainAnchor tiene espacios al final)
    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Trim() == childName) return child;
        }
        return null;
    }
}
