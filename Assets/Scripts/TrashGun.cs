using System.Collections;
using UnityEngine;
using Oculus.Interaction;


public class TrashGun : MonoBehaviour, IUpdatable
{
    public enum FireMode
    {
        Single = 0,
        Burst = 1,
        Auto = 2,
        Spread = 3
    }

    [System.Serializable]
    public class HapticProfile
    {
        public string modeName;
        [Range(0f, 1f)] public float frequency = 0.5f;
        [Range(0f, 1f)] public float amplitude = 0.5f;
        public float duration = 0.1f;
    }

    [Header("References")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform muzzle;

    [Header("Hitscan Settings")]
    [SerializeField] private float range = 20f;
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Fire Settings")]
    [SerializeField] private float fireRate = 0.3f;

    [Header("— Burst Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.08f;

    [Header("— Spread Settings")]
    [SerializeField] private int spreadCount = 5;
    [SerializeField] private float spreadAngle = 15f;

    [Header("Layer")]
    [SerializeField] private LayerMask shootableLayer;

    [Header("Bullet Trace")]
    [SerializeField] private LineRenderer tracePrefab;
    [SerializeField] private float traceDuration = 0.08f;
    [SerializeField] private float traceWidth = 0.005f;
    [SerializeField] private Color traceColor = Color.yellow;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip modeSwitchSound;
    [SerializeField] private AudioClip noEnergySound;

    [Header("Mode Switch Button")]
    [SerializeField] private OVRInput.Button modeSwitchButton = OVRInput.Button.One;

    [Header("Color")]
    [SerializeField] private GunModeColorizer colorizer;

    [Header("Two-Handed Grip")]
    [SerializeField] private TwoHandedGunGrip twoHandedGrip;

    [Header("Energy")]
    [SerializeField] private GunEnergySystem energySystem;

    [Header("Recoil")]
    [SerializeField] private GunRecoil recoil;

    [Header("Haptics — Fire")]
    [SerializeField]
    private HapticProfile[] hapticProfiles = new HapticProfile[]
    {
        new HapticProfile { modeName = "Single", frequency = 0.8f, amplitude = 0.6f, duration = 0.08f },
        new HapticProfile { modeName = "Burst",  frequency = 0.5f, amplitude = 0.9f, duration = 0.06f },
        new HapticProfile { modeName = "Auto",   frequency = 1.0f, amplitude = 0.4f, duration = 0.05f },
        new HapticProfile { modeName = "Spread", frequency = 0.3f, amplitude = 1.0f, duration = 0.15f },
    };

    [Header("Haptics — Mode Switch")]
    [SerializeField]
    private HapticProfile modeSwitchHaptic = new HapticProfile
    { modeName = "Switch", frequency = 0.2f, amplitude = 0.5f, duration = 0.12f };

    [Header("Debug")]
    [SerializeField] private FireMode currentMode = FireMode.Single;

    private static readonly string[] modeTargetTags = { "Plastic", "Glass", "Organic", "Metal" };

    private float lastFireTime = -999f;
    private bool triggerWasPressed = false;
    private bool switchWasPressed = false;
    private bool isBursting = false;
    private OVRInput.Controller activeController = OVRInput.Controller.None;

    [SerializeField] private CustomUpdateManager updateManager;

    private void Reset() { grabbable = GetComponent<Grabbable>(); }

    private void OnEnable()
    {
        if (updateManager != null) updateManager.Register(this);
        colorizer?.SetMode((int)currentMode);
        energySystem?.SetCurrentMode((int)currentMode, colorizer?.GetCurrentMaterial());
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        if (grabbable == null || muzzle == null) return;

        bool held = grabbable.SelectingPointsCount > 0;
        if (!held) return;

        DetectActiveController();
        HandleModeSwitch();
        HandleFire();
    }

    // ─────────────────────────────────────────
    //  BULLET TRACE
    // ─────────────────────────────────────────

    private void SpawnTrace(Vector3 from, Vector3 to)
    {
        if (tracePrefab == null) return;
        LineRenderer trace = Instantiate(tracePrefab, Vector3.zero, Quaternion.identity);
        trace.useWorldSpace = true;
        trace.positionCount = 2;
        trace.startWidth = traceWidth;
        trace.endWidth = traceWidth * 0.3f;
        trace.startColor = traceColor;
        trace.endColor = new Color(traceColor.r, traceColor.g, traceColor.b, 0f);
        trace.SetPosition(0, from);
        trace.SetPosition(1, to);
        StartCoroutine(FadeTrace(trace));
    }

    private IEnumerator FadeTrace(LineRenderer trace)
    {
        float t = 0f;
        Color startA = trace.startColor;
        Color endA = trace.endColor;
        while (t < traceDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t / traceDuration);
            trace.startColor = new Color(startA.r, startA.g, startA.b, alpha);
            trace.endColor = new Color(endA.r, endA.g, endA.b, alpha * 0.3f);
            yield return null;
        }
        Destroy(trace.gameObject);
    }

    // ─────────────────────────────────────────
    //  CONTROLLER DETECTION
    // ─────────────────────────────────────────

    private void DetectActiveController()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        if (right >= left && right > 0.01f)
            activeController = OVRInput.Controller.RTouch;
        else if (left > 0.01f)
            activeController = OVRInput.Controller.LTouch;
    }

    // ─────────────────────────────────────────
    //  MODE SWITCH
    // ─────────────────────────────────────────

    private void HandleModeSwitch()
    {
        if (energySystem != null && energySystem.IsDepleted)
        {
            switchWasPressed = OVRInput.Get(modeSwitchButton);
            return;
        }

        bool switchPressed = OVRInput.Get(modeSwitchButton);

        if (switchPressed && !switchWasPressed)
        {
            int next = ((int)currentMode + 1) % modeTargetTags.Length;
            currentMode = (FireMode)next;

            if (audioSource != null && modeSwitchSound != null)
                audioSource.PlayOneShot(modeSwitchSound);

            colorizer?.SetMode((int)currentMode);
            energySystem?.SetCurrentMode((int)currentMode, colorizer?.GetCurrentMaterial());
            Vibrate(modeSwitchHaptic);

            Debug.Log($"[TrashGun] Modo: {currentMode} -> Tag: {modeTargetTags[(int)currentMode]}");
        }

        switchWasPressed = switchPressed;
    }

    // ─────────────────────────────────────────
    //  FIRE ROUTING
    // ─────────────────────────────────────────

    private void HandleFire()
    {
        if (!IsMainHandHoldingGun()) return;

        bool triggerPressed = IsRightTriggerPressed();

        switch (currentMode)
        {
            case FireMode.Single:
                if (triggerPressed && !triggerWasPressed && CanFire())
                {
                    if (TryConsumeEnergy())
                    {
                        FireSingle(muzzle.forward);
                        lastFireTime = Time.time;
                    }
                }
                break;

            case FireMode.Burst:
                if (triggerPressed && !triggerWasPressed && CanFire() && !isBursting)
                {
                    if (TryConsumeEnergy())
                    {
                        StartCoroutine(FireBurst());
                        lastFireTime = Time.time;
                    }
                }
                break;

            case FireMode.Auto:
                if (triggerPressed && CanFire())
                {
                    if (TryConsumeEnergy())
                    {
                        FireSingle(muzzle.forward);
                        lastFireTime = Time.time;
                    }
                }
                break;

            case FireMode.Spread:
                if (triggerPressed && !triggerWasPressed && CanFire())
                {
                    if (TryConsumeEnergy())
                    {
                        FireSpread();
                        lastFireTime = Time.time;
                    }
                }
                break;
        }

        triggerWasPressed = triggerPressed;
    }

    private bool CanFire() => Time.time >= lastFireTime + fireRate;

    private bool TryConsumeEnergy()
    {
        if (energySystem == null) return true;

        bool canShoot = energySystem.TryShoot();

        if (!canShoot && audioSource != null && noEnergySound != null)
            audioSource.PlayOneShot(noEnergySound);

        return canShoot;
    }

    private bool IsMainHandHoldingGun()
    {
        if (twoHandedGrip == null) return true;
        return twoHandedGrip.IsMainHandActive;
    }

    // ─────────────────────────────────────────
    //  FIRE MODES
    // ─────────────────────────────────────────

    private void FireSingle(Vector3 direction)
    {
        PlayShootSound();
        Hitscan(muzzle.position, direction);
        VibrateForMode();
        recoil?.ApplyRecoil((int)currentMode);
    }

    private IEnumerator FireBurst()
    {
        isBursting = true;
        for (int i = 0; i < burstCount; i++)
        {
            PlayShootSound();
            Hitscan(muzzle.position, muzzle.forward);
            VibrateForMode();
            recoil?.ApplyRecoil((int)currentMode);
            yield return new WaitForSeconds(burstDelay);
        }
        isBursting = false;
    }

    private void FireSpread()
    {
        PlayShootSound();
        float halfAngle = spreadAngle / 2f;
        float step = spreadCount > 1 ? spreadAngle / (spreadCount - 1) : 0f;

        for (int i = 0; i < spreadCount; i++)
        {
            float yaw = -halfAngle + step * i;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * muzzle.forward;
            Hitscan(muzzle.position, dir);
        }

        VibrateForMode();
        recoil?.ApplyRecoil((int)currentMode);
    }

    // ─────────────────────────────────────────
    //  HITSCAN
    // ─────────────────────────────────────────

    private void Hitscan(Vector3 origin, Vector3 direction)
    {
        string targetTag = modeTargetTags[(int)currentMode];
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            endPoint = hit.point;

            if (hit.collider.CompareTag(targetTag))
            {
                if (impactSound != null)
                    AudioSource.PlayClipAtPoint(impactSound, hit.point);

                if (TrashDiscoveryManager.Instance != null)
                    TrashDiscoveryManager.Instance.OnTrashCollected(hit.collider.gameObject);

                Destroy(hit.collider.gameObject);
            }
        }

        SpawnTrace(origin, endPoint);
    }

    // ─────────────────────────────────────────
    //  HAPTICS
    // ─────────────────────────────────────────

    private void VibrateForMode()
    {
        int index = (int)currentMode;
        if (hapticProfiles == null || index >= hapticProfiles.Length) return;
        Vibrate(hapticProfiles[index]);
    }

    private void Vibrate(HapticProfile profile)
    {
        if (profile == null) return;
        OVRInput.Controller controller = activeController != OVRInput.Controller.None
            ? activeController
            : OVRInput.Controller.RTouch;

        OVRInput.SetControllerVibration(profile.frequency, profile.amplitude, controller);
        StartCoroutine(StopVibration(profile.duration, controller));
    }

    private IEnumerator StopVibration(float delay, OVRInput.Controller controller)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    private bool IsRightTriggerPressed()
    {
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return right > triggerThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(muzzle.position, muzzle.forward * range);

        if (currentMode == FireMode.Spread)
        {
            Gizmos.color = Color.red;
            float half = spreadAngle / 2f;
            Gizmos.DrawRay(muzzle.position, Quaternion.Euler(0, half, 0) * muzzle.forward * range);
            Gizmos.DrawRay(muzzle.position, Quaternion.Euler(0, -half, 0) * muzzle.forward * range);
        }
    }
}