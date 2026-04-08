# Run 09 Reward Structure

# Discrete Rewards

Capture the point: 1f | CapturePoint.cs | OnCaptured()
Destroy the enemy: 0.5f | EnvironmentManager.cs | OnTankDestroyed()
Hit the enemy: 0.5f | TankShell.cs | OnCollisionEnter()
Missed shot: -0.05f | TankShell.cs | OnCollisionEnter() for wall hits & OnDestroy() for full misses
Hit by enemy: -0.25f | TankHealth.cs | TakeDamage()
Death penalty: -0.5f | EnvironmentManager.cs | OnTankDestroyed()
Lose the capture point: -1f | EnvironmentManager.cs | OnCapturePointCaptured()

# Continuous Rewards

Capture point progress: 0.1f | TankyAgent.cs | OnActionReceived()
Capture point distance: ~0.005f max | TankyAgent.cs | OnActionReceived()
Turret alignment: ~0.006f max | TankyAgent.cs | OnActionReceived(), gated behind enemyInSight
Step penalty: -0.001f naive | TankyAgent.cs | OnActionReceived()

# Perfect Episode Estimate

Capture +1f
1 hit +0.5f
Capture distance ~+1f (crossed map to capture the point)
Capture progress +1f (captured point completely)
Turret alignment +0.25f
Step penalty ~-0.3f

Total: 3.45f

# Worst Episode Estimate

Lost capture point -1f
Hit by enemy -0.25f
Missed shots -1f
Step penalty ~-4.5f
Capture distance ~-1f (crossed map to run away like a coward!)
Turret alignment -0.25f

Total: -8f

# Long-term considerations

Avoiding detection, avoiding obstacles
