using UnityEngine;

public class RobotVision : MonoBehaviour
{
    [Header("Sensor Settings")]
    public int rayCount = 100; // Number of rays to cast
    public float arcAngle = 90f;  // total spread in degrees
    public float sensorRange = 25f;
    public LayerMask detectionLayers;

    private RaycastHit hit;

    void FireArcSensor()
    {
        float angleStep = arcAngle / (rayCount - 1);
        float startAngle = -arcAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            if (Physics.Raycast(transform.position, direction, out hit, sensorRange, detectionLayers))
            {
                Debug.Log($"Detected: {hit.collider.name} at {hit.distance:F2}m");

                // Visualise the hit in the Scene view
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * sensorRange, Color.green);
            }
        }
    }

    void Update()
    {
        FireArcSensor();
    }
}
