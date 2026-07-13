using TMPro;
using UnityEngine;

public class FpsCounterDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // cada cuanto actualiza

    [Header("Colores")]
    [SerializeField] private Color goodColor = Color.green;    // 60+
    [SerializeField] private Color okColor = Color.yellow;     // 30-60
    [SerializeField] private Color badColor = Color.red;       // menos de 30

    private float timer = 0f;
    private int frameCount = 0;
    private float fps = 0f;

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime; // unscaled para que no lo afecte el timeScale

        if (timer >= updateInterval)
        {
            fps = frameCount / timer;
            frameCount = 0;
            timer = 0f;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (fpsText == null) return;

        // Color según rendimiento
        Color color;
        if (fps >= 60f)
            color = goodColor;
        else if (fps >= 30f)
            color = okColor;
        else
            color = badColor;

        fpsText.color = color;
        fpsText.text = $"FPS: {Mathf.RoundToInt(fps)} | {(1000f / fps):F1}ms";
    }
}
