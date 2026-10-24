using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleDisplay : MonoBehaviour
{
    public static SubtitleDisplay Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float charsPerSecond = 30f;

    private Coroutine activeCoroutine;

    public TMP_Text Text => subtitleText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        subtitleText.text = string.Empty;
        subtitleText.gameObject.SetActive(false);
    }

    // typingSpeed multiplica la velocidad de escritura (para acompanar audio acelerado)
    public void Show(string text, float duration, float typingSpeed = 1f)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(TypeAndHide(text, duration, Mathf.Max(typingSpeed, 0.01f)));
    }

    public void Hide()
    {
        if (activeCoroutine != null) { StopCoroutine(activeCoroutine); activeCoroutine = null; }
        subtitleText.gameObject.SetActive(false);
        subtitleText.text = string.Empty;
    }

    private IEnumerator TypeAndHide(string text, float duration, float typingSpeed)
    {
        subtitleText.text = string.Empty;
        subtitleText.gameObject.SetActive(true);

        float delay = 1f / (charsPerSecond * typingSpeed);
        for (int i = 0; i < text.Length; i++)
        {
            subtitleText.text = text.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }

        float typingTime = text.Length * delay;
        float remainingTime = duration - typingTime;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        Hide();
    }
}
