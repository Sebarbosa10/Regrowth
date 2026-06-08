using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class TrashGun : MonoBehaviour, IUpdatable
{
    public enum FireMode
    {
        Single = 0,  // Plastic
        Burst = 1,  // Glass
        Auto = 2,  // Organic
        Spread = 3   // Metal
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

    [Header("Laser Sight")]
    [SerializeField] private LineRenderer laserSight;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.002f;

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

    [Header("Mode Switch Button")]
    [SerializeField] private OVRInput.Button modeSwitchButton = OVRInput.Button.One;

    [Header("Color")]
    [SerializeField] private GunModeColorizer colorizer;

    [Header("Two-Handed Grip")]
    [Tooltip("Referencia al script de dos manos. Si no está asignado, dispara con cualquier mano.")]
    [SerializeField] private TwoHandedGunGrip twoHandedGrip;

    [Header("Haptics — Fire (uno por modo)")]
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
        SetupLaser();
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
        if (laserSight != null) laserSight.enabled = false;
    }

    public void Tick(float deltaTime)
    {
        if (grabbable == null || muzzle == null)
            return;

        bool held = grabbable.SelectingPointsCount > 0;

        UpdateLaser(held);

        if (!held) return;

        DetectActiveController();
        HandleModeSwitch();
        HandleFire();
    }

    // ─────────────────────────────────────────
    //  LASER SIGHT
    // ─────────────────────────────────────────

    private void SetupLaser()
    {
        if (laserSight == null) return;
        laserSight.positionCount = 2;
        laserSight.startWidth = laserWidth;
        laserSight.endWidth = laserWidth;
        laserSight.startColor = laserColor;
        laserSight.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        laserSight.useWorldSpace = true;
    }

    private void UpdateLaser(bool held)
    {
        if (laserSight == null) return;

        laserSight.enabled = held;
        if (!held) return;

        Vector3 start = muzzle.position;
        Vector3 end;

        if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, range, shootableLayer))
            end = hit.point;
        else
            end = muzzle.position + muzzle.forward * range;

        laserSight.SetPosition(0, start);
        laserSight.SetPosition(1, end);
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
        bool switchPressed = OVRInput.Get(modeSwitchButton);

        if (switchPressed && !switchWasPressed)
        {
            int next = ((int)currentMode + 1) % modeTargetTags.Length;
            currentMode = (FireMode)next;

            if (audioSource != null && modeSwitchSound != null)
                audioSource.PlayOneShot(modeSwitchSound);

            colorizer?.SetMode((int)currentMode);
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
        // Solo dispara si la mano del mango (derecha) está activa
        if (!IsMainHandHoldingGun()) return;

        bool triggerPressed = IsRightTriggerPressed();

        switch (currentMode)
        {
            case FireMode.Single:
                if (triggerPressed && !triggerWasPressed && CanFire())
                {
                    FireSingle(muzzle.forward);
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Burst:
                if (triggerPressed && !triggerWasPressed && CanFire() && !isBursting)
                {
                    StartCoroutine(FireBurst());
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Auto:
                if (triggerPressed && CanFire())
                {
                    FireSingle(muzzle.forward);
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Spread:
                if (triggerPressed && !triggerWasPressed && CanFire())
                {
                    FireSpread();
                    lastFireTime = Time.time;
                }
                break;
        }

        triggerWasPressed = triggerPressed;
    }

    private bool CanFire() => Time.time >= lastFireTime + fireRate;

    /// <summary>
    /// Devuelve true si la mano derecha tiene el mango agarrado.
    /// Si no hay TwoHandedGrip asignado, siempre devuelve true (comportamiento original).
    /// </summary>
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
    }

    private IEnumerator FireBurst()
    {
        isBursting = true;
        for (int i = 0; i < burstCount; i++)
        {
            PlayShootSound();
            Hitscan(muzzle.position, muzzle.forward);
            VibrateForMode();
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
    }

    // ─────────────────────────────────────────
    //  HITSCAN CORE
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

    /// <summary>Solo el trigger derecho dispara — la mano del mango.</summary>
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