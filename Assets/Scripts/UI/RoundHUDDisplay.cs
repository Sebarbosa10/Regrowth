using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en el HUD el total de basura de la ronda y cuántas se destruyeron.
/// Adjuntarlo al mismo GameObject que FloatingHUD, o a cualquier hijo del Canvas.
/// </summary>
public class RoundHUDDisplay : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI destroyedText;

    [Header("Formato")]
    [SerializeField] private string totalLabel = "Total";
    [SerializeField] private string destroyedLabel = "Destruidas";

    private int total = 0;
    private int destroyed = 0;

    public static RoundHUDDisplay Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Llamar desde StageManager al iniciar cada ronda.
    /// </summary>
    public void SetRoundTotal(int totalTrash)
    {
        total = totalTrash;
        destroyed = 0;
        Refresh();
    }

    /// <summary>
    /// Llamar desde StageManager cada vez que se destruye una basura.
    /// </summary>
    public void RegisterDestroyed()
    {
        destroyed++;
        Refresh();
    }

    private void Refresh()
    {
        if (totalText != null)
            totalText.text = $"{totalLabel}: {total}";

        if (destroyedText != null)
            destroyedText.text = $"{destroyedLabel}: {destroyed}";
    }
}