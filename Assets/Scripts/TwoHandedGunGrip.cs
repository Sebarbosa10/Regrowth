using UnityEngine;

/// <summary>
/// Two-handed rifle stabilizer.
/// No modifica el sistema de grab — solo lee OVRInput directamente.
/// Cuando ambas manos están "en posición", toma control de la rotación.
/// </summary>
public class TwoHandedGunGrip : MonoBehaviour
{
    
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

    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateHandPositions();

        isMainHandActive = IsRightHandNearMango();
        isTwoHanded = isMainHandActive && IsLeftHandNearCanon();

        if (isTwoHanded)
            ApplyTwoHandedRotation();
    }

    

    private void UpdateHandPositions()
    {
        rightHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        leftHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);

        var trackingSpace = FindTrackingSpace();
        if (trackingSpace != null)
        {
            rightHandPos = trackingSpace.TransformPoint(rightHandPos);
            leftHandPos = trackingSpace.TransformPoint(leftHandPos);
        }
    }

    
    private Transform _trackingSpaceCache;
    private bool _trackingSpaceSearched = false;

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
        return Vector3.Distance(leftHandPos, forwardAnchor.position) < grabRadius
               && leftGrip > gripThreshold;
    }

    

    private void ApplyTwoHandedRotation()
    {
        
        Vector3 aimDir = (leftHandPos - rightHandPos).normalized;
        if (aimDir == Vector3.zero) return;

        
        Vector3 rightHandUp = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.up;

        var trackingSpace = FindTrackingSpace();
        if (trackingSpace != null)
            rightHandUp = trackingSpace.TransformDirection(rightHandUp);

        Quaternion targetRot = Quaternion.LookRotation(aimDir, rightHandUp);

        if (rollOffset != 0f)
            targetRot *= Quaternion.Euler(0f, 0f, rollOffset);

       
        rb.MoveRotation(
            Quaternion.Slerp(rb.rotation, targetRot, rotationBlend * Time.fixedDeltaTime)
        );
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