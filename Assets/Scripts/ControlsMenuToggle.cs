using System.Collections;
using UnityEngine;

public class ControlsMenuToggle : MonoBehaviour
{
    [SerializeField] private OVRInput.Button toggleButton = OVRInput.Button.Three;

    [Header("Panel (tiene el material Dissolve)")]
    [SerializeField] private GameObject panel;

    [Header("Hijos a mostrar/ocultar (TMP, iconos, etc)")]
    [SerializeField] private GameObject[] children;

    [Header("Dissolve")]
    [SerializeField] private float dissolveDuration = 0.4f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");

    private Renderer panelRenderer;
    private MaterialPropertyBlock propBlock;
    private bool wasPressed = false;
    private bool isAnimating = false;
    private bool isOpen = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (panel != null)
            panelRenderer = panel.GetComponent<Renderer>();

        // Estado inicial: cerrado
        SetChildrenActive(false);
        ApplyDissolve(1f);
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        bool pressed = OVRInput.Get(toggleButton);

        if (pressed && !wasPressed && !isAnimating)
            StartCoroutine(isOpen ? Hide() : Show());

        wasPressed = pressed;
    }

    // ─────────────────────────────────────────

    private IEnumerator Show()
    {
        isAnimating = true;

        // 1. Activar panel, hijos ocultos todavia, dissolve arranca en 1
        panel.SetActive(true);
        SetChildrenActive(false);
        ApplyDissolve(1f);

        PlaySound(openSound);

        // 2. Panel aparece: dissolve 1 → 0
        yield return StartCoroutine(AnimateDissolve(1f, 0f));

        // 3. Panel ya visible, mostrar hijos
        SetChildrenActive(true);

        isOpen = true;
        isAnimating = false;
    }

    private IEnumerator Hide()
    {
        isAnimating = true;

        // 1. Ocultar hijos primero
        SetChildrenActive(false);

        PlaySound(closeSound);

        // 2. Panel desaparece: dissolve 0 → 1
        yield return StartCoroutine(AnimateDissolve(0f, 1f));

        // 3. Desactivar panel
        panel.SetActive(false);

        isOpen = false;
        isAnimating = false;
    }

    // ─────────────────────────────────────────

    private IEnumerator AnimateDissolve(float from, float to)
    {
        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / dissolveDuration);
            ApplyDissolve(Mathf.Lerp(from, to, dissolveCurve.Evaluate(progress)));
            yield return null;
        }
        ApplyDissolve(to);
    }

    private void ApplyDissolve(float value)
    {
        if (panelRenderer == null) return;
        panelRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(DissolveID, value);
        panelRenderer.SetPropertyBlock(propBlock);
    }

    private void SetChildrenActive(bool active)
    {
        if (children == null) return;
        foreach (GameObject child in children)
            if (child != null) child.SetActive(active);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}