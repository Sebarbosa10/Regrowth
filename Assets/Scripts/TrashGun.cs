using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class TrashGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform muzzle; 

    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 3f;

    [Header("Fire Settings")]
    [SerializeField] private float fireRate = 0.3f; 
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Layer")]
    [SerializeField] private LayerMask trashLayer;

    private float lastFireTime = -999f;
    private bool triggerWasPressed = false;

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void Update()
    {
        if (grabbable == null || muzzle == null || bulletPrefab == null)
            return;

        if (grabbable.SelectingPointsCount <= 0)
            return;

        bool triggerPressed = IsIndexTriggerPressed();

        
        if (triggerPressed && !triggerWasPressed)
        {
            if (Time.time >= lastFireTime + fireRate)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }

        triggerWasPressed = triggerPressed;
    }

    private void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = muzzle.forward * bulletSpeed;
        }

        
        TrashBullet trashBullet = bullet.GetComponent<TrashBullet>();
        if (trashBullet != null)
        {
            trashBullet.SetTrashLayer(trashLayer);
        }

        Destroy(bullet, bulletLifetime);
    }

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > triggerThreshold || right > triggerThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(muzzle.position, muzzle.forward * 2f);
    }
}