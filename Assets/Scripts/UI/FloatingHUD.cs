using UnityEngine;


public class FloatingHUD : MonoBehaviour
{
    
    [SerializeField] private Transform cameraTransform;

    
    [SerializeField] private float distance = 2f;

    
    [SerializeField] private float verticalAngleDown = 20f;

    
    [SerializeField] private float verticalOffset = -0.1f;

    
    [SerializeField] private float positionLerpSpeed = 3f;

    
    [SerializeField] private float rotationLerpSpeed = 5f;

    
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
            
            enabled = false;
            return;
        }

        lastCameraForward = cameraTransform.forward;

        
        SnapToTarget();
    }

    private void LateUpdate()
    {
        
        float angleDelta = Vector3.Angle(lastCameraForward, cameraTransform.forward);
        if (angleDelta > angleTreshold)
        {
            lastCameraForward = cameraTransform.forward;
            ComputeTarget();
        }

        
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
    }

    private void ComputeTarget()
    {
        
        Vector3 flatForward = cameraTransform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        
        Vector3 downwardDir = Quaternion.AngleAxis(verticalAngleDown, cameraTransform.right) * flatForward;

        targetPosition = cameraTransform.position
                         + downwardDir * distance
                         + Vector3.up * verticalOffset;

        
        Vector3 lookDir = targetPosition - cameraTransform.position;
        targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }

    private void SnapToTarget()
    {
        ComputeTarget();
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    
    
    private void DebugSnap() => SnapToTarget();
}