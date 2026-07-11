using System.Collections;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Grabbable))]
public class MagazineReload : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GunEnergySystem energySystem;
    [SerializeField] private Grabbable gunGrabbable;
    [SerializeField] private Vector3 pullLocalDirection = Vector3.down;
    [SerializeField] private float maxPullDistance = 0.08f;
    [SerializeField] private float reloadTriggerThreshold = 0.95f;
    [SerializeField] private float returnDuration = 0.15f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip reloadTriggerSound;

    private Vector3 restLocalPosition;
    private Vector3 pullAxisNormalized;
    private Vector3 grabStartLocalPos;
    private Vector3 grabStartPointerWorldPos;
    private bool isGrabbed = false;
    private bool reloadTriggeredThisGrab = false;
    private Coroutine returnRoutine;

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void Awake()
    {
        pullAxisNormalized = pullLocalDirection.normalized;
    }

    private Quaternion restLocalRotation;

    private void Start()
    {
        restLocalPosition = transform.localPosition;
        restLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private Vector3 lastPointerWorldPos;
    private bool havePointerPos = false;

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnGrabStart(evt.Pose.position);
                lastPointerWorldPos = evt.Pose.position;
                havePointerPos = true;
                break;

            case PointerEventType.Move:
                lastPointerWorldPos = evt.Pose.position;
                havePointerPos = true;
                if (isGrabbed) OnGrabMove(evt.Pose.position);
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                havePointerPos = false;
                OnGrabEnd();
                break;
        }
    }

    private void LateUpdate()
    {
        if (isGrabbed && havePointerPos)
        {
            OnGrabMove(lastPointerWorldPos);
        }

        transform.localRotation = restLocalRotation;
    }

    private bool IsGunHeld()
    {
        if (gunGrabbable == null) return true;
        return gunGrabbable.SelectingPointsCount > 0;
    }

    private void OnGrabStart(Vector3 pointerWorldPos)
    {
        if (!IsGunHeld()) return;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        isGrabbed = true;
        reloadTriggeredThisGrab = false;
        grabStartLocalPos = transform.localPosition;
        grabStartPointerWorldPos = pointerWorldPos;
    }

    private void OnGrabMove(Vector3 pointerWorldPos)
    {
        if (!IsGunHeld())
        {
            OnGrabEnd();
            return;
        }

        Transform parent = transform.parent;

        Vector3 worldDelta = pointerWorldPos - grabStartPointerWorldPos;
        Vector3 localDelta = parent != null ? parent.InverseTransformVector(worldDelta) : worldDelta;

        float distanceAlongAxis = Vector3.Dot(localDelta, pullAxisNormalized);
        distanceAlongAxis = Mathf.Clamp(distanceAlongAxis, 0f, maxPullDistance);

        transform.localPosition = grabStartLocalPos + pullAxisNormalized * distanceAlongAxis;

        if (!reloadTriggeredThisGrab && distanceAlongAxis >= maxPullDistance * reloadTriggerThreshold)
        {
            reloadTriggeredThisGrab = true;
            TriggerReload();
        }
    }

    private void OnGrabEnd()
    {
        if (!isGrabbed) return;

        isGrabbed = false;
        returnRoutine = StartCoroutine(ReturnToRest());
    }

    private void TriggerReload()
    {
        if (audioSource != null && reloadTriggerSound != null)
            audioSource.PlayOneShot(reloadTriggerSound);

        energySystem?.ForceRecharge();
    }

    private IEnumerator ReturnToRest()
    {
        Vector3 startPos = transform.localPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, returnDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t); 
            transform.localPosition = Vector3.Lerp(startPos, restLocalPosition, t);
            yield return null;
        }

        transform.localPosition = restLocalPosition;
        returnRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 localRest = Application.isPlaying ? restLocalPosition : transform.localPosition;
        Vector3 worldRest = transform.parent != null
            ? transform.parent.TransformPoint(localRest)
            : localRest;

        Vector3 axisWorld = transform.parent != null
            ? transform.parent.TransformDirection(pullLocalDirection.normalized)
            : pullLocalDirection.normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(worldRest, 0.008f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(worldRest, worldRest + axisWorld * maxPullDistance);
        Gizmos.DrawWireSphere(worldRest + axisWorld * maxPullDistance, 0.008f);
    }
}