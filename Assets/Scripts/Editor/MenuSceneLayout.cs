using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramienta de editor: coloca la escena Menu para VR con un clic (menu Regrowth > Colocar escena Menu).
// Usa Undo, asi que todo se puede deshacer con Ctrl+Z antes de guardar.
// Distancias pensadas para Quest 2: jugador en el origen mirando a +Z, ojos a ~1,6 m.
public static class MenuSceneLayout
{
    // Jugador
    private const float EyeHeight = 1.6f;

    // Panel del menu: cerca, un poco por debajo de los ojos e inclinado hacia el jugador
    private const float PanelDistance = 1.8f;
    private const float PanelHeight = 1.25f;
    private const float PanelTilt = 10f;
    private const float PanelWidthMeters = 0.9f;
    private const float CanvasSize = 300f;
    private const float PanelWidthInCanvas = 0.7f;

    // Titulo sobre el horizonte
    private const float TitleDistance = 12f;
    private const float TitleHeight = 4.2f;
    private const float TitleFontSize = 8.5f;
    private const float SubtitleHeight = 3.3f;
    private const float SubtitleFontSize = 2.1f;

    // Mar y cielo
    private const float WaterLevel = -1f;

    [MenuItem("Regrowth/Colocar escena Menu")]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Menu")
        {
            EditorUtility.DisplayDialog("Colocar escena Menu", "Abre primero la escena Menu.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Colocar escena Menu");
        int undoGroup = Undo.GetCurrentGroup();

        PlaceCameraRig();
        PlaceMenuPanel();
        PlaceTitles();
        PlaceEnvironment();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[MenuSceneLayout] Escena Menu colocada. Revisa y guarda con Ctrl+S (Ctrl+Z para deshacer).");
    }

    // El rig en el origen y sin inclinacion: cualquier giro en X/Z inclina el horizonte y marea
    private static void PlaceCameraRig()
    {
        OVRCameraRig rig = Object.FindObjectOfType<OVRCameraRig>();
        if (rig == null)
        {
            Debug.LogWarning("[MenuSceneLayout] No se encontro el OVRCameraRig.");
            return;
        }

        Transform root = rig.transform.root;
        Undo.RecordObject(root, "Colocar rig");
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (rig.transform != root)
        {
            Undo.RecordObject(rig.transform, "Colocar rig");
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
        }
    }

    private static void PlaceMenuPanel()
    {
        MenuStyler styler = Object.FindObjectOfType<MenuStyler>();
        if (styler == null)
        {
            Debug.LogWarning("[MenuSceneLayout] No se encontro MenuStyler en el Canvas del menu.");
            return;
        }

        var canvas = (RectTransform)styler.transform;
        Transform uiRoot = canvas.parent != null ? canvas.parent : canvas;

        Undo.RecordObject(uiRoot, "Colocar panel");
        uiRoot.position = new Vector3(0f, PanelHeight, PanelDistance);
        uiRoot.rotation = Quaternion.Euler(PanelTilt, 0f, 0f);
        if (uiRoot != canvas) uiRoot.localScale = Vector3.one;

        // El panel (PanelWidthInCanvas del Canvas) mide PanelWidthMeters de ancho
        float scale = PanelWidthMeters / (CanvasSize * PanelWidthInCanvas);
        Undo.RecordObject(canvas, "Colocar panel");
        if (uiRoot != canvas)
        {
            canvas.localPosition = Vector3.zero;
            canvas.localRotation = Quaternion.identity;
        }
        canvas.sizeDelta = new Vector2(CanvasSize, CanvasSize);
        canvas.localScale = Vector3.one * scale;

        var serialized = new SerializedObject(styler);
        serialized.FindProperty("panelWidthInCanvas").floatValue = PanelWidthInCanvas;
        serialized.FindProperty("panelOffset").vector2Value = Vector2.zero;

        SerializedProperty font = serialized.FindProperty("font");
        if (font.objectReferenceValue == null)
        {
            string[] guids = AssetDatabase.FindAssets("ScienceGothic t:TMP_FontAsset");
            if (guids.Length > 0)
                font.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        SerializedProperty title = serialized.FindProperty("title");
        if (title.objectReferenceValue == null) title.objectReferenceValue = FindWorldText("REGROWTH");
        SerializedProperty subtitle = serialized.FindProperty("subtitle");
        if (subtitle.objectReferenceValue == null) subtitle.objectReferenceValue = FindWorldText("simulaci");

        serialized.ApplyModifiedProperties();
    }

    private static void PlaceTitles()
    {
        PlaceWorldText(FindWorldText("REGROWTH"), TitleHeight, TitleFontSize, 24f, 3f);
        PlaceWorldText(FindWorldText("simulaci"), SubtitleHeight, SubtitleFontSize, 24f, 1f);
    }

    private static void PlaceWorldText(TMP_Text text, float height, float fontSize, float width, float boxHeight)
    {
        if (text == null) return;

        Undo.RecordObject(text.transform, "Colocar titulo");
        text.transform.SetPositionAndRotation(new Vector3(0f, height, TitleDistance), Quaternion.identity);
        text.transform.localScale = Vector3.one;

        Undo.RecordObject(text.rectTransform, "Colocar titulo");
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(width, boxHeight);

        Undo.RecordObject(text, "Colocar titulo");
        text.text = text.text.Trim();
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(text);
    }

    private static void PlaceEnvironment()
    {
        // El agua antigua de IgniteCoders tiene las texturas borradas
        GameObject oldWater = GameObject.Find("WaterBlock_50m");
        if (oldWater != null)
        {
            Undo.RecordObject(oldWater, "Desactivar agua antigua");
            oldWater.SetActive(false);
        }

        MenuEnvironment environment = Object.FindObjectOfType<MenuEnvironment>();
        if (environment == null)
        {
            var go = new GameObject("MenuEnvironment");
            Undo.RegisterCreatedObjectUndo(go, "Crear MenuEnvironment");
            environment = Undo.AddComponent<MenuEnvironment>(go);
        }

        Undo.RecordObject(environment.transform, "Colocar mar");
        environment.transform.SetPositionAndRotation(new Vector3(0f, WaterLevel, 0f), Quaternion.identity);

        var serialized = new SerializedObject(environment);
        // Centro del cielo a la altura de los ojos
        serialized.FindProperty("skyCenterHeight").floatValue = EyeHeight - WaterLevel;
        // Luna arriba a la derecha, como en el diseno, para que no tape el titulo
        serialized.FindProperty("moonDirection").vector3Value = new Vector3(0.55f, 0.45f, 1f);

        SerializedProperty waterShader = serialized.FindProperty("waterShader");
        if (waterShader.objectReferenceValue == null) waterShader.objectReferenceValue = Shader.Find("Regrowth/MenuWater");
        SerializedProperty skyShader = serialized.FindProperty("skyShader");
        if (skyShader.objectReferenceValue == null) skyShader.objectReferenceValue = Shader.Find("Regrowth/MenuSky");

        serialized.ApplyModifiedProperties();
    }

    private static TMP_Text FindWorldText(string contains)
    {
        foreach (TextMeshPro text in Object.FindObjectsOfType<TextMeshPro>(true))
        {
            if (text.text != null && text.text.Contains(contains)) return text;
        }
        return null;
    }
}
