using UnityEngine;
using Oculus.Interaction;

public class WeaponHolster : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private float snapRadius = 0.3f;
    [SerializeField] private float maxDistance = 2.0f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject weaponObject; // arrastrá el Vacuum acá

    private bool isStored = false;
    private bool hasBeenGrabbed = false;

    private void Start()
    {
        if (weaponObject != null)
        {
            WeaponMarker marker = weaponObject.GetComponent<WeaponMarker>();
            if (marker != null) marker.myHolster = this;
        }
    }

    private void Update()
    {
        if (playerTransform == null || weaponObject == null) return;

        Grabbable grab = weaponObject.GetComponentInChildren<Grabbable>();
        bool isBeingHeld = (grab != null && grab.SelectingPointsCount > 0);

        if (isBeingHeld)
        {
            hasBeenGrabbed = true;

            if (isStored)
            {
                Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                weaponObject.transform.SetParent(null);
                isStored = false;
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
                PlaceWeaponInHolster();
            }
        }
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
        weaponObject.transform.SetParent(snapPoint);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;
    }

    private void ForceReturnToHolster()
    {
        weaponObject.transform.position = snapPoint.position;
        weaponObject.transform.rotation = snapPoint.rotation;
        PlaceWeaponInHolster();
    }
}