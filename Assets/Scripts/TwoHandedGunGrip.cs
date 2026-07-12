using UnityEngine;
using Oculus.Interaction;

public class TwoHandedGunGrip : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Transform mainAnchor;
    [SerializeField] private Transform forwardAnchor;
    [SerializeField] private float grabRadius = 0.12f;
    [SerializeField] private float gripThreshold = 0.3f;
    [SerializeField] private float rotationBlend = 12f;
    [SerializeField] private float rollOffset = 0f;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool isTwoHanded = false;
    [SerializeField] private bool isMainHandActive = false;

    private Rigidbody rb;
    private Vector3 rightHandPos;
    private Vector3 leftHandPos;

    public bool IsMainHandActive => isMainHandActive;
    public bool IsTwoHanded => isTwoHanded;
    public OVRInput.Controller MainHandController => mainHandController;

    private OVRInput.Controller mainHandController = OVRInput.Controller.None;
    private bool wasHeldLastFrame = false;

    private Vector3 lastHeldPosition;
    private Quaternion lastHeldRotation = Quaternion.identity;
    private int releaseGraceFramesRemaining = 0;
    private const int ReleaseGraceFrames = 5;

    private Transform _trackingSpaceCache;
    private bool _trackingSpaceSearched = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        bool isHeld = grabbable != null && grabbable.SelectingPointsCount > 0;

        if (!isHeld)
        {
            if (wasHeldLastFrame)
            {
                releaseGraceFramesRemaining = ReleaseGraceFrames;
            }

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (releaseGraceFramesRemaining > 0)
            {
                transform.position = lastHeldPosition;
                transform.rotation = lastHeldRotation;
                releaseGraceFramesRemaining--;
            }

            isMainHandActive = false;
            isTwoHanded = false;
            mainHandController = OVRInput.Controller.None;
            wasHeldLastFrame = false;
            return;
        }

        wasHeldLastFrame = true;

        UpdateHandPositions();
        DetermineMainHand();
        isTwoHanded = isMainHandActive && IsOffHandNearForwardAnchor();

        if (!isMainHandActive) return;

        Quaternion targetRot = isTwoHanded ? ComputeTwoHandedTargetRotation() : ComputeMainHandTargetRotation();
        ApplyGripSnap(targetRot);

        lastHeldPosition = transform.position;
        lastHeldRotation = transform.rotation;
    }

    private void UpdateHandPositions()
    {
        rightHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        leftHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);

        Transform ts = FindTrackingSpace();
        if (ts != null)
        {
            rightHandPos = ts.TransformPoint(rightHandPos);
            leftHandPos = ts.TransformPoint(leftHandPos);
        }
    }

    private Transform FindTrackingSpace()
    {
        if (_trackingSpaceSearched) return _trackingSpaceCache;
        _trackingSpaceSearched = true;
        var rig = FindObjectOfType<OVRCameraRig>();
        _trackingSpaceCache = rig != null ? rig.trackingSpace : null;
        return _trackingSpaceCache;
    }

    private void DetermineMainHand()
    {
        if (mainAnchor == null)
        {
            isMainHandActive = false;
            mainHandController = OVRInput.Controller.None;
            return;
        }

        bool rightNear = Vector3.Distance(rightHandPos, mainAnchor.position) < grabRadius;
        bool leftNear = Vector3.Distance(leftHandPos, mainAnchor.position) < grabRadius;

        if (rightNear && leftNear)
        {
            mainHandController = mainHandController == OVRInput.Controller.LTouch
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;
        }
        else if (rightNear)
        {
            mainHandController = OVRInput.Controller.RTouch;
        }
        else if (leftNear)
        {
            mainHandController = OVRInput.Controller.LTouch;
        }
        else
        {
            mainHandController = OVRInput.Controller.None;
        }

        isMainHandActive = mainHandController != OVRInput.Controller.None;
    }

    private bool IsOffHandNearForwardAnchor()
    {
        if (forwardAnchor == null || mainHandController == OVRInput.Controller.None) return false;

        bool mainIsRight = mainHandController == OVRInput.Controller.RTouch;
        OVRInput.Controller offHand = mainIsRight ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        Vector3 offHandPos = mainIsRight ? leftHandPos : rightHandPos;

        float offGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, offHand);
        return Vector3.Distance(offHandPos, forwardAnchor.position) < grabRadius && offGrip > gripThreshold;
    }

    private Quaternion ComputeTwoHandedTargetRotation()
    {
        bool mainIsRight = mainHandController == OVRInput.Controller.RTouch;
        Vector3 mainPos = mainIsRight ? rightHandPos : leftHandPos;
        Vector3 offPos = mainIsRight ? leftHandPos : rightHandPos;

        Vector3 aimDir = (offPos - mainPos).normalized;
        if (aimDir == Vector3.zero) return transform.rotation;

        Vector3 mainHandUp = OVRInput.GetLocalControllerRotation(mainHandController) * Vector3.up;

        Transform ts = FindTrackingSpace();
        if (ts != null) mainHandUp = ts.TransformDirection(mainHandUp);

        Quaternion lookRot = Quaternion.LookRotation(aimDir, mainHandUp);
        if (rollOffset != 0f) lookRot *= Quaternion.Euler(0f, 0f, rollOffset);

        return Quaternion.Slerp(transform.rotation, lookRot, rotationBlend * Time.deltaTime);
    }

    private Quaternion ComputeMainHandTargetRotation()
    {
        Quaternion controllerRot = OVRInput.GetLocalControllerRotation(mainHandController);

        Transform ts = FindTrackingSpace();
        if (ts != null) controllerRot = ts.rotation * controllerRot;
        return controllerRot * Quaternion.Inverse(mainAnchor.localRotation);
    }

    private void ApplyGripSnap(Quaternion targetRot)
    {
        Vector3 mainHandPos = mainHandController == OVRInput.Controller.RTouch ? rightHandPos : leftHandPos;
        Vector3 scaledLocalOffset = Vector3.Scale(mainAnchor.localPosition, transform.lossyScale);
        Vector3 targetPos = mainHandPos - (targetRot * scaledLocalOffset);

        transform.rotation = targetRot;
        transform.position = targetPos;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (mainAnchor != null)
        {
            Gizmos.color = isMainHandActive ? Color.green : new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(mainAnchor.position, grabRadius);
            Gizmos.DrawRay(mainAnchor.position, mainAnchor.forward * 0.06f);
        }
        if (forwardAnchor != null)
        {
            Gizmos.color = isTwoHanded ? Color.cyan : new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(forwardAnchor.position, grabRadius);
            Gizmos.DrawRay(forwardAnchor.position, forwardAnchor.forward * 0.06f);
        }

        if (Application.isPlaying && isTwoHanded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightHandPos, leftHandPos);
            Gizmos.DrawSphere(rightHandPos, 0.01f);
            Gizmos.DrawSphere(leftHandPos, 0.01f);
        }
    }
}