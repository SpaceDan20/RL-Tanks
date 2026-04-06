using UnityEngine;
using UnityEngine.InputSystem;

/// GodCamera controls:
/// - WASD / arrows: pan horizontally
/// - Left Shift: move faster
/// - Right mouse held: free-look rotation (yaw + pitch)
/// - Middle mouse held: drag pan
/// - Scroll wheel: zoom (raise/lower)
/// - V: toggle between God view and TankCamera

public class GodCamera : MonoBehaviour
{
    [Header("References")]
    public Camera godCamera;
    public Camera tankCamera;

    [Header("Movement")]
    public float moveSpeed = 20f;
    public float fastMultiplier = 3f;

    [Header("Rotation")]
    public float rotateSensitivity = 0.2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Pan")]
    public float panSensitivity = 3f;

    [Header("Zoom")]
    public float scrollSpeed = 500f;
    public float minHeight = 5f;
    public float maxHeight = 80f;

    [Header("Starting Position")]
    public Vector3 startPosition = new Vector3(0f, 40f, 0f);
    public Vector3 startRotation = new Vector3(60f, 0f, 0f);

    private bool godViewActive = true;
    private float yaw;
    private float pitch;
    private Vector2 lastMousePosition;

    private void Start()
    {
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        yaw   = startRotation.y;
        pitch = startRotation.x;

        ApplyActiveCamera();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse    = Mouse.current;

        if (keyboard == null || mouse == null) return;

        if (keyboard.vKey.wasPressedThisFrame)
        {
            godViewActive = !godViewActive;
            ApplyActiveCamera();
        }

        if (!godViewActive) return;

        HandleRotation(mouse);
        HandleMovement(keyboard);
        HandleMousePan(mouse);
        HandleZoom(mouse);
    }

    private void HandleRotation(Mouse mouse)
    {
        if (mouse.rightButton.wasPressedThisFrame)
            lastMousePosition = mouse.position.ReadValue();

        if (!mouse.rightButton.isPressed) return;

        Vector2 current = mouse.position.ReadValue();
        Vector2 delta   = current - lastMousePosition;
        lastMousePosition = current;

        yaw   += delta.x * rotateSensitivity;
        pitch -= delta.y * rotateSensitivity;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement(Keyboard keyboard)
    {
        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f);

        Vector3 move = Vector3.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            move += FlatForward() * speed;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            move -= FlatForward() * speed;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            move += transform.right * speed;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            move -= transform.right * speed;

        transform.position += move * Time.deltaTime;
        ClampHeight();
    }

    private void HandleMousePan(Mouse mouse)
    {
        if (mouse.middleButton.wasPressedThisFrame)
            lastMousePosition = mouse.position.ReadValue();

        if (!mouse.middleButton.isPressed) return;

        Vector2 current = mouse.position.ReadValue();
        Vector2 delta   = current - lastMousePosition;
        Vector3 pan     = (delta.x * -transform.right + delta.y * -FlatForward())
                          * panSensitivity * Time.deltaTime;
        transform.position += pan;
        ClampHeight();
        lastMousePosition = current;
    }

    private void HandleZoom(Mouse mouse)
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll == 0f) return;

        Vector3 pos = transform.position;
        pos.y -= scroll * scrollSpeed * Time.deltaTime;
        pos.y  = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }

    private void ApplyActiveCamera()
    {
        if (godCamera != null)  godCamera.enabled  = godViewActive;
        if (tankCamera != null) tankCamera.enabled = !godViewActive;
    }

    // Forward projected onto the horizontal plane so WASD stays grounded
    private Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    private void ClampHeight()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }
}
