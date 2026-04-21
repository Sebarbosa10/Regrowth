using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(AudioSource))]
public class VacuumGunAuto : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform suctionPoint;

    [SerializeField] private float suctionRadius = 3f;
    [SerializeField] private float suctionForce = 20f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private LayerMask vacuumLayer;

    [SerializeField] private float triggerThreshold = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioSource suctionAudioSource; // loop de aspirado
    [SerializeField] private AudioClip suctionLoopClip;

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
        suctionAudioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (suctionAudioSource != null)
        {
            suctionAudioSource.loop = true;
            suctionAudioSource.playOnAwake = false;
            if (suctionLoopClip != null)
                suctionAudioSource.clip = suctionLoopClip;
        }
    }

    private void Update()
    {
        // manejamos el loop de audio en Update, no en FixedUpdate
        HandleSuctionAudio();
    }

    private void FixedUpdate()
    {
        if (grabbable == null || suctionPoint == null) return;
        if (grabbable.SelectingPointsCount <= 0) return;
        if (!IsIndexTriggerPressed()) return;

        Collider[] hits = Physics.OverlapSphere(
            suctionPoint.position,
            suctionRadius,
            vacuumLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;

            Vector3 dir = suctionPoint.position - rb.position;
            float distance = dir.magnitude;

            if (distance <= destroyDistance)
            {
                var trash = rb.GetComponent<TrashItem>();
                if (trash == null) trash = rb.GetComponentInParent<TrashItem>();
                if (trash != null) trash.OnVacuumed();

               
                if (TrashAudioPlayer.Instance != null)
                    TrashAudioPlayer.Instance.PlayPop();

                Destroy(rb.gameObject);
                continue;
            }

            dir.Normalize();
            rb.AddForce(dir * suctionForce, ForceMode.Acceleration);
        }
    }

    private void HandleSuctionAudio()
    {
        if (suctionAudioSource == null) return;

        bool isGrabbed = grabbable != null && grabbable.SelectingPointsCount > 0;
        bool shouldPlay = isGrabbed && IsIndexTriggerPressed();

        if (shouldPlay && !suctionAudioSource.isPlaying)
            suctionAudioSource.Play();
        else if (!shouldPlay && suctionAudioSource.isPlaying)
            suctionAudioSource.Stop();
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