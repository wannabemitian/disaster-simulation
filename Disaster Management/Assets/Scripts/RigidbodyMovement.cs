
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody rb;
    private Vector2 movementInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
    }

    void Update()
    {
        movementInput = Vector2.zero;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.isPressed)
            movementInput.y += 1;

        if (Keyboard.current.sKey.isPressed)
            movementInput.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            movementInput.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            movementInput.x += 1;

        movementInput = Vector2.ClampMagnitude(
            movementInput, 1f
        );
    }

    void FixedUpdate()
    {
        Vector3 direction =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        Vector3 movement = direction * speed;

        rb.linearVelocity = new Vector3(
            movement.x,
            rb.linearVelocity.y,
            movement.z
        );
    }
}
