using UnityEngine;
using Oculus.Interaction;

public class GunEnergySystem : MonoBehaviour, IUpdatable
{
    [SerializeField] private int maxShots = 10;
    [SerializeField] private float rechargeShakeTime = 3f;
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private GunModeColorizer colorizer;
    [SerializeField] private Material depletedMaterial;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip depletedSound;
    [SerializeField] private AudioClip rechargedSound;
    [SerializeField] private float rechargeHapticFrequency = 0.5f;
    [SerializeField] private float rechargeHapticAmplitude = 0.8f;
    [SerializeField] private CustomUpdateManager updateManager;
    [SerializeField] private Grabbable grabbable;

    private int shotsRemaining;
    private bool isDepleted = false;
    private float shakeTimer = 0f;
    private Vector3 lastControllerPos;
    private int currentModeIndex = 0;
    private Material lastChargedMaterial;
    private OVRInput.Controller activeShakeController = OVRInput.Controller.None;

    public bool IsDepleted => isDepleted;

    private void OnEnable()
    {
        if (updateManager != null) updateManager.Register(this);
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
    }

    private void Start()
    {
        shotsRemaining = maxShots;
        lastControllerPos = GetControllerPosition();
    }

    public void Tick(float deltaTime)
    {
        if (!isDepleted) return;

        if (grabbable != null && grabbable.SelectingPointsCount <= 0)
        {
            shakeTimer = 0f;
            return;
        }

        Vector3 currentPos = GetControllerPosition();
        float velocity = (currentPos - lastControllerPos).magnitude / deltaTime;
        lastControllerPos = currentPos;

        if (velocity > shakeThreshold)
        {
            shakeTimer += deltaTime;

            if (activeShakeController != OVRInput.Controller.None)
            {
                OVRInput.SetControllerVibration(
                    rechargeHapticFrequency,
                    rechargeHapticAmplitude * (shakeTimer / rechargeShakeTime),
                    activeShakeController
                );
            }

            if (shakeTimer >= rechargeShakeTime)
                Recharge();
        }
        else
        {
            shakeTimer = 0f;
            // Frenamos vibración en ambos controles por las dudas de que se haya
            // soltado justo cuando se estaba por cambiar de mano.
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        }
    }

    public bool TryShoot()
    {
        if (isDepleted) return false;

        shotsRemaining--;
        UpdateEnergyVisual();

        if (shotsRemaining <= 0)
        {
            Deplete();
            return false;
        }

        return true;
    }

    public void SetCurrentMode(int modeIndex, Material chargedMaterial)
    {
        currentModeIndex = modeIndex;
        lastChargedMaterial = chargedMaterial;
    }

    private void Deplete()
    {
        isDepleted = true;
        shakeTimer = 0f;
        colorizer?.SetDepletedMaterial(depletedMaterial);
        if (audioSource != null && depletedSound != null)
            audioSource.PlayOneShot(depletedSound);
    }

    private void Recharge()
    {
        isDepleted = false;
        shotsRemaining = maxShots;
        shakeTimer = 0f;
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        colorizer?.SetMode(currentModeIndex);
        if (audioSource != null && rechargedSound != null)
            audioSource.PlayOneShot(rechargedSound);
    }

    private void UpdateEnergyVisual()
    {
        if (colorizer == null || depletedMaterial == null) return;
        float t = 1f - ((float)shotsRemaining / maxShots);
        colorizer.SetEnergyLerp(t, depletedMaterial);
    }

    private Vector3 GetControllerPosition()
    {
        float rightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);

        if (rightGrip > 0.5f)
        {
            activeShakeController = OVRInput.Controller.RTouch;
            return OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        }
        if (leftGrip > 0.5f)
        {
            activeShakeController = OVRInput.Controller.LTouch;
            return OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        }

        activeShakeController = OVRInput.Controller.None;
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isDepleted) return;
        float progress = shakeTimer / rechargeShakeTime;
        Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
        Gizmos.DrawWireSphere(transform.position, 0.1f + progress * 0.1f);
    }
}