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

Turret alignment: PBRS | TankyAgent.cs | OnActionReceived(), gated behind enemyInSight
Step penalty: -0.001f naive | TankyAgent.cs | OnActionReceived()

# Perfect Episode Estimate

Capture +1f
1 hit +0.5f
Turret alignment ~+0.05f (need to observe)
Step penalty ~-0.3f

Total: 1.25

# Worst Episode Estimate

Lost capture point -1f
Hit by enemy -0.25f
Missed shots -1f
Step penalty ~-4.5f

Total: -6.75

# Long-term considerations

Avoiding detection, avoiding obstacles, movement
