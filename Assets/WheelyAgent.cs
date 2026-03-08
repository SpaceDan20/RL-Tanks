using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class WheelyAgent : Agent
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;

    [Header("Target")]
    public Transform target;

    [Header("Sensors")]
    public float sensorRange = 20f;
    public int rayCount = 50;
    public float arcAngle = 120f;
    public LayerMask detectionLayers;
    private bool sphereInSight;
    private float previousDistanceToTarget;
    private float distanceToTarget;
    private float sphereAngle;
    private int sphereHits;


    public override void OnEpisodeBegin()
    {
        // reset parameters for new episode
        previousDistanceToTarget = 0f;
        distanceToTarget = 0f;

        // Spawn in the center rather than a corner
        transform.position = new Vector3(0, transform.position.y, 0);

        // Random starting rotation so he faces a random direction
        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Respawn sphere at random position but not too close to Wheely
        Vector3 newPos;
        do
        {
            newPos = new Vector3(
                Random.Range(-20f, 20f),
                target.position.y,
                Random.Range(-20f, 20f)
            );
        } while (Vector3.Distance(newPos, transform.position) < 10f); // ensure target isn't too close at start

        target.position = newPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float angleStep = arcAngle / (rayCount - 1);
        float startAngle = -arcAngle / 2f;

        // Reset sphere detection variables
        bool sphereDetected = false;
        float sphereDistance = 0f;
        sphereAngle = 0f;
        sphereHits = 0;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, dir, out hit, sensorRange, detectionLayers))
            {
                sensor.AddObservation(1f);
                sensor.AddObservation(hit.distance / sensorRange);

                // Check if this ray specifically hit the sphere
                if (hit.collider.CompareTag("SphereOfInterest"))
                {
                    sphereDetected = true;
                    sphereDistance += hit.distance / sensorRange;
                    sphereAngle += angle / (arcAngle / 2f);  // normalized -1 to 1
                    sphereHits++;
                }
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(1f);
            }
        }

        // Only reveal sphere location if a ray actually hit it
        if (sphereDetected)
        {
            sphereInSight = true;
            distanceToTarget = sphereDistance / sphereHits; // average distance from rays that hit the sphere
            sensor.AddObservation(sphereDistance / sphereHits);  // how far (on average)
            sensor.AddObservation(sphereAngle / sphereHits);     // which direction (on average)
        }
        else
        {
            sphereInSight = false;
            distanceToTarget = 0f;
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Read actions from the network
        float turn = actions.ContinuousActions[0];  // -1 to 1
        float move = actions.ContinuousActions[1];  //  0 to 1

        // Apply movement
        transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);
        transform.Translate(Vector3.forward * move * moveSpeed * Time.deltaTime);

        // Small reward for keeping the target in sight, encourages exploration and tracking
        if (sphereInSight)
        {
            AddReward(0.001f); // reward for seeing the target

            // reward for centering the target in the sensor arc
            float avgAngle = sphereAngle / sphereHits;
            float centeredness = 1f - Mathf.Abs(avgAngle);
            AddReward(centeredness * 0.001f);

            if (distanceToTarget < previousDistanceToTarget)
                AddReward(0.005f);  // reward for getting closer to the target 
            else
                AddReward(-0.003f); // penalty for moving away from the target
        }

        // Small penalty each step to encourage efficiency
        AddReward(-0.001f);

        // Update previous distance for next step's reward calculation
        previousDistanceToTarget = distanceToTarget;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Keyboard.current.aKey.isPressed ? -1f :
                               Keyboard.current.dKey.isPressed ? 1f : 0f;
        continuousActions[1] = Keyboard.current.wKey.isPressed ? 1f :
                               Keyboard.current.sKey.isPressed ? -1f : 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SphereOfInterest"))
        {
            AddReward(2f);   // big reward for finding the target
            EndEpisode();      // start fresh
        }

        if (other.CompareTag("Wall"))
        {
            AddReward(-0.5f);  // penalty for hitting a wall
            EndEpisode();      // reset rather than getting stuck
        }
    }

    void OnDrawGizmos()
    {
        // Visualise sensor rays in Scene view
        if (!Application.isPlaying) return;

        float angleStep = arcAngle / (rayCount - 1);
        float startAngle = -arcAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, dir * sensorRange);
        }
    }
}
