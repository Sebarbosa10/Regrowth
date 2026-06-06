using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;


public class VacuumGunAuto : MonoBehaviour, IUpdatable
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform suctionPoint;

    [SerializeField] private float suctionRadius = 3f;
    [SerializeField] private float suctionForce = 20f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private LayerMask vacuumLayer;
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip vacuumLoopSound; 
    [SerializeField] private AudioClip trashAbsorbSound; 
    private readonly Collider[] _hitBuffer = new Collider[20];
    private float _vacuumTickInterval = 0.05f;
    private float _vacuumTimer;

    [SerializeField] private CustomUpdateManager updateManager;

    private bool isVacuuming = false;

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void OnEnable()
    {
        if (updateManager != null) updateManager.Register(this);
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
        StopVacuumSound();
    }

    public void Tick(float deltaTime)
    {
        if (grabbable == null || suctionPoint == null) { StopVacuumSound(); return; }
        if (grabbable.SelectingPointsCount <= 0 || !IsIndexTriggerPressed())
        { StopVacuumSound(); return; }

        PlayVacuumSound();

  
        _vacuumTimer += deltaTime;
        if (_vacuumTimer < _vacuumTickInterval) return;
        _vacuumTimer = 0f;

   
        int count = Physics.OverlapSphereNonAlloc(
            suctionPoint.position, suctionRadius, _hitBuffer, vacuumLayer,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Rigidbody rb = _hitBuffer[i].attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;

            Vector3 dir = suctionPoint.position - rb.position;
            float sqrDist = dir.sqrMagnitude; 

            if (sqrDist <= destroyDistance * destroyDistance)
            {
                PlayAbsorbSound();
                _hitBuffer[i].GetComponent<TrashObject>()?.Collect(); 
                continue;
            }

            rb.AddForce(dir.normalized * suctionForce, ForceMode.Acceleration);
        }
    }

    private void PlayVacuumSound()
    {
        if (audioSource == null || vacuumLoopSound == null) return;
        if (isVacuuming) return;

        audioSource.clip = vacuumLoopSound;
        audioSource.loop = true;
        audioSource.Play();
        isVacuuming = true;
    }

    private void StopVacuumSound()
    {
        if (audioSource == null || !isVacuuming) return;
        audioSource.Stop();
        audioSource.loop = false;
        isVacuuming = false;
    }

    private void PlayAbsorbSound()
    {
        if (audioSource == null || trashAbsorbSound == null) return;
        audioSource.PlayOneShot(trashAbsorbSound);
    }

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > triggerThreshold || right > triggerThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        if (suctionPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(suctionPoint.position, suctionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(suctionPoint.position, destroyDistance);
    }
}