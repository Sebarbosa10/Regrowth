using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Indicador bajo los subtitulos, sin texto: un joystick que se mueve a izquierda y derecha
// (saltar bloques) y un aro que se llena al mantenerlo a la derecha (saltar el beat entero).
// Se construye por codigo junto al texto de SubtitleDisplay; los sprites se generan al vuelo.
public class BeatSkipHint : MonoBehaviour
{
    private const int SpriteSize = 64;
    private const float RingThickness = 0.16f;
    private const float KnobTravel = 0.27f;
    private const float AnimationSpeed = 2.4f;

    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite triangleSprite;

    private RectTransform root;
    private RectTransform knob;
    private Image holdRing;
    private Image leftArrow;
    private Image rightArrow;
    private TMP_Text anchorText;
    private Color baseColor;
    private float diameter;
    private float holdProgress = 0f;

    // sizeInFontSizes: diametro del joystick en tamanos de letra del subtitulo.
    // gapInFontSizes: hueco entre el texto y el icono, tambien en tamanos de letra.
    // allTexts: todos los subtitulos posibles; el icono se coloca bajo el mas alto, asi su posicion
    // es siempre la misma y ningun subtitulo lo pisa.
    public static BeatSkipHint Create(TMP_Text anchor, Color accentColor, float sizeInFontSizes,
        float gapInFontSizes, System.Collections.Generic.IEnumerable<string> allTexts)
    {
        var go = new GameObject("BeatSkipHint", typeof(RectTransform));
        go.layer = anchor.gameObject.layer;

        BeatSkipHint hint = go.AddComponent<BeatSkipHint>();
        hint.Build(anchor, accentColor, sizeInFontSizes);
        hint.PlaceBelowText(gapInFontSizes, allTexts);
        go.SetActive(false);
        return hint;
    }

    public void Show(bool visible)
    {
        holdProgress = 0f;
        if (holdRing != null) holdRing.fillAmount = 0f;
        gameObject.SetActive(visible);
    }

    // Posicion fija bajo el subtitulo mas alto. Se tienen en cuenta los margenes del TMP: el subtitulo
    // de la escena tiene margenes negativos y su area real es mucho mayor que su RectTransform.
    // Supone alineacion vertical centrada (la que usa SubtitleDisplay).
    private void PlaceBelowText(float gapInFontSizes, System.Collections.Generic.IEnumerable<string> allTexts)
    {
        RectTransform textRect = anchorText.rectTransform;
        Vector4 margin = anchorText.margin; // izquierda, arriba, derecha, abajo
        float areaWidth = textRect.rect.width - margin.x - margin.z;
        float areaCenterY = (margin.w - margin.y) * 0.5f;

        float maxHeight = anchorText.fontSize;
        if (allTexts != null)
        {
            foreach (string text in allTexts)
            {
                if (string.IsNullOrEmpty(text)) continue;
                maxHeight = Mathf.Max(maxHeight, anchorText.GetPreferredValues(text, areaWidth, 0f).y);
            }
        }

        float offset = -areaCenterY + maxHeight * 0.5f + anchorText.fontSize * gapInFontSizes + root.sizeDelta.y * 0.5f;
        root.anchoredPosition = textRect.anchoredPosition + new Vector2(0f, -offset);
    }

    public void SetHoldProgress(float progress)
    {
        holdProgress = Mathf.Clamp01(progress);
        holdRing.fillAmount = holdProgress;
    }

    // Llamado cada frame mientras suena un beat
    public void Animate(float time)
    {
        float side = holdProgress > 0f ? 1f : Mathf.Sin(time * AnimationSpeed);
        knob.anchoredPosition = new Vector2(side * diameter * KnobTravel, 0f);
        SetAlpha(rightArrow, 0.35f + 0.65f * Mathf.Max(side, 0f));
        SetAlpha(leftArrow, 0.35f + 0.65f * Mathf.Max(-side, 0f));
    }

    // ─────────────────────────────────────────
    //  CONSTRUCCION
    // ─────────────────────────────────────────

    private void Build(TMP_Text anchor, Color accentColor, float sizeInFontSizes)
    {
        anchorText = anchor;
        baseColor = anchor.color;
        diameter = anchor.fontSize * sizeInFontSizes;
        EnsureSprites();

        root = (RectTransform)transform;
        root.SetParent(anchor.transform.parent, false);
        root.anchorMin = anchor.rectTransform.anchorMin;
        root.anchorMax = anchor.rectTransform.anchorMax;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(diameter * 3.2f, diameter);

        // Base del joystick, aro de progreso, palanca y flechas
        CreateImage("Ring", ringSprite, Vector2.zero, diameter, WithAlpha(baseColor, 0.5f));
        holdRing = CreateImage("HoldRing", ringSprite, Vector2.zero, diameter, accentColor);
        holdRing.type = Image.Type.Filled;
        holdRing.fillMethod = Image.FillMethod.Radial360;
        holdRing.fillOrigin = (int)Image.Origin360.Top;
        holdRing.fillClockwise = true;
        holdRing.fillAmount = 0f;

        knob = CreateImage("Knob", circleSprite, Vector2.zero, diameter * 0.42f, WithAlpha(baseColor, 0.9f)).rectTransform;

        float arrowSize = diameter * 0.35f;
        float arrowX = diameter * 0.5f + arrowSize;
        rightArrow = CreateImage("RightArrow", triangleSprite, new Vector2(arrowX, 0f), arrowSize, baseColor);
        leftArrow = CreateImage("LeftArrow", triangleSprite, new Vector2(-arrowX, 0f), arrowSize, baseColor);
        leftArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
    }

    private Image CreateImage(string objectName, Sprite sprite, Vector2 position, float size, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(root, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // ─────────────────────────────────────────
    //  SPRITES PROCEDURALES (una sola vez)
    // ─────────────────────────────────────────

    private static void EnsureSprites()
    {
        if (circleSprite != null) return;

        float aa = 3f / SpriteSize; // borde suavizado de ~1,5 px
        circleSprite = CreateSprite("HintCircle", (u, v) => (1f - Mathf.Sqrt(u * u + v * v)) / aa);
        ringSprite = CreateSprite("HintRing", (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            return Mathf.Min((1f - r) / aa, (r - (1f - RingThickness)) / aa);
        });
        // Triangulo apuntando a la derecha
        triangleSprite = CreateSprite("HintTriangle", (u, v) =>
            Mathf.Min(u + 0.6f, 0.5f * (0.8f - u) - Mathf.Abs(v)) / aa);
    }

    private static Sprite CreateSprite(string spriteName, System.Func<float, float, float> coverage)
    {
        var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
        {
            name = spriteName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[SpriteSize * SpriteSize];
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float u = (x + 0.5f) / SpriteSize * 2f - 1f;
                float v = (y + 0.5f) / SpriteSize * 2f - 1f;
                byte alpha = (byte)(Mathf.Clamp01(coverage(u, v)) * 255f);
                pixels[y * SpriteSize + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f), 100f);
    }
}
