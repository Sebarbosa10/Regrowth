using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleDisplay : MonoBehaviour
{
    public static SubtitleDisplay Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI subtitleText;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        subtitleText.text = string.Empty;
        subtitleText.gameObject.SetActive(false);
    }

    public void Show(string text, float duration)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        subtitleText.text = text;
        subtitleText.gameObject.SetActive(true);
        hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    public void Hide()
    {
        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        subtitleText.gameObject.SetActive(false);
        subtitleText.text = string.Empty;
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Hide();
    }
}
