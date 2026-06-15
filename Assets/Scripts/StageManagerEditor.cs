using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StageManager))]
public class StageManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        StageManager manager = (StageManager)target;

        SerializedProperty boxCenterProp = serializedObject.FindProperty("boxCenter");
        SerializedProperty boxSizeProp = serializedObject.FindProperty("boxSize");

        Transform boxCenterTransform = boxCenterProp.objectReferenceValue as Transform;
        Vector3 center = boxCenterTransform != null ? boxCenterTransform.position : manager.transform.position;

        Vector3 size = boxSizeProp.vector3Value;
        Vector3 half = size * 0.5f;

        EditorGUI.BeginChangeCheck();

        Handles.color = new Color(0f, 1f, 0.4f, 0.8f);

        Vector3 newPX = Handles.Slider(center + new Vector3(half.x, 0, 0), Vector3.right);
        Vector3 newNX = Handles.Slider(center + new Vector3(-half.x, 0, 0), Vector3.left);
        Vector3 newPY = Handles.Slider(center + new Vector3(0, half.y, 0), Vector3.up);
        Vector3 newNY = Handles.Slider(center + new Vector3(0, -half.y, 0), Vector3.down);
        Vector3 newPZ = Handles.Slider(center + new Vector3(0, 0, half.z), Vector3.forward);
        Vector3 newNZ = Handles.Slider(center + new Vector3(0, 0, -half.z), Vector3.back);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Edit Spawn Box");

            float sizeX = Mathf.Max(0.1f, (newPX.x - center.x) + (center.x - newNX.x));
            float sizeY = Mathf.Max(0.1f, (newPY.y - center.y) + (center.y - newNY.y));
            float sizeZ = Mathf.Max(0.1f, (newPZ.z - center.z) + (center.z - newNZ.z));

            boxSizeProp.vector3Value = new Vector3(sizeX, sizeY, sizeZ);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
