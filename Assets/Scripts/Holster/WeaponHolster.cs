using UnityEngine;

/// <summary>
/// Holster corporal VR. Se adjunta a un punto del cuerpo del jugador (pierna, espalda).
/// Detecta armas cercanas, las snappea al soltarlas y permite volver a agarrarlas.
///
/// SETUP EN UNITY:
///   1. Crear GameObject hijo del CameraRig (o del tracking de pierna/espalda)
///   2. Agregar este script
///   3. Crear un hijo vacío llamado "SnapPoint" ? posición y rotación donde queda el arma
///   4. Asignar _snapPoint con ese hijo
///   5. Elegir _slot (RightThigh, Back, etc.)
///   El HolsterManager se crea automáticamente, no hace falta nada manual.
/// </summary>
public class WeaponHolster : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform _snapPoint;
    [SerializeField] private float _snapRadius = 0.15f;
    [SerializeField] private HolsterSlot _slot;

    [Header("Feedback")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _snapInClip;
    [SerializeField] private AudioClip _snapOutClip;
    [SerializeField] private float _hapticAmplitude = 0.3f;
    [SerializeField] private float _hapticDuration = 0.1f;

    // Estado
    private HolsterableWeapon _storedWeapon;
    public bool IsOccupied => _storedWeapon != null;
    public HolsterSlot Slot => _slot;
    public Transform SnapPoint => _snapPoint;

    // ?????????????????????????????????????????????
    #region Unity Callbacks

    private void OnEnable()
    {
        EnsureManagerExists();
        HolsterManager.Instance.RegisterHolster(this, _snapPoint);
    }

    private void OnDisable()
    {
        HolsterManager.Instance?.UnregisterHolster(this, _snapPoint);
    }

    private void OnDrawGizmosSelected()
    {
        if (_snapPoint == null) return;
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(_snapPoint.position, _snapRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(_snapPoint.position, _snapPoint.forward * 0.1f);
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Public API

    /// <summary>
    /// Intenta guardar un arma. Retorna true si tuvo éxito.
    /// </summary>
    public bool TryStore(HolsterableWeapon weapon)
    {
        if (IsOccupied) return false;
        if (!IsWeaponCompatible(weapon)) return false;

        _storedWeapon = weapon;
        _storedWeapon.OnStoredInHolster(this);

        PlayAudio(_snapInClip);
        TriggerHaptic();

        Debug.Log($"[Holster:{_slot}] Guardada: {weapon.name}");
        return true;
    }

    /// <summary>
    /// El jugador agarra el arma del holster.
    /// </summary>
    public void OnWeaponGrabbed()
    {
        if (!IsOccupied) return;

        Debug.Log($"[Holster:{_slot}] Sacada: {_storedWeapon.name}");
        _storedWeapon = null;

        PlayAudio(_snapOutClip);
        TriggerHaptic();
    }

    /// <summary>
    /// Verifica si el arma está dentro del radio de snap.
    /// </summary>
    public bool IsWeaponInRange(HolsterableWeapon weapon)
    {
        if (_snapPoint == null) return false;
        float distSqr = (_snapPoint.position - weapon.transform.position).sqrMagnitude;
        return distSqr <= _snapRadius * _snapRadius;
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Private Helpers

    private bool IsWeaponCompatible(HolsterableWeapon weapon)
    {
        if (weapon.AllowedSlot == HolsterSlot.Any) return true;
        return weapon.AllowedSlot == _slot;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.PlayOneShot(clip);
    }

    private void TriggerHaptic()
    {
        OVRInput.SetControllerVibration(_hapticAmplitude, _hapticAmplitude, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(_hapticAmplitude, _hapticAmplitude, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptic), _hapticDuration);
    }

    private void StopHaptic()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }

    private void EnsureManagerExists()
    {
        if (HolsterManager.Instance != null) return;
        new GameObject("HolsterManager").AddComponent<HolsterManager>();
    }

    #endregion
}

/// <summary>
/// Slots disponibles en el cuerpo. Expandible según el juego crezca.
/// </summary>
public enum HolsterSlot
{
    Any,
    RightThigh,
    Back,
    LeftThigh,
    Belt,
}