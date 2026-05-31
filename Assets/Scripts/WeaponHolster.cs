using UnityEngine;
using Oculus.Interaction;

public class WeaponHolster : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private float snapRadius = 0.3f;
    [SerializeField] private string weaponTag = "Weapon";

    private GameObject storedWeapon = null;
    private bool isLocked = false; // bloquea el holster durante transiciones

    private void Update()
    {
        if (isLocked) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, snapRadius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag(weaponTag))
                continue;

            GameObject weaponRoot = hit.transform.root.gameObject;

            Grabbable grab = weaponRoot.GetComponentInChildren<Grabbable>();
            if (grab == null)
                continue;

            if (grab.SelectingPointsCount == 0 && storedWeapon == null)
            {
                StoreWeapon(weaponRoot);
            }

            if (grab.SelectingPointsCount > 0 && weaponRoot == storedWeapon)
            {
                ReleaseWeapon();
            }
        }
    }

    public void ForceStore(GameObject weapon)
    {
        // Libera lo que había antes
        if (storedWeapon != null)
        {
            ReleaseWeapon();
        }

        StoreWeapon(weapon);
    }

    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;

    private void StoreWeapon(GameObject weapon)
    {
        storedWeapon = weapon;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        weapon.transform.SetParent(snapPoint);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public void ReleaseWeapon()
    {
        if (storedWeapon == null) return;

        storedWeapon.transform.SetParent(null);

        Rigidbody rb = storedWeapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        storedWeapon = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}