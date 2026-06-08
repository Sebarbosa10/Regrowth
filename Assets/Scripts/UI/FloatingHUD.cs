using UnityEngine;

/// <summary>
/// Mantiene el Canvas flotando suavemente debajo del campo de visión del jugador.
/// Adjuntarlo al mismo GameObject que tiene el Canvas en World Space.
/// </summary>
public class FloatingHUD : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La cámara principal del playerRig (MainCamera)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Posicionamiento")]
    [Tooltip("Distancia en metros frente al jugador")]
    [SerializeField] private float distance = 2f;

    [Tooltip("Grados hacia abajo desde el horizonte (positivo = abajo)")]
    [SerializeField] private float verticalAngleDown = 20f;

    [Tooltip("Offset vertical adicional en metros (fino ajuste)")]
    [SerializeField] private float verticalOffset = -0.1f;

    [Header("Suavizado")]
    [Tooltip("Qué tan rápido sigue la posición (menor = más suave)")]
    [SerializeField] private float positionLerpSpeed = 3f;

    [Tooltip("Qué tan rápido rota para mirar al jugador")]
    [SerializeField] private float rotationLerpSpeed = 5f;

    [Tooltip("Ángulo mínimo que debe girar la cámara para que el HUD se reposicione")]
    [SerializeField] private float angleTreshold = 15f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 lastCameraForward;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("[FloatingHUD] No se encontró la cámara. Asigná cameraTransform en el Inspector.");
            enabled = false;
            return;
        }

        lastCameraForward = cameraTransform.forward;

        // Posición inicial inmediata (sin lerp)
        SnapToTarget();
    }

    private void LateUpdate()
    {
        // Solo recalcular target si la cámara giró lo suficiente
        float angleDelta = Vector3.Angle(lastCameraForward, cameraTransform.forward);
        if (angleDelta > angleTreshold)
        {
            lastCameraForward = cameraTransform.forward;
            ComputeTarget();
        }

        // Mover suavemente hacia el target
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
    }

    private void ComputeTarget()
    {
        // Dirección horizontal de la cámara (ignoramos roll)
        Vector3 flatForward = cameraTransform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        // Rotar esa dirección hacia abajo por verticalAngleDown grados
        Vector3 downwardDir = Quaternion.AngleAxis(verticalAngleDown, cameraTransform.right) * flatForward;

        targetPosition = cameraTransform.position
                         + downwardDir * distance
                         + Vector3.up * verticalOffset;

        // El canvas siempre mira hacia la cámara (cara al jugador)
        Vector3 lookDir = targetPosition - cameraTransform.position;
        targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }

    private void SnapToTarget()
    {
        ComputeTarget();
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    // Botón de debug en el Inspector para testear el snap
    [ContextMenu("Snap to Target Now")]
    private void DebugSnap() => SnapToTarget();
}