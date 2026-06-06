using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class TrashGun : MonoBehaviour, IUpdatable
{
    [Header("References")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform muzzle;
    [SerializeField] private CustomUpdateManager updateManager;

    [Header("Bullet Settings")]
    [SerializeField] private TrashBullet bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private int poolInitialSize = 10;

    [Header("Fire Settings")]
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Layer & Audio")]
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip impactSound;

    private float _lastFireTime = -999f;
    private bool _triggerWasPressed;
    private ObjectPool<TrashBullet> _bulletPool;

    private void Start()
    {
        _bulletPool = new ObjectPool<TrashBullet>(bulletPrefab, poolInitialSize, transform);
    }

    private void OnEnable() => updateManager?.Register(this);
    private void OnDisable() => updateManager?.Unregister(this);

    public void Tick(float deltaTime)
    {
        if (grabbable == null || muzzle == null) return;
        if (grabbable.SelectingPointsCount <= 0) return;

        bool triggerPressed = IsIndexTriggerPressed(); 

        if (triggerPressed && !_triggerWasPressed && Time.time >= _lastFireTime + fireRate)
        {
            Fire();
            _lastFireTime = Time.time;
        }

        _triggerWasPressed = triggerPressed;
    }

    private void Fire()
    {
        audioSource.PlayOneShot(shootSound);
        TrashBullet bullet = _bulletPool.Get(muzzle.position, muzzle.rotation);
        bullet.Initialize(muzzle.forward * bulletSpeed, trashLayer, impactSound, bulletLifetime, _bulletPool);
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