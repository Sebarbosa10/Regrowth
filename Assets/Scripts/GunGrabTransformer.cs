using Oculus.Interaction;
using UnityEngine;

// Transformer de agarre del arma. Se asigna en Grabbable > One Grab Transformer.
// En vez de conservar la postura con la que se agarro (comportamiento por defecto del SDK),
// coloca siempre el arma en la misma pose respecto al mando. Tambien aplica el apuntado a
// dos manos, en el mismo paso, para que nada mas mueva el arma mientras esta en la mano.
public class GunGrabTransformer : MonoBehaviour, ITransformer
{
    [Header("Referencias")]
    [Tooltip("Punto del arma que queda en la mano (MainAnchor)")]
    [SerializeField] private Transform gripAnchor;
    [SerializeField] private TwoHandedGunGrip twoHandedGrip;
    [Tooltip("TrackingSpace del Camera Rig. Si se deja vacio se busca al arrancar")]
    [SerializeField] private Transform trackingSpace;

    [Header("Pose en la mano")]
    [Tooltip("Giro del arma respecto al mando. X positivo baja el canon. Con ~35 el arma queda recta sujetando el mando de forma natural (en 0 apunta como el laser del mando y obliga a doblar la muneca)")]
    [SerializeField] private Vector3 gripRotationOffset = new Vector3(DefaultGripPitch, 0f, 0f);

    public const float DefaultGripPitch = 35f;
    [Tooltip("Desplazamiento del punto de agarre respecto al mando, en metros (ejes del mando)")]
    [SerializeField] private Vector3 gripPositionOffset = Vector3.zero;
    [Tooltip("Tiempo que tarda el arma en acomodarse en la mano al agarrarla")]
    [SerializeField] private float snapTime = 0.1f;

    private IGrabbable grabbable;
    private Vector3 anchorLocalPos;
    private OVRInput.Controller holdingController = OVRInput.Controller.RTouch;
    private OVRInput.Controller offController = OVRInput.Controller.LTouch;

    private bool wasTwoHanded = false;
    private Quaternion twoHandOffset = Quaternion.identity;
    private Quaternion lastRotation = Quaternion.identity;

    private float blend = 1f;
    private bool blendPosition = false;
    private Quaternion blendStartRotation;
    private Vector3 blendStartPosition;
    private int lastBlendFrame = -1;

    public void Initialize(IGrabbable grabbable)
    {
        this.grabbable = grabbable;

        if (trackingSpace == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null) trackingSpace = rig.trackingSpace;
        }

        anchorLocalPos = gripAnchor != null
            ? grabbable.Transform.InverseTransformPoint(gripAnchor.position)
            : Vector3.zero;
    }

    public void BeginTransform()
    {
        Transform target = grabbable.Transform;

        // La mano que sostiene el arma es la del mando mas cercano al punto de agarre
        Vector3 grabPosition = grabbable.GrabPoints[0].position;
        float rightDistance = (ControllerPosition(OVRInput.Controller.RTouch) - grabPosition).sqrMagnitude;
        float leftDistance = (ControllerPosition(OVRInput.Controller.LTouch) - grabPosition).sqrMagnitude;
        bool rightHolds = rightDistance <= leftDistance;
        holdingController = rightHolds ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
        offController = rightHolds ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

        wasTwoHanded = false;
        lastRotation = target.rotation;
        StartBlend(target, true);
    }

    public void UpdateTransform()
    {
        Transform target = grabbable.Transform;

        if (blend < 1f && Time.frameCount != lastBlendFrame)
        {
            lastBlendFrame = Time.frameCount;
            blend = snapTime > 0f ? Mathf.Clamp01(blend + Time.unscaledDeltaTime / snapTime) : 1f;
        }

        Quaternion controllerRotation = ControllerRotation(holdingController);
        Vector3 gripPosition = ControllerPosition(holdingController) + controllerRotation * gripPositionOffset;
        Quaternion targetRotation = controllerRotation * Quaternion.Euler(gripRotationOffset);

        // El apuntado a dos manos solo existe con el arma en la derecha (igual que el disparo)
        bool twoHanded = holdingController == OVRInput.Controller.RTouch
            && twoHandedGrip != null && twoHandedGrip.IsTwoHanded;

        if (twoHanded)
        {
            Vector3 aim = ControllerPosition(offController) - gripPosition;
            if (aim.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(aim.normalized, targetRotation * Vector3.up);

                // Al entrar se guarda la diferencia con la rotacion actual: el arma no salta,
                // y a partir de ahi sigue a la segunda mano.
                if (!wasTwoHanded)
                    twoHandOffset = Quaternion.Inverse(look) * lastRotation;

                targetRotation = look * twoHandOffset;
            }
        }
        else if (wasTwoHanded)
        {
            // Al soltar la segunda mano vuelve suavemente a la pose de una mano
            StartBlend(target, false);
        }
        wasTwoHanded = twoHanded;

        Quaternion rotation = targetRotation;
        if (blend < 1f)
            rotation = Quaternion.Slerp(blendStartRotation, targetRotation, Mathf.SmoothStep(0f, 1f, blend));

        Vector3 position = gripPosition - rotation * Vector3.Scale(anchorLocalPos, target.lossyScale);
        if (blend < 1f && blendPosition)
            position = Vector3.Lerp(blendStartPosition, position, Mathf.SmoothStep(0f, 1f, blend));

        target.SetPositionAndRotation(position, rotation);
        lastRotation = rotation;
    }

    public void EndTransform() { }

    private void StartBlend(Transform target, bool includePosition)
    {
        blend = 0f;
        blendPosition = includePosition;
        blendStartRotation = lastRotation;
        blendStartPosition = target.position;
        lastBlendFrame = Time.frameCount;
    }

    private Vector3 ControllerPosition(OVRInput.Controller controller)
    {
        Vector3 local = OVRInput.GetLocalControllerPosition(controller);
        return trackingSpace != null ? trackingSpace.TransformPoint(local) : local;
    }

    private Quaternion ControllerRotation(OVRInput.Controller controller)
    {
        Quaternion local = OVRInput.GetLocalControllerRotation(controller);
        return trackingSpace != null ? trackingSpace.rotation * local : local;
    }
}
