using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float range = 20f;
    [SerializeField] private float triggerThreshold = 0.7f;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float autoFireRate = 0.08f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.08f;
    [SerializeField] private int spreadCount = 5;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private LayerMask shootableLayer;
    [SerializeField] private LineRenderer tracePrefab;
    [SerializeField] private float traceDuration = 0.08f;
    [SerializeField] private float traceWidth = 0.005f;
    [SerializeField] private Color traceColor = Color.yellow;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip modeSwitchSound;
    [SerializeField] private AudioClip noEnergySound;
    [SerializeField] private OVRInput.Button modeSwitchButton = OVRInput.Button.One;
    [SerializeField] private GunModeColorizer colorizer;
    [SerializeField] private TwoHandedGunGrip twoHandedGrip;
    [SerializeField] private GunEnergySystem energySystem;
    [SerializeField] private GunRecoil recoil;
    [SerializeField] private HapticProfile[] hapticProfiles = new HapticProfile[]
    {
        new HapticProfile { modeName = "Single", frequency = 0.8f, amplitude = 0.6f, duration = 0.08f },
        new HapticProfile { modeName = "Burst",  frequency = 0.5f, amplitude = 0.9f, duration = 0.06f },
        new HapticProfile { modeName = "Auto",   frequency = 1.0f, amplitude = 0.4f, duration = 0.05f },
        new HapticProfile { modeName = "Spread", frequency = 0.3f, amplitude = 1.0f, duration = 0.15f },
    };
    [SerializeField] private HapticProfile modeSwitchHaptic = new HapticProfile
        { modeName = "Switch", frequency = 0.2f, amplitude = 0.5f, duration = 0.12f };
    [SerializeField] private FireMode currentMode = FireMode.Single;
    [SerializeField] private CustomUpdateManager updateManager;

    private static readonly string[] modeTargetTags = { "Plastic", "Glass", "Organic", "Metal" };

    private float lastFireTime = -999f;
    private bool triggerWasPressed = false;
    private bool switchWasPressed = false;
    private bool isBursting = false;
    private OVRInput.Controller activeController = OVRInput.Controller.None;
    private readonly Queue<LineRenderer> tracePool = new Queue<LineRenderer>();

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
        if (grabbable.SelectingPointsCount <= 0) return;

        DetectActiveController();

        if (NarrativeBeatManager.Instance != null && NarrativeBeatManager.Instance.IsPlaying) return;

        HandleModeSwitch();
        HandleFire();
    }

    private void SpawnTrace(Vector3 from, Vector3 to)
    {
        if (tracePrefab == null) return;

        LineRenderer trace;
        if (tracePool.Count > 0)
        {
            trace = tracePool.Dequeue();
            trace.gameObject.SetActive(true);
        }
        else
        {
            trace = Instantiate(tracePrefab, Vector3.zero, Quaternion.identity);
            trace.useWorldSpace = true;
            trace.positionCount = 2;
        }

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
        float invDuration = 1f / traceDuration;
        while (t < traceDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t * invDuration);
            trace.startColor = new Color(startA.r, startA.g, startA.b, alpha);
            trace.endColor = new Color(endA.r, endA.g, endA.b, alpha * 0.3f);
            yield return null;
        }
        trace.gameObject.SetActive(false);
        tracePool.Enqueue(trace);
    }

    private void DetectActiveController()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        if (right >= left && right > 0.01f)
            activeController = OVRInput.Controller.RTouch;
        else if (left > 0.01f)
            activeController = OVRInput.Controller.LTouch;
    }

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
            currentMode = (FireMode)(((int)currentMode + 1) % modeTargetTags.Length);

            if (audioSource != null && modeSwitchSound != null)
                audioSource.PlayOneShot(modeSwitchSound);

            colorizer?.SetMode((int)currentMode);
            energySystem?.SetCurrentMode((int)currentMode, colorizer?.GetCurrentMaterial());
            Vibrate(modeSwitchHaptic);
        }

        switchWasPressed = switchPressed;
    }

    private void HandleFire()
    {
        if (!IsMainHandHoldingGun()) return;

        bool triggerPressed = IsRightTriggerPressed();

        switch (currentMode)
        {
            case FireMode.Single:
                if (triggerPressed && !triggerWasPressed && CanFire() && TryConsumeEnergy())
                {
                    FireSingle(muzzle.forward);
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Burst:
                if (triggerPressed && !triggerWasPressed && CanFire() && !isBursting && TryConsumeEnergy())
                {
                    StartCoroutine(FireBurst());
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Auto:
                if (triggerPressed && CanFire() && TryConsumeEnergy())
                {
                    FireSingle(muzzle.forward);
                    lastFireTime = Time.time;
                }
                break;

            case FireMode.Spread:
                if (triggerPressed && !triggerWasPressed && CanFire() && TryConsumeEnergy())
                {
                    FireSpread();
                    lastFireTime = Time.time;
                }
                break;
        }

        triggerWasPressed = triggerPressed;
    }

    private bool CanFire()
    {
        float rate = currentMode == FireMode.Auto ? autoFireRate : fireRate;
        return Time.time >= lastFireTime + rate;
    }

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
            Vector3 dir = Quaternion.Euler(0f, -halfAngle + step * i, 0f) * muzzle.forward;
            Hitscan(muzzle.position, dir);
        }

        VibrateForMode();
        recoil?.ApplyRecoil((int)currentMode);
    }

    private void Hitscan(Vector3 origin, Vector3 direction)
    {
        string targetTag = modeTargetTags[(int)currentMode];
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            endPoint = hit.point;
            if (hit.collider.CompareTag(targetTag))
            {
                if (impactSound != null) AudioSource.PlayClipAtPoint(impactSound, hit.point);
                Destroy(hit.collider.gameObject);
            }
        }

        SpawnTrace(origin, endPoint);
    }

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
            ? activeController : OVRInput.Controller.RTouch;

        OVRInput.SetControllerVibration(profile.frequency, profile.amplitude, controller);
        StartCoroutine(StopVibration(profile.duration, controller));
    }

    private IEnumerator StopVibration(float delay, OVRInput.Controller controller)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    private bool IsRightTriggerPressed()
    {
        return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > triggerThreshold;
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
