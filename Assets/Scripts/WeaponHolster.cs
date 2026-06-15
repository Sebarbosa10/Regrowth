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

    private Grabbable cachedGrabbable;
    private Rigidbody cachedRb;

    public event Action OnWeaponStored;
    public event Action OnWeaponRemoved;

    private void CacheWeaponComponents()
    {
        if (weaponObject == null) { cachedGrabbable = null; cachedRb = null; return; }
        cachedGrabbable = weaponObject.GetComponentInChildren<Grabbable>();
        cachedRb = weaponObject.GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (weaponObject != null)
        {
            WeaponMarker marker = weaponObject.GetComponent<WeaponMarker>();
            if (marker != null) marker.myHolster = this;
        }
        CacheWeaponComponents();
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
        bool isBeingHeld = cachedGrabbable != null && cachedGrabbable.SelectingPointsCount > 0;

        if (isBeingHeld)
        {
            if (beatPlaying && !isStored) return;

            hasBeenGrabbed = true;

            if (isStored)
            {
                if (beatPlaying) return;

                if (cachedRb != null) cachedRb.isKinematic = false;
                weaponObject.transform.SetParent(null);
                isStored = false;

                OnWeaponRemoved?.Invoke();
            }
        }
        else if (hasBeenGrabbed && !isStored)
        {
            float distToPlayer = Vector3.Distance(playerTransform.position, weaponObject.transform.position);

            if (distToPlayer > maxDistance)
                ForceReturnToHolster();
            else if (Vector3.Distance(transform.position, weaponObject.transform.position) < snapRadius)
            {
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

        isStored = false;
        hasBeenGrabbed = false;
        CacheWeaponComponents();

        ForceReturnToHolster();
    }

    private void PlaceWeaponInHolster()
    {
        if (cachedRb != null)
        {
            cachedRb.velocity = Vector3.zero;
            cachedRb.angularVelocity = Vector3.zero;
            cachedRb.isKinematic = true;
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
        weaponObject.transform.SetParent(null);

        if (cachedRb != null)
        {
            cachedRb.isKinematic = true;
            cachedRb.velocity = Vector3.zero;
            cachedRb.angularVelocity = Vector3.zero;
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
