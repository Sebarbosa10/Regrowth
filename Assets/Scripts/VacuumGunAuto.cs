using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class VacuumGunAuto : MonoBehaviour, IUpdatable
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform suctionPoint;

    [Header("Cone Settings")]
    [SerializeField] private float coneRange = 3f;
    [SerializeField] private float coneAngle = 30f;
    [SerializeField] private float suctionSpeed = 3f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Layer")]
    [SerializeField] private LayerMask vacuumableLayer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip vacuumLoopSound;
    [SerializeField] private AudioClip plasticAbsorbSound;
    [SerializeField] private AudioClip glassAbsorbSound;
    [SerializeField] private AudioClip organicAbsorbSound;

    [SerializeField] private CustomUpdateManager updateManager;

    private bool isVacuuming = false;

    private void Reset() { grabbable = GetComponent<Grabbable>(); }

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

        PlayVacuumSound();

        Collider[] hits = Physics.OverlapSphere(
            suctionPoint.position,
            coneRange,
            vacuumableLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.gameObject == null) continue;

            Vector3 dirToTrash = hit.transform.position - suctionPoint.position;
            float distance = dirToTrash.magnitude;
            float angle = Vector3.Angle(suctionPoint.forward, dirToTrash);

            if (angle > coneAngle)
                continue;

            if (distance <= destroyDistance)
            {
              
                if (TrashDiscoveryManager.Instance != null)
                    TrashDiscoveryManager.Instance.OnTrashCollected(hit.gameObject);

                PlayAbsorbSound(hit.gameObject.tag);
                Destroy(hit.gameObject);
                continue;
            }

            hit.transform.position = Vector3.MoveTowards(
                hit.transform.position,
                suctionPoint.position,
                suctionSpeed * deltaTime
            );
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
        audioSource.loop = false;
        isVacuuming = false;
    }

    private void PlayAbsorbSound(string tag)
    {
        if (audioSource == null) return;

        AudioClip clip = null;

        switch (tag)
        {
            case "Plastic": clip = plasticAbsorbSound; break;
            case "Glass": clip = glassAbsorbSound; break;
            case "Organic": clip = organicAbsorbSound; break;
        }

        if (clip != null)
            audioSource.PlayOneShot(clip);
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
        Vector3 forward = suctionPoint.forward * coneRange;
        Vector3 right = Quaternion.Euler(0, coneAngle, 0) * suctionPoint.forward * coneRange;
        Vector3 left = Quaternion.Euler(0, -coneAngle, 0) * suctionPoint.forward * coneRange;
        Vector3 up = Quaternion.Euler(coneAngle, 0, 0) * suctionPoint.forward * coneRange;
        Vector3 down = Quaternion.Euler(-coneAngle, 0, 0) * suctionPoint.forward * coneRange;
        Gizmos.DrawRay(suctionPoint.position, forward);
        Gizmos.DrawRay(suctionPoint.position, right);
        Gizmos.DrawRay(suctionPoint.position, left);
        Gizmos.DrawRay(suctionPoint.position, up);
        Gizmos.DrawRay(suctionPoint.position, down);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(suctionPoint.position, destroyDistance);
    }
}