using UnityEngine;

public class TankCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Transform turret;

    [Header("Position")]
    public float distance = 10f;
    public float height = 5f;
    public float heightDamping = 2f;
    public float distanceDamping = 2f;

    [Header("Rotation")]
    public float rotationDamping = 3f;

    [Header("Collision")]
    public float minDistance = 2f;
    public LayerMask collisionLayers;

    private float currentDistance;
    private float currentHeight;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("TankCamera has no target assigned.");
            return;
        }
        currentDistance = distance;
        currentHeight = height;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Use turret rotation if assigned, otherwise fall back to hull
        Transform rotationSource = (turret != null) ? turret : target;
        float targetRotationAngle = rotationSource.eulerAngles.y;
        float currentRotationAngle = transform.eulerAngles.y;

        currentRotationAngle = Mathf.LerpAngle(
            currentRotationAngle,
            targetRotationAngle,
            rotationDamping * Time.deltaTime
        );

        // Smoothly adjust height
        float targetHeight = target.position.y + height;
        float currentActualHeight = transform.position.y;

        currentActualHeight = Mathf.Lerp(
            currentActualHeight,
            targetHeight,
            heightDamping * Time.deltaTime
        );

        // Calculate camera position behind tank
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 desiredPosition = target.position;
        desiredPosition -= currentRotation * Vector3.forward * currentDistance;
        desiredPosition.y = currentActualHeight;

        // Simple collision check - pull camera forward if geometry blocks view
        RaycastHit hit;
        Vector3 directionToCamera = (desiredPosition - target.position).normalized;
        float desiredDistance = Vector3.Distance(target.position, desiredPosition);

        if (Physics.Raycast(target.position, directionToCamera, out hit,
            desiredDistance, collisionLayers))
        {
            currentDistance = Mathf.Lerp(currentDistance,
                Mathf.Max(hit.distance - 0.5f, minDistance),
                distanceDamping * Time.deltaTime);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance,
                distance,
                distanceDamping * Time.deltaTime);
        }

        // Recalculate with adjusted distance
        desiredPosition = target.position;
        desiredPosition -= currentRotation * Vector3.forward * currentDistance;
        desiredPosition.y = currentActualHeight;

        // Apply position and look at tank
        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}