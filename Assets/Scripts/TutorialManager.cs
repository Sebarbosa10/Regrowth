using System.Collections;
using TMPro;
using UnityEngine;

// Tutorial integrado en la ronda 1: avisos de texto que pausan el juego
// hasta que el jugador pulsa el boton de continuar.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private enum Step { Grab, Shoot, SwitchMode, Recharge, Store }

    [Header("Referencias")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("Cabeza del jugador (CenterEyeAnchor). El panel aparece delante de ella")]
    [SerializeField] private Transform head;
    [SerializeField] private TrashGun trashGun;
    [SerializeField] private GunEnergySystem energySystem;
    [SerializeField] private WeaponHolster[] holsters;

    [Header("Panel")]
    [SerializeField] private float panelDistance = 1.2f;
    [SerializeField] private float panelHeightOffset = -0.1f;
    [SerializeField] private OVRInput.Button continueButton = OVRInput.Button.Two;
    [Tooltip("Tiempo minimo en pantalla antes de poder cerrarlo")]
    [SerializeField] private float minDisplayTime = 0.75f;

    [Header("Tiempos")]
    [Tooltip("Espera tras el texto inicial (lo que tarda en disolverse la sala)")]
    [SerializeField] private float introPromptDelay = 4f;
    [SerializeField] private float promptDelay = 0.6f;
    [SerializeField] private int shotsBeforeSwitchPrompt = 3;

    [Header("Textos ({0} = boton)")]
    [SerializeField, TextArea(2, 5)] private string grabText =
        "Agarra el arma de tu funda con el botón de agarre lateral.";
    [SerializeField, TextArea(2, 5)] private string shootText =
        "Apunta a la basura y dispara con el gatillo derecho.";
    [SerializeField, TextArea(2, 5)] private string switchText =
        "Pulsa {0} para cambiar el tipo de disparo.\nCada tipo destruye una sola clase de basura:\nplástico, vidrio, orgánico o metal.";
    [SerializeField, TextArea(2, 5)] private string rechargeText =
        "Te quedaste sin energía.\nAgita el arma para recargarla.";
    [SerializeField, TextArea(2, 5)] private string storeText =
        "Zona limpia.\nGuarda el arma en la funda para continuar.";
    [SerializeField] private string continueText = "Pulsa {0} para continuar";

    private bool isShowing = false;
    private bool weaponGrabbed = false;
    private bool switchShown = false;
    private bool rechargeShown = false;
    private bool storeShown = false;
    private int shotsFired = 0;

    public bool IsPaused => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Start()
    {
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null) holster.OnWeaponRemoved += OnWeaponRemoved;
        }
        if (trashGun != null) trashGun.OnShotFired += OnShotFired;
        if (energySystem != null) energySystem.OnDepleted += OnEnergyDepleted;
    }

    private void OnDestroy()
    {
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null) holster.OnWeaponRemoved -= OnWeaponRemoved;
        }
        if (trashGun != null) trashGun.OnShotFired -= OnShotFired;
        if (energySystem != null) energySystem.OnDepleted -= OnEnergyDepleted;

        // Nunca dejar el juego congelado si se cambia de escena con un aviso abierto
        if (isShowing) Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────
    //  EVENTOS DEL JUEGO
    // ─────────────────────────────────────────

    public void OnIntroFinished()
    {
        Show(Step.Grab, grabText, introPromptDelay);
    }

    public void OnRoundCleared()
    {
        if (storeShown) return;
        storeShown = true;
        Show(Step.Store, storeText, promptDelay);
    }

    private void OnWeaponRemoved()
    {
        if (weaponGrabbed) return;
        weaponGrabbed = true;
        Show(Step.Shoot, shootText, promptDelay);
    }

    private void OnShotFired()
    {
        if (switchShown) return;
        shotsFired++;
        if (shotsFired < shotsBeforeSwitchPrompt) return;

        switchShown = true;
        string button = trashGun != null ? ButtonName(trashGun.ModeSwitchButton) : "A";
        Show(Step.SwitchMode, string.Format(switchText, button), promptDelay);
    }

    private void OnEnergyDepleted()
    {
        if (rechargeShown) return;
        rechargeShown = true;
        Show(Step.Recharge, rechargeText, promptDelay);
    }

    // ─────────────────────────────────────────
    //  AVISO + PAUSA
    // ─────────────────────────────────────────

    private void Show(Step step, string text, float delay)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (panelRoot == null || promptText == null) return;
        StartCoroutine(ShowRoutine(step, text, delay));
    }

    private IEnumerator ShowRoutine(Step step, string text, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Si ya hay un aviso abierto, este espera su turno
        while (isShowing)
            yield return null;

        // El jugador se adelanto y ya tiene el arma: este aviso sobra
        if (step == Step.Grab && weaponGrabbed) yield break;

        isShowing = true;
        Time.timeScale = 0f;
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);

        promptText.text = text + "\n\n<size=70%>" + string.Format(continueText, ButtonName(continueButton)) + "</size>";
        PlacePanelInFrontOfHead();
        panelRoot.SetActive(true);

        yield return new WaitForSecondsRealtime(minDisplayTime);

        while (!OVRInput.GetDown(continueButton))
            yield return null;

        panelRoot.SetActive(false);
        Time.timeScale = 1f;
        isShowing = false;
    }

    private void PlacePanelInFrontOfHead()
    {
        if (head == null) return;

        Vector3 forward = head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Transform panel = panelRoot.transform;
        panel.position = head.position + forward * panelDistance + Vector3.up * panelHeightOffset;
        panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private static string ButtonName(OVRInput.Button button)
    {
        switch (button)
        {
            case OVRInput.Button.One: return "A";
            case OVRInput.Button.Two: return "B";
            case OVRInput.Button.Three: return "X";
            case OVRInput.Button.Four: return "Y";
            default: return button.ToString();
        }
    }
}
