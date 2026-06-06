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
    [SerializeField] private AudioClip vacuumLoopSound;   // sonido continuo al aspirar
    [SerializeField] private AudioClip trashAbsorbSound;  // sonido al destruir basura

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
        if (grabbable == null || suctionPoint == null)
        {
            StopVacuumSound();
            return;
        }

        if (grabbable.SelectingPointsCount <= 0 || !IsIndexTriggerPressed())
        {
            StopVacuumSound();
            return;
        }

        // Sonido loop de aspiradora
        PlayVacuumSound();

        Collider[] hits = Physics.OverlapSphere(
            suctionPoint.position,
            suctionRadius,
            vacuumLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null || rb.isKinematic)
                continue;

            Vector3 dir = suctionPoint.position - rb.position;
            float distance = dir.magnitude;

            if (distance <= destroyDistance)
            {
                PlayAbsorbSound();
                Destroy(rb.gameObject);
                continue;
            }

            dir.Normalize();
            rb.AddForce(dir * suctionForce, ForceMode.Acceleration);
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