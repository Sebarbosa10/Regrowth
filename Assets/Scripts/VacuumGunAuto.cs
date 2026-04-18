using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class VacuumGunAuto : MonoBehaviour
{
    
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform suctionPoint;

    
    [SerializeField] private float suctionRadius = 3f;
    [SerializeField] private float suctionForce = 20f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private LayerMask vacuumLayer;

    
    [SerializeField] private float triggerThreshold = 0.7f;

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void FixedUpdate()
    {
        if (grabbable == null || suctionPoint == null)
            return;

        
        if (grabbable.SelectingPointsCount <= 0)
            return;

        
        if (!IsIndexTriggerPressed())
            return;

        
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
                Destroy(rb.gameObject);
                continue;
            }

            dir.Normalize();
            rb.AddForce(dir * suctionForce, ForceMode.Acceleration);
        }
    }


    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch
        );

        float right = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        );

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