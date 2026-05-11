using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Pistola de plasma para el juego de limpieza submarina.
/// Dispara esferas de plasma que desintegran objetos metálicos al contacto.
/// </summary>
public class PlasmaGun : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Grabbable _grabbable;
    [SerializeField] private Transform _muzzlePoint;

    [Header("Disparo")]
    [SerializeField] private PlasmaBall _plasmaBallPrefab;
    [SerializeField] private float _fireRate = 0.4f;
    [SerializeField] private float _ballSpeed = 12f;

    [Header("Input")]
    [SerializeField] private float _triggerThreshold = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fireClip;

    // Cache de todos los colliders del arma para que la bala los ignore al spawnar
    private Collider[] _ownColliders;
    private float _nextFireTime;
    private bool _triggerWasPressed;

    // ?????????????????????????????????????????????
    #region Unity Callbacks

    private void Reset()
    {
        _grabbable = GetComponent<Grabbable>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        // Cacheamos todos los colliders del arma (incluyendo hijos) una sola vez
        _ownColliders = GetComponentsInChildren<Collider>();
        Debug.Log($"[PlasmaGun] Colliders cacheados: {_ownColliders.Length}");
    }

    private void Update()
    {
        if (!IsGrabbed()) return;

        bool triggerPressed = IsIndexTriggerPressed();

        if (triggerPressed && !_triggerWasPressed)
            TryFire();

        _triggerWasPressed = triggerPressed;
    }

    private void OnDrawGizmosSelected()
    {
        if (_muzzlePoint == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(_muzzlePoint.position, _muzzlePoint.forward * 2f);
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Fire Logic

    private void TryFire()
    {
        if (Time.time < _nextFireTime) return;
        if (_plasmaBallPrefab == null || _muzzlePoint == null)
        {
            Debug.LogWarning("[PlasmaGun] Falta asignar PlasmaBallPrefab o MuzzlePoint en el Inspector.");
            return;
        }

        _nextFireTime = Time.time + _fireRate;

        SpawnPlasmaBall();
        PlayFireSound();
    }

    private void SpawnPlasmaBall()
    {
        PlasmaBall ball = Instantiate(_plasmaBallPrefab, _muzzlePoint.position, _muzzlePoint.rotation);

        // Le pasamos nuestros colliders para que los ignore al nacer
        ball.Initialize(_ballSpeed, _ownColliders);
    }

    private void PlayFireSound()
    {
        if (_audioSource == null || _fireClip == null) return;
        _audioSource.PlayOneShot(_fireClip);
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Input

    private bool IsGrabbed() =>
        _grabbable != null && _grabbable.SelectingPointsCount > 0;

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > _triggerThreshold || right > _triggerThreshold;
    }

    #endregion
}