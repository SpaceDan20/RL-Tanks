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
    public float shellSpeed = 30f;
    public float gunReloadTime = 5f;
    public Transform barrelTip;
    public GameObject shellPrefab;
    private float fireCooldown;

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

    [Header("Debug")]
    public bool debugLogging = false;

    [Header("References")]
    public Transform environmentCenter;
    public EnvironmentManager environmentManager;
    public CapturePoint capturePoint;
    public float maxCapturePointDistance = 100f;

    private Rigidbody rb;
    private TankHealth tankHealth;
    private float previousAlignmentPotential;
    private float previousCapturePointDistance;
    private float previousCaptureProgress;
    private float captureProgressBudget;
    private float episodeAlignmentReward;
    private float episodeCapturePointReward;
    private float episodeCaptureProgressReward;
    private bool enemyInSight;
    private float velocityLogTimer = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        tankHealth = GetComponent<TankHealth>();
        rb.sleepThreshold = 0.01f;
        accelerationRate = maxSpeed / accelerationTime;
        decelerationRate = maxSpeed / decelerationTime;
        brakeRate = decelerationRate * brakeMultiplier;
    }

    public void LogEpisodeStats()
    {
        if (debugLogging)
            Debug.Log($"[{gameObject.name}] Episode ended — Alignment: {episodeAlignmentReward:F4} | CapturePoint Distance: {episodeCapturePointReward:F4} | CapturePoint Progress: {episodeCaptureProgressReward:F4}");
    }

    public override void OnEpisodeBegin()
    {
        // Fallback: log episodes that ended via max steps (EnvironmentManager logs capture/destroy endings)
        LogEpisodeStats();

        // Reset velocity and angular velocity on episode start
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep(); // force sleep to ensure it doesn't start with residual motion

        // Reset firing cooldown
        fireCooldown = 0f;

        enemyInSight = false;

        // Reset per-episode reward totals
        episodeAlignmentReward = 0f;
        episodeCapturePointReward = 0f;
        episodeCaptureProgressReward = 0f;

        // Limit total shaping reward from capture progress to 1.0f per episode
        captureProgressBudget = 1f;

        // Reset health
        tankHealth.ResetHealth();

        // Compute spawn assignments (once per episode) then apply only this agent's spawn
        environmentManager.ResetEpisode();
        environmentManager.SpawnSelf(this);

        // Reset turret rotation
        turret.localRotation = Quaternion.identity;

        // Seed previous potentials from actual starting state so the first shaping
        // step reflects real improvement rather than a jump from zero
        previousCapturePointDistance = Vector3.Distance(transform.position, capturePoint.transform.position);
        previousAlignmentPotential = GetAlignmentPotential();
        previousCaptureProgress = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        enemyInSight = false; // Reset enemy in sight flag at the start of observation collection

        // --- Proprioception ---
        sensor.AddObservation(Vector3.Dot(rb.linearVelocity, transform.forward) / maxSpeed);  // Forward velocity [-1, 1]
        sensor.AddObservation(Vector3.Dot(rb.linearVelocity, transform.right) / maxSpeed);    // Lateral velocity [-1, 1]
        sensor.AddObservation(rb.angularVelocity.y / (maxTurnRate * Mathf.Deg2Rad));          // Yaw rate [-1, 1]

        // --- Health ---
        sensor.AddObservation(tankHealth.NormalizedHealth); // [0, 1]

        // --- Capture point ---
        float captureDistance = Vector3.Distance(transform.position, capturePoint.transform.position);
        sensor.AddObservation(Mathf.Clamp01(captureDistance / maxCapturePointDistance));       // Normalized distance [0, 1]
        Vector3 dirToCapture = (capturePoint.transform.position - transform.position).normalized;
        sensor.AddObservation(Vector3.SignedAngle(transform.forward, dirToCapture, Vector3.up) / 180f); // Angle to capture point [-1, 1]
        sensor.AddObservation(capturePoint.IsBeingCapturedBy(this) ? 1f : 0f);                // 1 if this agent is capturing
        sensor.AddObservation(capturePoint.IsBeingCapturedByEnemy(this) ? 1f : 0f);           // 1 if an enemy is capturing
        
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

        // Discrete action for firing (only allowed when combat is enabled by the curriculum)
        int fireAction = actions.DiscreteActions[0]; // 0 = no fire, 1 = fire
        if (environmentManager.CombatEnabled && fireCooldown <= 0.01f)
        {
            // Able to fire
            if (fireAction == 1)
            {
                GameObject shellObj = Instantiate(shellPrefab, barrelTip.position, barrelTip.rotation);
                TankShell shell = shellObj.GetComponent<TankShell>();
                shell.Initialize(this, shellSpeed, shellDamage);
                fireCooldown = gunReloadTime;
            }
        }

        if (enemyInSight && environmentManager.CombatEnabled) // Only reward turret alignment improvement when an enemy is detected in the sensor arrays and combat is enabled
        {
            float currentAlignmentPotential = GetAlignmentPotential();
            float alignmentReward = (currentAlignmentPotential - previousAlignmentPotential) * 0.25f;
            AddReward(alignmentReward);
            episodeAlignmentReward += alignmentReward;
            previousAlignmentPotential = currentAlignmentPotential;
        }

        // Reward shaping for moving towards the capture point
        float currentCapturePointDistance = Vector3.Distance(transform.position, capturePoint.transform.position);
        float capturePointReward = (previousCapturePointDistance - currentCapturePointDistance) / maxCapturePointDistance * 0.75f; // Scaled to a maximum of 0.75f reward per episode for moving from max distance to the point
        AddReward(capturePointReward);
        episodeCapturePointReward += capturePointReward;
        previousCapturePointDistance = currentCapturePointDistance;

        // Reward shaping for capture progress (only while this agent is capturing, capped at 1f total per episode)
        if (capturePoint.IsBeingCapturedBy(this) && captureProgressBudget > 0f)
        {
            float currentCaptureProgress = capturePoint.CaptureProgress;
            float captureProgressReward = Mathf.Min(
                (currentCaptureProgress - previousCaptureProgress) / capturePoint.captureTime,
                captureProgressBudget);
            AddReward(captureProgressReward);
            episodeCaptureProgressReward += captureProgressReward;
            captureProgressBudget -= captureProgressReward;
            previousCaptureProgress = currentCaptureProgress;
        }
        else
        {
            previousCaptureProgress = 0f; // Reset silently so the next capture starts without a spike
        }

        // Step penalty
        AddReward(-0.00017f);
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
        // Calculate how well the turret is aligned with the nearest enemy (1 = perfectly aligned, 0 = opposite direction)
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
            fireCooldown -= Time.deltaTime;

        // Periodic velocity debug log (gated on debugLogging)
        if (debugLogging)
        {
            velocityLogTimer -= Time.unscaledDeltaTime;
            if (velocityLogTimer <= 0f)
            {
                velocityLogTimer = 5f; // Log every 5 seconds
                // Log velocities
                float fwd = Vector3.Dot(rb.linearVelocity, transform.forward);
                float lat = Vector3.Dot(rb.linearVelocity, transform.right);
                float yaw = rb.angularVelocity.y * Mathf.Rad2Deg;
                Debug.Log($"[{gameObject.name}] Velocity — Forward: {fwd:F2} m/s | Lateral: {lat:F2} m/s | Yaw: {yaw:F2} °/s");
                // Log capture point info
                float capDist = Vector3.Distance(transform.position, capturePoint.transform.position);
                float capDistNorm = Mathf.Clamp01(capDist / maxCapturePointDistance);
                Vector3 dirToCapture = (capturePoint.transform.position - transform.position).normalized;
                float capAngle = Vector3.SignedAngle(transform.forward, dirToCapture, Vector3.up);
                bool isCap = capturePoint.IsBeingCapturedBy(this);
                bool enemyCap = capturePoint.IsBeingCapturedByEnemy(this);
                Debug.Log($"[{gameObject.name}] Capture Point — Dist: {capDistNorm:F2} ({capDist:F1} m) | Angle: {capAngle:F1}° | Capturing: {isCap} | Enemy Capturing: {enemyCap}");
            }
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

    public void OnCollisionEnter(Collision collision)
    {
        // Check for tag of collided object to check if it's a wall
        if (collision.collider.CompareTag("Wall"))
        {
            // Penalize for colliding with walls
            AddReward(-0.05f);
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        var da = actionsOut.DiscreteActions;

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
        da[0] = Keyboard.current.spaceKey.isPressed ? 1 : 0; // fire
    }
}