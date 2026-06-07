using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class VacuumGunAuto : MonoBehaviour, IUpdatable
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform suctionPoint;

    [Header("Cone Settings")]
    [SerializeField] private float coneRange = 3f;        // largo del cono
    [SerializeField] private float coneAngle = 30f;       // apertura del cono en grados
    [SerializeField] private float suctionSpeed = 3f;     // velocidad de atracción
    [SerializeField] private float destroyDistance = 0.2f;

    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip vacuumLoopSound;
    [SerializeField] private AudioClip trashAbsorbSound;

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

        PlayVacuumSound();

        Collider[] hits = Physics.OverlapSphere(
            suctionPoint.position,
            coneRange,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("VacuumGarbage"))
                continue;

            // Debugs temporales para ver qué detecta
            Debug.Log($"Detectado: {hit.gameObject.name} | Distancia: {Vector3.Distance(suctionPoint.position, hit.transform.position)} | Ángulo: {Vector3.Angle(suctionPoint.forward, hit.transform.position - suctionPoint.position)}");

            Vector3 dirToTrash = hit.transform.position - suctionPoint.position;
            float distance = dirToTrash.magnitude;
            float angle = Vector3.Angle(suctionPoint.forward, dirToTrash);

            if (angle > coneAngle)
            {
                Debug.Log($"Fuera del cono: {hit.gameObject.name} angulo={angle} max={coneAngle}");
                continue;
            }

            if (distance <= destroyDistance)
            {
                PlayAbsorbSound();
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

        // Dibujar el cono en el editor
        Gizmos.color = Color.cyan;
        float halfAngle = coneAngle * Mathf.Deg2Rad;
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