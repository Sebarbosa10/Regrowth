using UnityEngine;

public class VRJetpackFullMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRig;
    public Transform head;
    public CapsuleCollider playerCapsule;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float airMoveSpeed = 2.2f;

    [Header("Jetpack")]
    public float ascendSpeed = 2.5f;
    public float descendSpeed = 2.0f;
    public float acceleration = 5f;
    public float triggerDeadzone = 0.1f;

    [Header("Snap Turn")]
    public float snapAngle = 45f;
    public float snapCooldown = 0.3f;
    public float snapThreshold = 0.7f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float skinWidth = 0.05f;
    public float groundCheckDistance = 0.15f;

    private float currentVerticalSpeed;
    private float lastSnapTime;

    void Update()
    {
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        float leftTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);

        // Joystick derecho
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // Debug para verificar si Unity lee el joystick derecho
        if (Mathf.Abs(rightStick.x) > 0.2f)
        {
            Debug.Log("Right Stick X: " + rightStick.x);
        }

        HandleSnapTurn(rightStick);

        bool grounded = IsGrounded();
        bool jetpackPressed = leftTrigger > triggerDeadzone;

        Vector3 forward = cameraRig.forward;
        Vector3 right = cameraRig.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMove = forward * leftStick.y + right * leftStick.x;

        if (horizontalMove.magnitude > 1f)
        {
            horizontalMove.Normalize();
        }

        float currentMoveSpeed = grounded ? moveSpeed : airMoveSpeed;
        Vector3 horizontalVelocity = horizontalMove * currentMoveSpeed;

        float targetVerticalSpeed;

        if (jetpackPressed)
        {
            targetVerticalSpeed = ascendSpeed * leftTrigger;
        }
        else
        {
            targetVerticalSpeed = grounded ? 0f : -descendSpeed;
        }

        currentVerticalSpeed = Mathf.MoveTowards(
            currentVerticalSpeed,
            targetVerticalSpeed,
            acceleration * Time.deltaTime
        );

        Vector3 finalMove =
            horizontalVelocity * Time.deltaTime +
            Vector3.up * currentVerticalSpeed * Time.deltaTime;

        MoveWithCollision(finalMove);
    }

    void HandleSnapTurn(Vector2 rightStick)
    {
        if (Time.time - lastSnapTime < snapCooldown)
            return;

        if (rightStick.x > snapThreshold)
        {
            SnapTurn(snapAngle);
            lastSnapTime = Time.time;
        }
        else if (rightStick.x < -snapThreshold)
        {
            SnapTurn(-snapAngle);
            lastSnapTime = Time.time;
        }
    }

    void SnapTurn(float angle)
    {
        if (cameraRig == null || head == null)
            return;

        Vector3 headPositionBefore = head.position;

        cameraRig.RotateAround(headPositionBefore, Vector3.up, angle);

        Vector3 headPositionAfter = head.position;
        Vector3 correction = headPositionBefore - headPositionAfter;

        cameraRig.position += correction;
        transform.position += correction;

        Debug.Log("Snap Turn: " + angle);
    }

    void MoveWithCollision(Vector3 move)
    {
        if (move == Vector3.zero)
            return;

        Vector3 point1;
        Vector3 point2;
        GetCapsulePoints(out point1, out point2);

        float distance = move.magnitude;
        Vector3 direction = move.normalized;

        if (Physics.CapsuleCast(
            point1,
            point2,
            playerCapsule.radius,
            direction,
            out RaycastHit hit,
            distance + skinWidth,
            collisionMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            float safeDistance = Mathf.Max(hit.distance - skinWidth, 0f);
            Vector3 safeMove = direction * safeDistance;

            MovePlayer(safeMove);

            if (Vector3.Dot(direction, Vector3.up) > 0.5f ||
                Vector3.Dot(direction, Vector3.down) > 0.5f)
            {
                currentVerticalSpeed = 0f;
            }
        }
        else
        {
            MovePlayer(move);
        }
    }

    void MovePlayer(Vector3 movement)
    {
        transform.position += movement;

        if (cameraRig != null)
        {
            cameraRig.position += movement;
        }
    }

    bool IsGrounded()
    {
        Vector3 point1;
        Vector3 point2;
        GetCapsulePoints(out point1, out point2);

        return Physics.CapsuleCast(
            point1,
            point2,
            playerCapsule.radius,
            Vector3.down,
            groundCheckDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void GetCapsulePoints(out Vector3 point1, out Vector3 point2)
    {
        Vector3 center = playerCapsule.transform.TransformPoint(playerCapsule.center);

        float height = Mathf.Max(playerCapsule.height, playerCapsule.radius * 2f);
        float radius = playerCapsule.radius;

        Vector3 up = playerCapsule.transform.up;
        float halfHeight = height / 2f - radius;

        point1 = center + up * halfHeight;
        point2 = center - up * halfHeight;
    }
}