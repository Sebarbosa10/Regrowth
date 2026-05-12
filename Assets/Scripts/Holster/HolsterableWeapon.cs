using UnityEngine;
using Oculus.Interaction;


public class HolsterableWeapon : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Grabbable _grabbable;
    [SerializeField] private HolsterSlot _allowedSlot = HolsterSlot.Any;

    [Header("Snap")]
    [SerializeField] private float _snapSpeed = 15f;

    public HolsterSlot AllowedSlot => _allowedSlot;

    // Estado
    private Rigidbody _rb;
    private WeaponHolster _currentHolster;
    private bool _isStored;
    private bool _wasGrabbed;

    // ?????????????????????????????????????????????
    #region Unity Callbacks

    private void Reset()
    {
        _grabbable = GetComponent<Grabbable>();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleGrabStateChange();

        if (_isStored && _currentHolster != null)
            SnapToHolster();
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Grab State

    private void HandleGrabStateChange()
    {
        if (_grabbable == null) return;

        bool isGrabbedNow = _grabbable.SelectingPointsCount > 0;

        // Flanco: soltó el arma
        if (_wasGrabbed && !isGrabbedNow)
            OnReleased();

        // Flanco: agarró el arma
        if (!_wasGrabbed && isGrabbedNow)
            OnGrabbed();

        _wasGrabbed = isGrabbedNow;
    }

    private void OnGrabbed()
    {
        if (!_isStored || _currentHolster == null) return;

        _currentHolster.OnWeaponGrabbed();
        _currentHolster = null;
        _isStored = false;
        SetPhysicsEnabled(true);

        Debug.Log($"[HolsterableWeapon] {name} sacada del holster.");
    }

    private void OnReleased()
    {
        WeaponHolster nearest = HolsterManager.Instance?.FindNearestCompatibleHolster(this);

        if (nearest != null)
            nearest.TryStore(this);
        else
            Debug.Log($"[HolsterableWeapon] {name} soltada sin holster cercano.");
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Holster Interaction

    /// <summary>
    /// Llamado por WeaponHolster cuando acepta guardar esta arma.
    /// </summary>
    public void OnStoredInHolster(WeaponHolster holster)
    {
        _currentHolster = holster;
        _isStored = true;
        SetPhysicsEnabled(false);
    }

    /// <summary>
    /// Lerp suave hacia el snapPoint del holster. Solo corre mientras está guardada.
    /// </summary>
    private void SnapToHolster()
    {
        Transform snapPoint = _currentHolster.SnapPoint;
        if (snapPoint == null) return;

        transform.position = Vector3.Lerp(
            transform.position,
            snapPoint.position,
            Time.deltaTime * _snapSpeed
        );
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            snapPoint.rotation,
            Time.deltaTime * _snapSpeed
        );
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Physics

    private void SetPhysicsEnabled(bool enabled)
    {
        _rb.isKinematic = !enabled;
        _rb.useGravity = enabled;
    }

    #endregion
}