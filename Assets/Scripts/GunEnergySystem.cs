using UnityEngine;
using Oculus.Interaction;

public class GunEnergySystem : MonoBehaviour, IUpdatable
{
    [SerializeField] private int maxShots = 10;
    [SerializeField] private float rechargeShakeTime = 3f;
    [Tooltip("Velocidad del mando (m/s) a partir de la cual cuenta como agitar")]
    [SerializeField] private float shakeThreshold = 1.5f;
    [Tooltip("Velocidad de giro de muneca (grados/s) a partir de la cual cuenta como agitar")]
    [SerializeField] private float shakeAngularThreshold = 180f;
    [Tooltip("Tiempo que se puede frenar (cambios de direccion) sin perder progreso")]
    [SerializeField] private float shakeGraceTime = 0.3f;
    [Tooltip("Segundos de progreso que se pierden por segundo al dejar de agitar")]
    [SerializeField] private float shakeDecayRate = 1f;
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
    private float slowTimer = 0f;
    private float smoothedIntensity = 0f;
    private Vector3 lastControllerPos;
    private Quaternion lastControllerRot = Quaternion.identity;
    private OVRInput.Controller shakeController = OVRInput.Controller.None;
    private int currentModeIndex = 0;
    private Material lastChargedMaterial;

    public bool IsDepleted => isDepleted;

    public event System.Action OnDepleted;

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
    }

    public void Tick(float deltaTime)
    {
        if (!isDepleted) return;

        // Juego en pausa (tutorial): sin deltaTime no se puede medir la agitacion
        if (deltaTime <= 0f) return;

        if (grabbable != null && grabbable.SelectingPointsCount <= 0)
        {
            ResetShake();
            return;
        }

        OVRInput.Controller controller = GetHoldingController();
        Vector3 currentPos = OVRInput.GetLocalControllerPosition(controller);
        Quaternion currentRot = OVRInput.GetLocalControllerRotation(controller);

        // Primera lectura (o cambio de mano): no hay pose anterior valida con la que comparar
        if (controller != shakeController)
        {
            shakeController = controller;
            lastControllerPos = currentPos;
            lastControllerRot = currentRot;
            smoothedIntensity = 0f;
            return;
        }

        float linearSpeed = (currentPos - lastControllerPos).magnitude / deltaTime;
        float angularSpeed = Quaternion.Angle(lastControllerRot, currentRot) / deltaTime;
        lastControllerPos = currentPos;
        lastControllerRot = currentRot;

        // Cuenta cualquier movimiento rapido: desplazar el mando o girar la muneca, en cualquier direccion
        float intensity = Mathf.Max(
            linearSpeed / Mathf.Max(shakeThreshold, 0.001f),
            angularSpeed / Mathf.Max(shakeAngularThreshold, 0.001f));
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, intensity, 1f - Mathf.Exp(-12f * deltaTime));

        if (smoothedIntensity >= 1f)
        {
            slowTimer = 0f;
            shakeTimer += deltaTime;
            OVRInput.SetControllerVibration(
                rechargeHapticFrequency,
                rechargeHapticAmplitude * (shakeTimer / rechargeShakeTime),
                controller
            );

            if (shakeTimer >= rechargeShakeTime)
                Recharge();
        }
        else
        {
            // Al agitar el mando se frena en cada cambio de direccion: se da un margen
            // antes de empezar a perder progreso, y se pierde poco a poco, no de golpe.
            slowTimer += deltaTime;
            if (slowTimer > shakeGraceTime)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - shakeDecayRate * deltaTime);
                OVRInput.SetControllerVibration(0f, 0f, controller);
            }
        }
    }

    private void ResetShake()
    {
        if (shakeController != OVRInput.Controller.None)
            OVRInput.SetControllerVibration(0f, 0f, shakeController);

        shakeTimer = 0f;
        slowTimer = 0f;
        smoothedIntensity = 0f;
        shakeController = OVRInput.Controller.None;
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
        ResetShake();
        colorizer?.SetDepletedMaterial(depletedMaterial);
        if (audioSource != null && depletedSound != null)
            audioSource.PlayOneShot(depletedSound);
        OnDepleted?.Invoke();
    }

    private void Recharge()
    {
        isDepleted = false;
        shotsRemaining = maxShots;
        ResetShake();
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

    // Mando de la mano que sostiene el arma (derecha por defecto)
    private OVRInput.Controller GetHoldingController()
    {
        float rightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);

        if (rightGrip > 0.5f) return OVRInput.Controller.RTouch;
        if (leftGrip > 0.5f) return OVRInput.Controller.LTouch;
        return OVRInput.Controller.RTouch;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isDepleted) return;
        float progress = shakeTimer / rechargeShakeTime;
        Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
        Gizmos.DrawWireSphere(transform.position, 0.1f + progress * 0.1f);
    }
}
