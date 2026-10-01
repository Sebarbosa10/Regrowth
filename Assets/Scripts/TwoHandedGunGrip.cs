using UnityEngine;

public class TwoHandedGunGrip : MonoBehaviour
{
    [SerializeField] private Transform mainAnchor;
    [SerializeField] private Transform forwardAnchor;
    [SerializeField] private float grabRadius = 0.12f;
    [SerializeField] private float gripThreshold = 0.3f;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool isTwoHanded = false;
    [SerializeField] private bool isMainHandActive = false;

    private Vector3 rightHandPos;
    private Vector3 leftHandPos;

    public bool IsMainHandActive => isMainHandActive;
    public bool IsTwoHanded => isTwoHanded;

    private Transform _trackingSpaceCache;
    private bool _trackingSpaceSearched = false;

    // Solo detecta el estado de las manos. La rotacion a dos manos la aplica GunGrabTransformer:
    // hacerla aqui con el Rigidbody peleaba con el agarre y desviaba los disparos.
    private void FixedUpdate()
    {
        UpdateHandPositions();
        isMainHandActive = IsRightHandNearMango();
        isTwoHanded = isMainHandActive && IsLeftHandNearCanon();
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

    private bool IsRightHandNearMango()
    {
        if (mainAnchor == null) return false;
        return Vector3.Distance(rightHandPos, mainAnchor.position) < grabRadius;
    }

    private bool IsLeftHandNearCanon()
    {
        if (forwardAnchor == null) return false;
        float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        return Vector3.Distance(leftHandPos, forwardAnchor.position) < grabRadius && leftGrip > gripThreshold;
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
