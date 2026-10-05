using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Estados visuales de un boton del menu (normal / hover / pulsado): relleno, borde, grosor de
// borde y color de texto. El Button de Unity solo sabe tenir un grafico; esto cubre el resto.
public class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [System.Serializable]
    public class StateStyle
    {
        public Color fill = Color.black;
        public Color border = Color.white;
        public Color text = Color.white;
        public float borderWidth = 2f;
    }

    private Image border;
    private Image fill;
    private TMP_Text label;
    private StateStyle normal;
    private StateStyle hover;
    private StateStyle pressed;

    private bool isHovered = false;
    private bool isPressed = false;

    public void Init(Image border, Image fill, TMP_Text label, StateStyle normal, StateStyle hover, StateStyle pressed)
    {
        this.border = border;
        this.fill = fill;
        this.label = label;
        this.normal = normal;
        this.hover = hover;
        this.pressed = pressed;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; Apply(); }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; isPressed = false; Apply(); }
    public void OnPointerDown(PointerEventData eventData) { isPressed = true; Apply(); }
    public void OnPointerUp(PointerEventData eventData) { isPressed = false; Apply(); }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        Apply();
    }

    private void Apply()
    {
        if (border == null || fill == null || normal == null) return;

        StateStyle style = isPressed ? pressed : (isHovered ? hover : normal);
        border.color = style.border;
        fill.color = style.fill;
        if (label != null) label.color = style.text;

        // El relleno se encoge para dejar ver el borde con el grosor de este estado
        RectTransform fillRect = fill.rectTransform;
        fillRect.offsetMin = new Vector2(style.borderWidth, style.borderWidth);
        fillRect.offsetMax = new Vector2(-style.borderWidth, -style.borderWidth);
    }
}
