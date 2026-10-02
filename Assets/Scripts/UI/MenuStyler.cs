using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Da estilo al menu principal al arrancar (panel, botones y titulos) sin tocar la escena a mano.
// Se coloca en el Canvas del menu: si no se asignan referencias, busca el panel y los botones en sus hijos.
public class MenuStyler : MonoBehaviour
{
    [Header("Referencias (opcionales)")]
    [Tooltip("Contenedor de los botones. Vacio = primer VerticalLayoutGroup de los hijos")]
    [SerializeField] private RectTransform panel;
    [Tooltip("Vacio = todos los botones de los hijos")]
    [SerializeField] private Button[] buttons;
    [Tooltip("Titulo y subtitulo (los TextMeshPro 3D de la escena)")]
    [SerializeField] private TMP_Text[] titleTexts;

    [Header("Panel")]
    [SerializeField] private Color panelColor = new Color(0.02f, 0.05f, 0.10f, 0.78f);
    [SerializeField] private Color borderColor = new Color(0.1f, 0.9f, 1f, 0.9f);
    [SerializeField] private float borderWidth = 1.5f;
    [SerializeField] private float panelPadding = 12f;

    [Header("Botones")]
    [SerializeField] private Color buttonColor = new Color(0.04f, 0.14f, 0.22f, 0.95f);
    [SerializeField] private Color buttonHighlightColor = new Color(0.08f, 0.50f, 0.62f, 1f);
    [SerializeField] private Color buttonPressedColor = new Color(0.1f, 0.9f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private float buttonTextSpacing = 12f;
    [SerializeField] private float buttonFadeDuration = 0.08f;

    [Header("Titulos")]
    [SerializeField] private Color titleColor = new Color(0.75f, 0.97f, 1f, 1f);
    [SerializeField] private float titleSpacing = 18f;

    private void Awake()
    {
        if (panel == null)
        {
            VerticalLayoutGroup layout = GetComponentInChildren<VerticalLayoutGroup>(true);
            if (layout != null) panel = (RectTransform)layout.transform;
        }

        if (buttons == null || buttons.Length == 0)
            buttons = GetComponentsInChildren<Button>(true);

        StylePanel();
        StyleButtons();
        StyleTitles();
    }

    private void StylePanel()
    {
        if (panel == null) return;

        Image background = panel.GetComponent<Image>();
        if (background == null)
            background = CreateBackground();

        background.color = panelColor;
        background.raycastTarget = false;
        AddBorder(background.gameObject);
    }

    // Fondo detras del panel: mismo rectangulo con algo de margen, dibujado antes que el
    private Image CreateBackground()
    {
        var backgroundObject = new GameObject("MenuBackground", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)backgroundObject.transform;
        rect.SetParent(panel.parent, false);
        rect.SetSiblingIndex(panel.GetSiblingIndex());

        rect.anchorMin = panel.anchorMin;
        rect.anchorMax = panel.anchorMax;
        rect.pivot = panel.pivot;
        rect.anchoredPosition3D = panel.anchoredPosition3D;
        rect.localRotation = panel.localRotation;
        rect.localScale = panel.localScale;
        rect.sizeDelta = panel.sizeDelta + Vector2.one * (panelPadding * 2f);

        return backgroundObject.GetComponent<Image>();
    }

    private void StyleButtons()
    {
        foreach (Button button in buttons)
        {
            if (button == null) continue;

            // El grafico queda en blanco y el color lo ponen los estados del boton
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.white;
                AddBorder(button.targetGraphic.gameObject);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.selectedColor = buttonHighlightColor;
            colors.pressedColor = buttonPressedColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = buttonFadeDuration;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = buttonTextColor;
                label.characterSpacing = buttonTextSpacing;
                label.fontStyle |= FontStyles.Bold;
            }
        }
    }

    private void StyleTitles()
    {
        if (titleTexts == null) return;

        foreach (TMP_Text title in titleTexts)
        {
            if (title == null) continue;
            title.color = titleColor;
            title.characterSpacing = titleSpacing;
        }
    }

    private void AddBorder(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(borderWidth, -borderWidth);
        outline.useGraphicAlpha = false;
    }
}
