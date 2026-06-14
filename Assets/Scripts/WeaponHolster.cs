using UnityEngine;
using Oculus.Interaction;
using System;



public class WeaponHolster : MonoBehaviour, IUpdatable
{
    [Header("Settings")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private float snapRadius = 0.3f;
    [SerializeField] private float maxDistance = 2.0f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject weaponObject;
    [SerializeField] private CustomUpdateManager updateManager;

    private bool isStored = false;
    private bool hasBeenGrabbed = false;
    private bool isLocked = false;

    public event Action OnWeaponStored;
    public event Action OnWeaponRemoved;

    private void Start()
    {
        if (weaponObject != null)
        {
            WeaponMarker marker = weaponObject.GetComponent<WeaponMarker>();
            if (marker != null) marker.myHolster = this;
        }
    }

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
        if (isLocked || playerTransform == null || weaponObject == null) return;

        bool beatPlaying = NarrativeBeatManager.Instance != null && NarrativeBeatManager.Instance.IsPlaying;

        Grabbable grab = weaponObject.GetComponentInChildren<Grabbable>();
        bool isBeingHeld = (grab != null && grab.SelectingPointsCount > 0);

        if (isBeingHeld)
        {
            // No permitir sacar el arma mientras suena un beat
            if (beatPlaying && !isStored)
            {
                // Ya está fuera — no hacer nada, pero tampoco registrar el grab
                return;
            }

            hasBeenGrabbed = true;

            if (isStored)
            {
                // Bloquear sacar del holster durante beat
                if (beatPlaying) return;

                Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
                weaponObject.transform.SetParent(null);
                isStored = false;

                OnWeaponRemoved?.Invoke();
            }
        }
        else if (hasBeenGrabbed && !isStored)
        {
            float distToPlayer = Vector3.Distance(playerTransform.position, weaponObject.transform.position);

            if (distToPlayer > maxDistance)
            {
                ForceReturnToHolster();
            }
            else if (Vector3.Distance(transform.position, weaponObject.transform.position) < snapRadius)
            {
                // Bloquear guardar durante beat
                if (beatPlaying) return;

                PlaceWeaponInHolster();
            }
        }
    }

    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;
    public bool IsStored => isStored;

    public void ForceStore(GameObject weapon)
    {
        weaponObject = weapon;

        WeaponMarker marker = weapon.GetComponent<WeaponMarker>();
        if (marker != null) marker.myHolster = this;

        // Resetear estado antes de guardar
        isStored = false;
        hasBeenGrabbed = false;

        ForceReturnToHolster();
    }

    private void PlaceWeaponInHolster()
    {
        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        isStored = true;
        hasBeenGrabbed = false;
        weaponObject.transform.SetParent(snapPoint);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;

        OnWeaponStored?.Invoke();
    }

    private void ForceReturnToHolster()
    {
        // Limpiar parent anterior
        weaponObject.transform.SetParent(null);

        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        weaponObject.transform.position = snapPoint.position;
        weaponObject.transform.rotation = snapPoint.rotation;

        isStored = true;
        hasBeenGrabbed = false;
        weaponObject.transform.SetParent(snapPoint);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;
    }
}