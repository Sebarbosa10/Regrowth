using UnityEngine;

public class VRJetpackMeta : MonoBehaviour
{
    [Header("References")]
    public CapsuleCollider playerCapsule;

    [Header("Jetpack")]
    public float ascendSpeed = 2.5f;
    public float descendSpeed = 2f;
    public float acceleration = 5f;
    public float triggerDeadzone = 0.1f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float skinWidth = 0.05f;
    public float groundCheckDistance = 0.15f;

    private float currentVerticalSpeed;

    void Update()
    {
        float leftTrigger = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch
        );

        bool jetpackPressed = leftTrigger > triggerDeadzone;
        bool grounded = IsGrounded();

        float targetVerticalSpeed = 0f;

        if (jetpackPressed)
        {
            targetVerticalSpeed = ascendSpeed * leftTrigger;
        }
        else
        {
            if (!grounded)
            {
                targetVerticalSpeed = -descendSpeed;
            }
            else
            {
                targetVerticalSpeed = 0f;
            }
        }

        currentVerticalSpeed = Mathf.MoveTowards(
            currentVerticalSpeed,
            targetVerticalSpeed,
            acceleration * Time.deltaTime
        );

        Vector3 verticalMove = Vector3.up * currentVerticalSpeed * Time.deltaTime;

        MoveWithCollision(verticalMove);
    }

    void MoveWithCollision(Vector3 move)
    {
        if (move == Vector3.zero) return;

        Vector3 point1;
        Vector3 point2;
        GetCapsulePoints(out point1, out point2);

        float distance = Mathf.Abs(move.y);

        if (Physics.CapsuleCast(
            point1,
            point2,
            playerCapsule.radius,
            move.normalized,
            out RaycastHit hit,
            distance + skinWidth,
            collisionMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            float safeDistance = Mathf.Max(hit.distance - skinWidth, 0f);
            transform.position += move.normalized * safeDistance;

            currentVerticalSpeed = 0f;
        }
        else
        {
            transform.position += move;
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