using UnityEngine;
using Oculus.Interaction;

public class WeaponHolster : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private float snapRadius = 0.3f;
    [SerializeField] private float maxDistance = 2.0f;
    [SerializeField] private Transform playerTransform;

    private GameObject trackedWeapon = null;
    private bool isStored = false;
    private bool hasBeenGrabbed = false;

    private void Update()
    {
        if (playerTransform == null || trackedWeapon == null)
        {
            if (trackedWeapon == null) TryFindAndBindWeapon();
            return;
        }

        Grabbable grab = trackedWeapon.GetComponentInChildren<Grabbable>();
        bool isBeingHeld = (grab != null && grab.SelectingPointsCount > 0);

        if (isBeingHeld)
        {
            hasBeenGrabbed = true;
            // SI LA AGARRAMOS: Forzamos la desconexión total del holster
            if (isStored)
            {
                trackedWeapon.transform.SetParent(null);
                isStored = false;
            }
        }
        else
        {
            // SI NO LA ESTAMOS AGARRANDO:
            // Si está lejos y ya fue usada, la traemos de vuelta
            if (hasBeenGrabbed && !isStored)
            {
                float distToPlayer = Vector3.Distance(playerTransform.position, trackedWeapon.transform.position);

                if (distToPlayer > maxDistance)
                {
                    ForceReturnToHolster();
                }
                // Si la acercamos al holster, se guarda
                else if (Vector3.Distance(transform.position, trackedWeapon.transform.position) < snapRadius)
                {
                    PlaceWeaponInHolster();
                }
            }
        }
    }

    private void TryFindAndBindWeapon()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, snapRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Weapon"))
            {
                trackedWeapon = hit.transform.root.gameObject;
                PlaceWeaponInHolster();
                return;
            }
        }
    }

    private void PlaceWeaponInHolster()
    {
        isStored = true;

        // Al guardar, el arma DEBE ser hija del snapPoint
        trackedWeapon.transform.SetParent(snapPoint);
        trackedWeapon.transform.localPosition = Vector3.zero;
        trackedWeapon.transform.localRotation = Quaternion.identity;

        Rigidbody rb = trackedWeapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void ForceReturnToHolster()
    {
        // Teletransporte primero, luego guardamos
        trackedWeapon.transform.position = snapPoint.position;
        trackedWeapon.transform.rotation = snapPoint.rotation;
        PlaceWeaponInHolster();
    }
}