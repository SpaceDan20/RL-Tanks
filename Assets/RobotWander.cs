using UnityEngine;

public class RobotWander : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float turnSpeed = 90f;

    [Header("Wandering")]
    public float minWanderTime = 1f;   // min seconds before picking new direction
    public float maxWanderTime = 7f;   // max seconds before picking new direction
    public float minIdleTime = 0.5f;   // min seconds to idle before moving again
    public float maxIdleTime = 5f;     // max seconds to idle before moving again
    private float wanderTimer;
    private float targetAngle;
    private bool isIdle = false;

    [Header("Obstacle Avoidance")]
    public float sensorRange = 20f;
    public float reactionDistance = 5f;  // how close before reacting
    public int rayCount = 50;
    public float arcAngle = 120f;
    public LayerMask detectionLayers;

    [Header("Objective")]
    public string targetName = "SphereOfInterest";
    public float seekTurnSpeed = 120f;  // can turn faster when seeking
    private Transform target;
    private bool objectiveComplete = false;

    void Start()
    {
        PickNewDirection();

        // Find the sphere by name at startup
        GameObject obj = GameObject.Find(targetName);
        if (obj != null)
        {
            target = obj.transform;
        }
        else
        {
            Debug.LogWarning($"Could not find GameObject named '{targetName}'");
        }
    }

    void Update()
    {
        // Stop everything if objective is complete
        if (objectiveComplete) return;

        // If target is visible, seek it
        if (target != null && CanSeeTarget())
        {
            SeekTarget();
            return; // Skip wandering logic when seeking
        }

        // Check sensors for obstacles if not seeking
        if (ObstacleDetected())
        {
            // If an obstacle is detected, pick a new direction immediately
            PickClearestDirection();
            isIdle = false; // Ensure we are not idling when reacting to an obstacle
        }

        // Count down
        wanderTimer -= Time.deltaTime;

        // When timer runs out, pick a new direction or start idling
        if (wanderTimer <= 0f)
        {
            if (isIdle)
            {
                // Finished idling, start moving again
                PickNewDirection();
                isIdle = false;
            }
            else
            {
                // Finished wandering, start idling
                wanderTimer = Random.Range(minIdleTime, maxIdleTime); // Set idle timer
                isIdle = true; // Start idling
            }
        }

        // Only rotate and move if not idling
        if (!isIdle)
        {
            // Smoothly rotate toward target angle
            float currentAngle = transform.eulerAngles.y;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0, newAngle, 0);

            // Always move forward
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    void PickNewDirection()
    {
        targetAngle = Random.Range(0f, 360f);
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    void PickClearestDirection()
    {
        float angleStep = arcAngle / (rayCount - 1);
        float startAngle = -arcAngle / 2f;
        float bestDistance = -1f;
        float bestAngle = targetAngle;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            float distance;

            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, sensorRange, detectionLayers))
                distance = hit.distance;
            else
                distance = sensorRange;  // fully clear, treat as max range

            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestAngle = transform.eulerAngles.y + angle;
            }
        }

        // Add a random offset to the best angle so escapes feel unpredictable
        float randomOffset = Random.Range(-45f, 45f);
        targetAngle = bestAngle + randomOffset;

        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
        isIdle = false;
    }

    bool ObstacleDetected()
    {
        float angleStep = arcAngle / (rayCount - 1);
        float startAngle = -arcAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, sensorRange, detectionLayers))
            {
                if (hit.distance < reactionDistance)
                {
                    Debug.DrawLine(transform.position, hit.point, Color.red);
                    return true; // Obstacle detected within reaction distance
                }
                else
                {
                    Debug.DrawLine(transform.position, hit.point, Color.yellow); // Detected but not close enough to react
                }
            }
            else
            {
                Debug.DrawRay(transform.position, direction * sensorRange, Color.green); // No obstacle detected
            }
        }
        return false; // No obstacles detected within reaction distance
    }

    bool CanSeeTarget()
    {
        if (target == null) return false;

        Vector3 directionToTarget = target.position - transform.position;
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, sensorRange, detectionLayers))
        {
            if (hit.transform == target)
            {
                Debug.DrawLine(transform.position, hit.point, Color.blue); // Target visible
                return true;
            }
        }
        return false; // Target not visible
    }

    void SeekTarget()
    {
        // Rotate toward the target
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        float currentAngle = transform.eulerAngles.y;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angleToTarget, seekTurnSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(0, newAngle, 0);

        // Move toward it
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SphereOfInterest"))
        {
            objectiveComplete = true;
            Debug.Log("Wheely found the SphereOfInterest!");
        }
    }
}
