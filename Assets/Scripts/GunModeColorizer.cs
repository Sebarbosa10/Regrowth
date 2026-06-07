using System.Collections;
using UnityEngine;

public class GunModeColorizer : MonoBehaviour
{
    [System.Serializable]
    public class ModeColors
    {
        public string modeName;         // Solo para identificarlo en el Inspector
        public Color color = Color.white;
    }

    [Header("Renderers a colorear")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Index del material dentro de cada Renderer")]
    [SerializeField] private int materialIndex = 0;

    [Header("Colores por modo (Single, Burst, Auto, Spread)")]
    [SerializeField]
    private ModeColors[] modeColors = new ModeColors[]
    {
        new ModeColors { modeName = "Single  - Plastic", color = Color.green   },
        new ModeColors { modeName = "Burst   - Glass",   color = Color.cyan    },
        new ModeColors { modeName = "Auto    - Organic", color = Color.yellow  },
        new ModeColors { modeName = "Spread  - Metal",   color = Color.red     },
    };

    [Header("Lerp Settings")]
    [SerializeField] private float lerpDuration = 0.35f;
    [SerializeField] private string colorProperty = "_Color"; // "_BaseColor" para URP/HDRP

    private MaterialPropertyBlock propBlock;
    private Coroutine lerpCoroutine;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Llamar desde TrashGun al cambiar de modo.
    /// </summary>
    public void SetMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex >= modeColors.Length) return;

        if (lerpCoroutine != null) StopCoroutine(lerpCoroutine);
        lerpCoroutine = StartCoroutine(LerpToColor(modeColors[modeIndex].color));
    }

    private IEnumerator LerpToColor(Color targetColor)
    {
        // Leer el color actual del primer renderer como punto de partida
        Color startColor = GetCurrentColor();

        float t = 0f;
        while (t < lerpDuration)
        {
            t += Time.deltaTime;
            Color current = Color.Lerp(startColor, targetColor, t / lerpDuration);
            ApplyColor(current);
            yield return null;
        }

        ApplyColor(targetColor);
        lerpCoroutine = null;
    }

    private Color GetCurrentColor()
    {
        if (targetRenderers == null || targetRenderers.Length == 0) return Color.white;
        Renderer r = targetRenderers[0];
        if (r == null) return Color.white;

        r.GetPropertyBlock(propBlock, materialIndex);
        // Si el propBlock no tiene el color todavía, leer del material directamente
        Color c = propBlock.GetColor(colorProperty);
        return c == Color.clear
            ? r.sharedMaterials[materialIndex].GetColor(colorProperty)
            : c;
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderers == null) return;
        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock, materialIndex);
            propBlock.SetColor(colorProperty, color);
            r.SetPropertyBlock(propBlock, materialIndex);
        }
    }
}