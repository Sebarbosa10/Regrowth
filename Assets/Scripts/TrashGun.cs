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

    [Header("References")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform muzzle;

    [Header("Bullet Prefabs (uno por modo)")]
    [SerializeField] private GameObject bulletSingle;   // Plastic
    [SerializeField] private GameObject bulletBurst;    // Glass
    [SerializeField] private GameObject bulletAuto;     // Organic
    [SerializeField] private GameObject bulletSpread;   // Metal

    [Header("Bullet Settings")]
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 3f;

    [Header("Fire Settings")]
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("— Burst Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.08f;

    [Header("— Spread Settings")]
    [SerializeField] private int spreadCount = 5;
    [SerializeField] private float spreadAngle = 15f;

    [Header("Layer")]
    [SerializeField] private LayerMask shootableLayer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip modeSwitchSound;

    [Header("Mode Switch Button")]
    [SerializeField] private OVRInput.Button modeSwitchButton = OVRInput.Button.One;

    [Header("Debug")]
    [SerializeField] private FireMode currentMode = FireMode.Single;

    // Mapeo fijo modo → tag
    private static readonly string[] modeTargetTags = { "Plastic", "Glass", "Organic", "Metal" };

    private float lastFireTime = -999f;
    private bool triggerWasPressed = false;
    private bool switchWasPressed = false;
    private bool isBursting = false;

    [SerializeField] private CustomUpdateManager updateManager;

    private void Reset() { grabbable = GetComponent<Grabbable>(); }

    private void OnEnable()
    {
        if (updateManager != null) updateManager.Register(this);
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        if (grabbable == null || muzzle == null)
            return;

        if (grabbable.SelectingPointsCount <= 0)
            return;

        HandleModeSwitch();
        HandleFire();
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

            Debug.Log($"[TrashGun] Modo: {currentMode} → Tag: {modeTargetTags[(int)currentMode]}");
        }

        switchWasPressed = switchPressed;
    }

    // ─────────────────────────────────────────
    //  FIRE ROUTING
    // ─────────────────────────────────────────

    private void HandleFire()
    {
        bool triggerPressed = IsIndexTriggerPressed();

        switch (currentMode)
        {
            case FireMode.Single:
                if (triggerPressed && !triggerWasPressed && CanFire())
                {
                    FireSingle();
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
                    FireSingle();
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

    // ─────────────────────────────────────────
    //  FIRE MODES
    // ─────────────────────────────────────────

    private void FireSingle()
    {
        PlayShootSound();
        SpawnBullet(muzzle.position, muzzle.rotation);
    }

    private IEnumerator FireBurst()
    {
        isBursting = true;
        for (int i = 0; i < burstCount; i++)
        {
            PlayShootSound();
            SpawnBullet(muzzle.position, muzzle.rotation);
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
            Quaternion spreadRot = muzzle.rotation * Quaternion.Euler(0f, yaw, 0f);
            SpawnBullet(muzzle.position, spreadRot);
        }
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    private void SpawnBullet(Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetCurrentPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[TrashGun] No hay prefab asignado para el modo {currentMode}");
            return;
        }

        GameObject bullet = Instantiate(prefab, position, rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = rotation * Vector3.forward * bulletSpeed;

        TrashBullet trashBullet = bullet.GetComponent<TrashBullet>();
        if (trashBullet != null)
        {
            trashBullet.SetTargetTag(modeTargetTags[(int)currentMode]);
            trashBullet.SetImpactSound(impactSound);
        }

        Destroy(bullet, bulletLifetime);
    }

    private GameObject GetCurrentPrefab()
    {
        switch (currentMode)
        {
            case FireMode.Single: return bulletSingle;
            case FireMode.Burst: return bulletBurst;
            case FireMode.Auto: return bulletAuto;
            case FireMode.Spread: return bulletSpread;
            default: return bulletSingle;
        }
    }

    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > triggerThreshold || right > triggerThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(muzzle.position, muzzle.forward * 2f);

        if (currentMode == FireMode.Spread)
        {
            Gizmos.color = Color.red;
            float half = spreadAngle / 2f;
            Gizmos.DrawRay(muzzle.position, Quaternion.Euler(0, half, 0) * muzzle.forward * 2f);
            Gizmos.DrawRay(muzzle.position, Quaternion.Euler(0, -half, 0) * muzzle.forward * 2f);
        }
    }
}