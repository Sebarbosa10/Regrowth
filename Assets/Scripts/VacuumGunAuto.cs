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

    [Header("Filtrado")]
    [SerializeField] private LayerMask compatibleLayer; // Selecciona la capa en el Inspector
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip vacuumLoopSound;
    [SerializeField] private AudioClip trashAbsorbSound;

    [SerializeField] private CustomUpdateManager updateManager;

    private bool isVacuuming = false;

    private void Reset() => grabbable = GetComponent<Grabbable>();

    private void OnEnable() => updateManager?.Register(this);
    private void OnDisable()
    {
        updateManager?.Unregister(this);
        StopVacuumSound();
    }

    public void Tick(float deltaTime)
    {
        if (grabbable == null || suctionPoint == null || grabbable.SelectingPointsCount <= 0 || !IsIndexTriggerPressed())
        {
            StopVacuumSound();
            return;
        }

        PlayVacuumSound();

        // El filtrado por capa ocurre aquí, optimizando el rendimiento
        Collider[] hits = Physics.OverlapSphere(suctionPoint.position, suctionRadius, compatibleLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;

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
        if (audioSource == null || vacuumLoopSound == null || isVacuuming) return;
        audioSource.clip = vacuumLoopSound;
        audioSource.loop = true;
        audioSource.Play();
        isVacuuming = true;
    }

    private void StopVacuumSound()
    {
        if (audioSource == null || !isVacuuming) return;
        audioSource.Stop();
        isVacuuming = false;
    }

    private void PlayAbsorbSound()
    {
        if (audioSource != null && trashAbsorbSound != null) audioSource.PlayOneShot(trashAbsorbSound);
    }

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > triggerThreshold || right > triggerThreshold;
    }
}