using UnityEngine;

/// <summary>
/// Two-handed rifle stabilizer.
/// No modifica el sistema de grab — solo lee OVRInput directamente.
/// Cuando ambas manos están "en posición", toma control de la rotación.
/// </summary>
public class TwoHandedGunGrip : MonoBehaviour
{
    [Header("Anchor Points en el modelo")]
    [Tooltip("Transform vacío en el mango (donde agarra la mano derecha)")]
    [SerializeField] private Transform mainAnchor;
    [Tooltip("Transform vacío en el cañón (donde apoya la mano izquierda)")]
    [SerializeField] private Transform forwardAnchor;

    [Header("Detección de agarre")]
    [Tooltip("Distancia máxima entre la mano y su anchor para considerar 'agarrada'")]
    [SerializeField] private float grabRadius = 0.12f;
    [Tooltip("Cuánto grip debe apretar el trigger para activar la mano delantera")]
    [SerializeField] private float gripThreshold = 0.3f;

    [Header("Estabilización")]
    [Tooltip("Velocidad de interpolación de rotación con dos manos")]
    [SerializeField] private float rotationBlend = 12f;
    [Tooltip("Roll fijo del rifle (ajustá si el modelo queda inclinado)")]
    [SerializeField] private float rollOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool isTwoHanded = false; // visible en Inspector en runtime

    private Rigidbody rb;

    // Posiciones de los controllers en world space
    private Vector3 rightHandPos;
    private Vector3 leftHandPos;

    // ─────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateHandPositions();
        isTwoHanded = IsTwoHandedGrip();

        if (isTwoHanded)
            ApplyTwoHandedRotation();
    }

    // ─────────────────────────────────────────
    //  LECTURA DE MANOS
    // ─────────────────────────────────────────

    private void UpdateHandPositions()
    {
        // OVRInput da la posición local al tracking space;
        // multiplicamos por el anchor del OVRCameraRig si existe,
        // o usamos world space directamente.
        rightHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        leftHandPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);

        // Si tenés un OVRCameraRig en la escena con tracking space, convertir a world:
        var trackingSpace = FindTrackingSpace();
        if (trackingSpace != null)
        {
            rightHandPos = trackingSpace.TransformPoint(rightHandPos);
            leftHandPos = trackingSpace.TransformPoint(leftHandPos);
        }
    }

    /// Cache del tracking space para no hacer Find cada frame
    private Transform _trackingSpaceCache;
    private bool _trackingSpaceSearched = false;

    private Transform FindTrackingSpace()
    {
        if (_trackingSpaceSearched) return _trackingSpaceCache;
        _trackingSpaceSearched = true;

        // El TrackingSpace es hijo del OVRCameraRig
        var rig = FindObjectOfType<OVRCameraRig>();
        _trackingSpaceCache = rig != null ? rig.trackingSpace : null;
        return _trackingSpaceCache;
    }

    // ─────────────────────────────────────────
    //  DETECCIÓN DE AGARRE CON DOS MANOS
    // ─────────────────────────────────────────

    private bool IsTwoHandedGrip()
    {
        // Mano derecha cerca del mango
        bool rightNearMain = mainAnchor != null &&
                             Vector3.Distance(rightHandPos, mainAnchor.position) < grabRadius;

        // Mano izquierda cerca del cañón Y apretando grip
        float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        bool leftNearForward = forwardAnchor != null &&
                               Vector3.Distance(leftHandPos, forwardAnchor.position) < grabRadius &&
                               leftGrip > gripThreshold;

        return rightNearMain && leftNearForward;
    }

    // ─────────────────────────────────────────
    //  APLICAR ROTACIÓN DE DOS MANOS
    // ─────────────────────────────────────────

    private void ApplyTwoHandedRotation()
    {
        // El forward del rifle = vector de mano derecha → mano izquierda
        Vector3 aimDir = (leftHandPos - rightHandPos).normalized;

        if (aimDir == Vector3.zero) return;

        // Up estable: up del controller derecho
        Vector3 rightHandUp = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.up;

        var trackingSpace = FindTrackingSpace();
        if (trackingSpace != null)
            rightHandUp = trackingSpace.TransformDirection(rightHandUp);

        // Rotación objetivo
        Quaternion targetRot = Quaternion.LookRotation(aimDir, rightHandUp);

        // Aplicar roll offset si el modelo necesita corrección
        if (rollOffset != 0f)
            targetRot *= Quaternion.Euler(0f, 0f, rollOffset);

        // Interpolar suavemente → efecto "rifle firme pero vivo"
        rb.MoveRotation(
            Quaternion.Slerp(rb.rotation, targetRot, rotationBlend * Time.fixedDeltaTime)
        );
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Anchors
        if (mainAnchor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(mainAnchor.position, grabRadius);
            Gizmos.DrawRay(mainAnchor.position, mainAnchor.forward * 0.06f);
        }
        if (forwardAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(forwardAnchor.position, grabRadius);
            Gizmos.DrawRay(forwardAnchor.position, forwardAnchor.forward * 0.06f);
        }

        // Línea de aim en runtime
        if (Application.isPlaying && isTwoHanded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightHandPos, leftHandPos);
            Gizmos.DrawSphere(rightHandPos, 0.01f);
            Gizmos.DrawSphere(leftHandPos, 0.01f);
        }
    }
}