using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Construye el menu principal al arrancar segun el diseno de REGROWTH (panel holografico con
// rejilla y esquinas, barras de modo, botones con estados y pantalla de controles).
// Va en el Canvas del menu: se construye dentro de el para reutilizar la interaccion de rayo y
// poke del Interaction SDK. Todas las medidas estan en px del diseno (panel de 520x560).
public class MenuStyler : MonoBehaviour
{
    [System.Serializable]
    public class ControlEntry
    {
        public string action;
        public string control;
    }

    [Header("Referencias")]
    [Tooltip("Vacio = se busca al arrancar")]
    [SerializeField] private MainMenu mainMenu;
    [Tooltip("Fuente Science Gothic (TMP). Vacio = la de los botones antiguos")]
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("Contenido antiguo del menu: se ocultan sus hijos y sus graficos, el objeto sigue activo. Vacio = primer VerticalLayoutGroup de los hijos")]
    [SerializeField] private GameObject legacyContent;
    [Tooltip("Textos 3D de la escena")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitle;

    [Header("Tamano")]
    [Tooltip("Ancho del panel principal respecto al ancho del Canvas")]
    [SerializeField, Range(0.2f, 1f)] private float panelWidthInCanvas = 0.8f;
    [Tooltip("Desplazamiento de los paneles en px del diseno (Y positivo = abajo)")]
    [SerializeField] private Vector2 panelOffset = Vector2.zero;

    [Header("Paleta")]
    [SerializeField] private Color accentColor = Hex("#1AE6FF");
    [SerializeField] private Color textColor = Hex("#BFF7FF");
    [SerializeField] private Color panelColor = new Color(5f / 255f, 13f / 255f, 26f / 255f, 0.78f);
    [SerializeField] private Color gridColor = new Color(26f / 255f, 230f / 255f, 1f, 0.07f);
    [Tooltip("Plastico, vidrio, organico, metal")]
    [SerializeField] private Color[] modeColors = { Hex("#FF00E3"), Hex("#FFD000"), Hex("#54FF00"), Hex("#FF0000") };

    [Header("Botones")]
    [SerializeField] private MenuButtonVisual.StateStyle normalStyle = new MenuButtonVisual.StateStyle
        { fill = Hex("#0A2438"), border = Hex("#1A5A78"), text = Hex("#BFF7FF"), borderWidth = 2f };
    [SerializeField] private MenuButtonVisual.StateStyle hoverStyle = new MenuButtonVisual.StateStyle
        { fill = Hex("#14809E"), border = Hex("#1AE6FF"), text = Color.white, borderWidth = 3f };
    [SerializeField] private MenuButtonVisual.StateStyle pressedStyle = new MenuButtonVisual.StateStyle
        { fill = Hex("#1AE6FF"), border = Hex("#BFF7FF"), text = Hex("#010308"), borderWidth = 3f };

    [Header("Textos")]
    [SerializeField] private string mainHeader = "MENÚ PRINCIPAL";
    [SerializeField] private string playLabel = "JUGAR";
    [SerializeField] private string controlsLabel = "CONTROLES";
    [SerializeField] private string quitLabel = "SALIR";
    [SerializeField] private string controlsHeader = "CONTROLES";
    [SerializeField] private string backLabel = "VOLVER";
    [SerializeField] private ControlEntry[] controls =
    {
        new ControlEntry { action = "Agarrar el arma", control = "Botón lateral" },
        new ControlEntry { action = "Disparar", control = "Gatillo derecho" },
        new ControlEntry { action = "Cambiar disparo", control = "Botón A" },
        new ControlEntry { action = "Recargar", control = "Agitar el arma" },
        new ControlEntry { action = "Apuntar a dos manos", control = "Agarre izquierdo" },
        new ControlEntry { action = "Ver controles", control = "Botón X" },
        new ControlEntry { action = "Continuar avisos", control = "Botón B" },
    };

    // Medidas del diseno (px)
    private const float MainWidth = 520f;
    private const float MainHeight = 560f;
    private const float ControlsWidth = 680f;
    private const float Padding = 40f;
    private const float GridStep = 40f;
    private const float FrameWidth = 2f;
    private const float CornerSize = 36f;
    private const float CornerThickness = 4f;
    private const float CornerOverhang = 10f;
    private const float HeaderSize = 24f;
    private const float HeaderGap = 16f;
    private const float BarHeight = 8f;
    private const float BarGap = 10f;
    private const float SectionGap = 26f;
    private const float ButtonHeight = 108f;
    private const float ButtonGap = 24f;
    private const float ButtonTextSize = 42f;
    private const float RowHeight = 46f;
    private const float RowTextSize = 24f;
    private const float WideSpacing = 30f;

    private GameObject mainPanel;
    private GameObject controlsPanel;
    private readonly List<Button> mainButtons = new List<Button>();

    private void Awake()
    {
        // Una sola busqueda al arrancar el menu
        if (mainMenu == null) mainMenu = FindObjectOfType<MainMenu>();

        if (legacyContent == null)
        {
            VerticalLayoutGroup layout = GetComponentInChildren<VerticalLayoutGroup>(true);
            if (layout != null) legacyContent = layout.gameObject;
        }

        if (font == null)
        {
            TMP_Text oldLabel = legacyContent != null ? legacyContent.GetComponentInChildren<TMP_Text>(true) : null;
            font = oldLabel != null ? oldLabel.font : TMP_Settings.defaultFontAsset;
        }

        if (legacyContent != null) HideLegacyContent();

        RectTransform canvasRect = (RectTransform)transform;
        float scale = canvasRect.rect.width * panelWidthInCanvas / MainWidth;

        mainPanel = BuildMainPanel(scale);
        controlsPanel = BuildControlsPanel(scale);
        controlsPanel.SetActive(false);

        StyleWorldText(title, 42f, true);
        StyleWorldText(subtitle, 34f, false);
    }

    // No se desactiva el objeto en si: puede llevar scripts que el menu necesita activos
    // (MainMenu esta en UIElements y arranca el fundido con una corrutina).
    private void HideLegacyContent()
    {
        foreach (Transform child in legacyContent.transform)
            child.gameObject.SetActive(false);

        foreach (Graphic graphic in legacyContent.GetComponents<Graphic>())
            graphic.enabled = false;
    }

    // ─────────────────────────────────────────
    //  PANELES
    // ─────────────────────────────────────────

    private GameObject BuildMainPanel(float scale)
    {
        RectTransform panel = CreatePanel("MainMenuPanel", MainWidth, MainHeight, scale);
        float contentWidth = MainWidth - Padding * 2f;
        float y = BuildHeader(panel, mainHeader, contentWidth);

        string[] labels = { playLabel, controlsLabel, quitLabel };
        UnityEngine.Events.UnityAction[] actions = { OnPlay, () => ShowControls(true), OnQuit };

        for (int i = 0; i < labels.Length; i++)
        {
            Button button = CreateButton(panel, Padding, y, contentWidth, ButtonHeight, labels[i], actions[i]);
            mainButtons.Add(button);
            y += ButtonHeight + ButtonGap;
        }

        return panel.gameObject;
    }

    private GameObject BuildControlsPanel(float scale)
    {
        float height = Padding + HeaderSize + HeaderGap + BarHeight + SectionGap
                       + controls.Length * RowHeight + ButtonGap + ButtonHeight + Padding;
        RectTransform panel = CreatePanel("ControlsPanel", ControlsWidth, height, scale);
        float contentWidth = ControlsWidth - Padding * 2f;
        float y = BuildHeader(panel, controlsHeader, contentWidth);

        foreach (ControlEntry entry in controls)
        {
            if (entry == null) continue;
            TMP_Text action = CreateText(Box("Action", panel, Padding, y, contentWidth * 0.55f, RowHeight),
                entry.action, RowTextSize, FontStyles.Normal, 4f, TextAlignmentOptions.MidlineLeft);
            action.color = textColor;
            TMP_Text control = CreateText(Box("Control", panel, Padding + contentWidth * 0.55f, y, contentWidth * 0.45f, RowHeight),
                entry.control, RowTextSize, FontStyles.Bold, 4f, TextAlignmentOptions.MidlineRight);
            control.color = accentColor;
            y += RowHeight;
        }

        CreateButton(panel, Padding, y + ButtonGap, contentWidth, ButtonHeight, backLabel, () => ShowControls(false));
        return panel.gameObject;
    }

    // Encabezado + las cuatro barras de modo. Devuelve la Y donde empieza el contenido.
    private float BuildHeader(RectTransform panel, string text, float contentWidth)
    {
        TMP_Text header = CreateText(Box("Header", panel, Padding, Padding, contentWidth, HeaderSize),
            text, HeaderSize, FontStyles.Normal, WideSpacing, TextAlignmentOptions.MidlineLeft);
        header.color = textColor;

        float barsY = Padding + HeaderSize + HeaderGap;
        int count = modeColors.Length;
        float barWidth = (contentWidth - BarGap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
            CreateImage(Box("ModeBar", panel, Padding + i * (barWidth + BarGap), barsY, barWidth, BarHeight), modeColors[i]);

        return barsY + BarHeight + SectionGap;
    }

    // Fondo translucido, rejilla, marco y marcas de esquina en "L"
    private RectTransform CreatePanel(string panelName, float width, float height, float scale)
    {
        RectTransform panel = CreateRect(panelName, transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(width, height);
        panel.anchoredPosition = new Vector2(panelOffset.x, -panelOffset.y) * scale;
        panel.localScale = Vector3.one * scale;

        CreateImage(Stretch(CreateRect("Background", panel)), panelColor);

        for (float x = GridStep; x < width; x += GridStep)
            CreateImage(Box("GridV", panel, x, 0f, 1f, height), gridColor);
        for (float y = GridStep; y < height; y += GridStep)
            CreateImage(Box("GridH", panel, 0f, y, width, 1f), gridColor);

        CreateImage(Box("FrameTop", panel, 0f, 0f, width, FrameWidth), accentColor);
        CreateImage(Box("FrameBottom", panel, 0f, height - FrameWidth, width, FrameWidth), accentColor);
        CreateImage(Box("FrameLeft", panel, 0f, 0f, FrameWidth, height), accentColor);
        CreateImage(Box("FrameRight", panel, width - FrameWidth, 0f, FrameWidth, height), accentColor);

        float left = -CornerOverhang;
        float right = width + CornerOverhang - CornerSize;
        float top = -CornerOverhang;
        float bottom = height + CornerOverhang - CornerSize;
        CreateCorner(panel, left, top, false, false);
        CreateCorner(panel, right, top, true, false);
        CreateCorner(panel, left, bottom, false, true);
        CreateCorner(panel, right, bottom, true, true);

        return panel;
    }

    private void CreateCorner(RectTransform panel, float x, float y, bool isRight, bool isBottom)
    {
        float barY = isBottom ? y + CornerSize - CornerThickness : y;
        float barX = isRight ? x + CornerSize - CornerThickness : x;
        CreateImage(Box("CornerH", panel, x, barY, CornerSize, CornerThickness), accentColor);
        CreateImage(Box("CornerV", panel, barX, y, CornerThickness, CornerSize), accentColor);
    }

    private Button CreateButton(RectTransform panel, float x, float y, float width, float height,
        string text, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform root = Box("Button_" + text, panel, x, y, width, height);
        Image border = CreateImage(root, normalStyle.border);
        border.raycastTarget = true;

        Image fill = CreateImage(Stretch(CreateRect("Fill", root)), normalStyle.fill);
        TMP_Text label = CreateText(Stretch(CreateRect("Label", root)), text, ButtonTextSize,
            FontStyles.Bold, WideSpacing, TextAlignmentOptions.Center);
        // Compensa el espacio que el interletrado deja tras la ultima letra
        label.margin = new Vector4(ButtonTextSize * WideSpacing / 100f, 0f, 0f, 0f);

        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = border;
        button.onClick.AddListener(onClick);

        root.gameObject.AddComponent<MenuButtonVisual>().Init(border, fill, label, normalStyle, hoverStyle, pressedStyle);
        return button;
    }

    // ─────────────────────────────────────────
    //  ACCIONES
    // ─────────────────────────────────────────

    private void OnPlay()
    {
        // Evita cargar la escena dos veces si se pulsa de nuevo durante el fundido
        foreach (Button button in mainButtons) button.interactable = false;
        if (mainMenu != null) mainMenu.OnPlayPressed();
        else Debug.LogError("[MenuStyler] No hay MainMenu en la escena.");
    }

    private void OnQuit()
    {
        if (mainMenu != null) mainMenu.OnQuitPressed();
    }

    private void ShowControls(bool show)
    {
        mainPanel.SetActive(!show);
        controlsPanel.SetActive(show);
    }

    // ─────────────────────────────────────────
    //  TITULOS 3D
    // ─────────────────────────────────────────

    private void StyleWorldText(TMP_Text text, float spacing, bool glow)
    {
        if (text == null) return;

        if (font != null) text.font = font;
        text.color = textColor;
        text.characterSpacing = spacing;
        if (glow) text.fontStyle |= FontStyles.Bold;

        if (!glow) return;

        // Brillo cian con el underlay de TMP (funciona tambien con los shaders Mobile)
        Material material = text.fontMaterial;
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f));
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 1f);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.4f);
        text.UpdateMeshPadding();
    }

    // ─────────────────────────────────────────
    //  HELPERS DE UI
    // ─────────────────────────────────────────

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    // Rectangulo en px del diseno, medido desde la esquina superior izquierda del padre
    private RectTransform Box(string objectName, RectTransform parent, float x, float y, float width, float height)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image CreateImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(RectTransform rect, string text, float size, FontStyles style,
        float spacing, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.characterSpacing = spacing;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    // Parser propio (sin APIs nativas de Unity) porque se usa en inicializadores de campos,
    // que se ejecutan durante la deserializacion
    private static Color Hex(string hex)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) return Color.magenta;

        int value = System.Convert.ToInt32(hex, 16);
        return new Color(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f, 1f);
    }
}
