using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankyAgent : Agent
{
    [Header("Hull Movement")]
    public float maxSpeed = 7f;
    public float accelerationTime = 3f; // Time to reach max speed
    public float decelerationTime = 1.5f; // Time to come to a stop
    public float brakeMultiplier = 2f; // Multiplier for braking deceleration
    private float currentSpeed = 0f;
    private float accelerationRate;
    private float decelerationRate;
    private float brakeRate;

    [Header("Hull Turning")]
    public float minTurnRate = 20f; // minimum degrees per second
    public float maxTurnRate = 60f; // maximum degrees per second at low speed

    [Header("Turret")]
    public Transform turret;
    public float turretRotationSpeed = 45f;

    [Header("Combat")]
    public float shellDamage = 50f;
    public float gunReloadTime = 5f;
    public Transform barrelTip;
    private float fireCooldown;
    private LineRenderer shotLine;
    public float shotLineDuration = 1f;

    [Header("Sensors")]
    public LayerMask detectionLayers;
    public int gunSensors = 10;
    public float gunSensorRange = 30f;
    public float gunSensorAngle = 30f;
    public int turretSensors = 15;
    public float turretSensorRange = 20f;
    public float turretSensorAngle = 60f;
    public int driverSensors = 20;
    public float driverSensorRange = 10f;
    public float driverSensorAngle = 120f;
    public int hullSensors = 45;
    public float hullSensorRange = 5f;
    public float hullSensorAngle = 240f;

    [Header("References")]
    public Transform environmentCenter;
    public EnvironmentManager environmentManager;

    private Rigidbody rb;
    private float previousAlignmentPotential;
    private bool enemyInSight;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.01f;
        accelerationRate = maxSpeed / accelerationTime;
        decelerationRate = maxSpeed / decelerationTime;
        brakeRate = decelerationRate * brakeMultiplier;
        shotLine = gameObject.AddComponent<LineRenderer>();
        shotLine.startWidth = 0.5f;
        shotLine.endWidth = 0.4f;
        shotLine.material = new Material(Shader.Find("Sprites/Default"));
        shotLine.startColor = new Color(1f, 0.5f, 0f, 0.8f);
        shotLine.endColor = new Color(1f, 0.5f, 0f, 0f);
        shotLine.enabled = false;
        shotLine.useWorldSpace = true;
    }

    public override void OnEpisodeBegin()
    {
        // Reset velocity and angular velocity on episode start
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep(); // force sleep to ensure it doesn't start with residual motion

        // Reset firing cooldown
        fireCooldown = 0f;

        // Reset alignment potential tracking and enemy sight flag
        previousAlignmentPotential = 0f;
        enemyInSight = false;

        // Reset health
        GetComponent<TankHealth>().ResetHealth();

        // Reset episode in the environment manager to reposition tanks and reset any necessary state
        environmentManager.ResetEpisode();

        // Reset turret rotation 
        turret.localRotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        enemyInSight = false; // Reset enemy in sight flag at the start of observation collection
        // Gun sensors for detecting enemies and obstacles in the firing arc
        CastSensorArray(sensor, turret, gunSensorAngle, gunSensorRange, gunSensors, 0f); // Centered on turret forward
        // Turret sensors for situational awareness around the turret
        CastSensorArray(sensor, turret, turretSensorAngle, turretSensorRange, turretSensors, 0f); // Centered on turret forward
        // Driver sensors for close-range awareness around the hull
        CastSensorArray(sensor, transform, driverSensorAngle, driverSensorRange, driverSensors, 0f); // Centered on hull forward
        // Hull sensors for very close obstacles, especially behind the tank
        CastSensorArray(sensor, transform, hullSensorAngle, hullSensorRange, hullSensors, -180f); // Centered on hull backward
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Interpret continuous actions for left and right tracks, and turret rotation
        float leftTrack = actions.ContinuousActions[0]; // -1 to 1
        float rightTrack = actions.ContinuousActions[1]; // -1 to 1
        float turretRotation = actions.ContinuousActions[2]; // -1 to 1
        // Calculate forward and turn inputs
        float forwardInput = (leftTrack + rightTrack) / 2f; // average of both tracks for forward/backward (normalized -1 to 1)
        float turnInput = (leftTrack - rightTrack) / 2f; // difference of tracks for turning (normalized -1 for left, 1 for right)

        float targetSpeed = forwardInput * maxSpeed; // determine target speed based on input
        if (forwardInput > 0.01f) // Forward input
        {
            if (currentSpeed < -0.01f)
            {
                // Braking to a stop
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeRate * Time.deltaTime);
            }
            else
            {
                // Accelerating forward
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);
            }
        }
        else if (forwardInput < -0.01f) // Backward input
        {
            if (currentSpeed > 0.01f)
            {
                // Braking to a stop
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeRate * Time.deltaTime);
            }
            else
            {
                // If already stopped or moving backward, accelerate backward
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);
            }
        }
        else // No input, decelerate to stop
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelerationRate * Time.deltaTime);
        }
        // Clamp current speed to max limits
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // Apply linear velocity based on current speed
        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            transform.forward * currentSpeed,
            Time.deltaTime * 8f);

        // Calculate turn rate based on current speed
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float effectiveTurnRate = Mathf.Lerp(maxTurnRate, minTurnRate, speedFactor);

        // Apply turning
        float targetAngularVelocity = turnInput * effectiveTurnRate * Mathf.Deg2Rad;
        rb.angularVelocity = Vector3.Lerp(
            rb.angularVelocity,
            new Vector3(0, targetAngularVelocity, 0),
            Time.deltaTime * 8f
        );

        // Rotate turret independently of tank body
        if (turret != null)
        {
            turret.Rotate(0, turretRotation * turretRotationSpeed * Time.deltaTime, 0);
        }

        // Discrete action for firing
        int fireAction = actions.DiscreteActions[0]; // 0 = no fire, 1 = fire
        if (fireCooldown <= 0.01f)
        {
            // Able to fire
            if (fireAction == 1)
            {
                Vector3 dir = barrelTip.forward; // direction the barrel is pointing
                RaycastHit hit;
                bool hitSomething = Physics.Raycast(barrelTip.position, dir, out hit, 1000f, detectionLayers);
                Vector3 shotEnd = hitSomething ? hit.point : barrelTip.position + barrelTip.forward * gunSensorRange;
                StartCoroutine(ShowShotLine(barrelTip.position, shotEnd));
                if (hitSomething && hit.collider.CompareTag("Tank"))
                {
                    Debug.Log($"{gameObject.name} hit {hit.collider.gameObject.name} at distance {hit.distance:F2}");
                    // Hit an enemy, apply damage and reward
                    TankHealth enemyHealth = hit.collider.GetComponent<TankHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(shellDamage);
                        AddReward(0.5f); // Reward for hitting an enemy
                    }
                }
                else
                {
                    AddReward(-0.05f); // Missed shot penalty to encourage accuracy
                }
                fireCooldown = gunReloadTime; // reset cooldown
            }
        }

        if (enemyInSight) // Only calculate alignment potential if an enemy is detected in the sensor arrays
        {
            float currentAlignmentPotential = GetAlignmentPotential();
            float alignmentShaping = (0.99f * currentAlignmentPotential) - previousAlignmentPotential;
            AddReward(alignmentShaping * 0.1f); // Reward for improving alignment with nearest enemy, scaled down
            previousAlignmentPotential = currentAlignmentPotential;
        }
        else
        {
            previousAlignmentPotential = 0f;
        }

        // Step penalty
        AddReward(-0.001f);

        // Discrete rewards:
        // Kill, Hits

        // Discrete penalties:
        // Death, Getting hit, Missed shots, Step penalty

        // PBRS:
        // Turret aiming

        // Considerations:
        // Avoiding detection, avoiding obstacles, movement
    }

    private TankyAgent GetNearestEnemy()
    {
        TankyAgent nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (TankyAgent agent in environmentManager.GetAllEnemies(this))
        {
            float distance = Vector3.Distance(transform.position, agent.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = agent;
            }
        }

        return nearest;
    }

    private float GetAlignmentPotential()
    {
        TankyAgent nearestEnemy = GetNearestEnemy();
        if (nearestEnemy == null) return 0f;

        Vector3 toEnemy = (nearestEnemy.transform.position - turret.position).normalized;
        float angle = Vector3.Angle(turret.forward, toEnemy);
        return 1f - (angle / 180f);
    }

    private void Update()
    {
        // Cooldown management for firing
        if (fireCooldown > 0.01f)
        {
            fireCooldown -= Time.deltaTime;
        }
    }

    private void CastSensorArray(
        VectorSensor sensor,
        Transform origin,
        float arcAngle,
        float range,
        int rayCount,
        float startAngleOffset = 0f
        ) // defaults to 0 for centered arcs
    {
        float startAngle = (-arcAngle / 2f) - startAngleOffset; // start angle for the first ray
        float angleStep = arcAngle / (rayCount - 1); // angle between each ray

        // Cast rays in the defined arc and add observations
        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * origin.forward;
            RaycastHit hit;

            // Check if the ray hits something within range on the detection layers
            if (Physics.Raycast(origin.position, dir, out hit, range, detectionLayers))
            {
                sensor.AddObservation(1f); // Hit something
                if (hit.collider.CompareTag("Tank"))
                {
                    enemyInSight = true;
                    sensor.AddObservation(hit.distance / range); // Normalized distance to enemy
                    sensor.AddObservation(1f); // Enemy detected
                }
                else
                {
                    sensor.AddObservation(hit.distance / range); // Normalized distance to obstacle
                    sensor.AddObservation(0.5f); // Obstacle detected
                }
            }
            else
            {
                sensor.AddObservation(0f); // No hit
                sensor.AddObservation(1f); // Clear line of sight
                sensor.AddObservation(0f); // Nothing detected
            }
        }
    }

    public void OnDestroyed()
    {
        environmentManager.OnTankDestroyed(this); // Notify environment manager of destruction
    }

    private void DrawSensorGizmos(
        Transform origin,
        float arcAngle,
        float range,
        int rayCount,
        float startAngleOffset,
        Color color)
    {
        // Draw sensor rays for each sensor array in the Scene view for debugging
        Gizmos.color = color;
        float startAngle = (-arcAngle / 2f) - startAngleOffset; // start angle for the first ray
        float angleStep = arcAngle / (rayCount - 1); // angle between each ray
        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * origin.forward;
            Gizmos.DrawRay(origin.position, dir * range);
        }
    }

    private void OnDrawGizmos()
    {
        // Visualise sensor rays in Scene view
        if (turret == null) return;
        DrawSensorGizmos(turret, gunSensorAngle, gunSensorRange, gunSensors, 0f, new Color(1f, 0f, 0f, 0.1f));
        DrawSensorGizmos(turret, turretSensorAngle, turretSensorRange, turretSensors, 0f, new Color(1f, 1f, 0f, 0.1f));
        DrawSensorGizmos(transform, driverSensorAngle, driverSensorRange, driverSensors, 0f, new Color(0f, 1f, 0f, 0.1f));
        DrawSensorGizmos(transform, hullSensorAngle, hullSensorRange, hullSensors, -180f, new Color(0f, 0f, 1f, 0.1f));
    }

    private IEnumerator ShowShotLine(Vector3 start, Vector3 end)
    {
        // Visual feedback for firing - shows a line from the barrel tip to the hit point for a brief moment
        shotLine.SetPosition(0, start);
        shotLine.SetPosition(1, end);
        shotLine.enabled = true;
        yield return new WaitForSeconds(shotLineDuration);
        shotLine.enabled = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;

        // W/S controls both tracks together (forward/back)
        float forward = Keyboard.current.wKey.isPressed ? 1f :
                        Keyboard.current.sKey.isPressed ? -1f : 0f;

        // A/D creates track differential for turning
        float turn = Keyboard.current.aKey.isPressed ? 1f :
                     Keyboard.current.dKey.isPressed ? -1f : 0f;

        ca[0] = forward - turn;   // left track
        ca[1] = forward + turn;   // right track
        ca[2] = Keyboard.current.qKey.isPressed ? -1f :
                Keyboard.current.eKey.isPressed ? 1f : 0f;  // turret
    }
}