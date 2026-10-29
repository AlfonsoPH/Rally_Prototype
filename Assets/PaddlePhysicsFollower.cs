
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddlePhysicsFollow : MonoBehaviour
{
    public Transform paddleFollowTarget;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
    }

    void FixedUpdate()
    {
        if (paddleFollowTarget == null)
            return;

        rb.MovePosition(paddleFollowTarget.position);
        rb.MoveRotation(paddleFollowTarget.rotation);
    }
}
