using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMovement : MonoBehaviour
{
    // Movement speed in units per second
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;

    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        if (Keyboard.current.wKey.isPressed) moveInput = 1f;
        if (Keyboard.current.sKey.isPressed) moveInput = -1f;
        if (Keyboard.current.aKey.isPressed) turnInput = -1f;
        if (Keyboard.current.dKey.isPressed) turnInput = 1f;

        // Move forward/backward
        Vector3 move = transform.forward * moveInput * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);

        // Rotate left/right
        Quaternion turn = Quaternion.Euler(0f, turnInput * turnSpeed * Time.deltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }
}
