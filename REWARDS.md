# Reward Structure

## Discrete Rewards

- Capture the point: 1.5f | CapturePoint.cs | OnCaptured()
- Destroy the enemy: 1f | EnvironmentManager.cs | OnTankDestroyed()
- Hit the enemy: 0.5f | TankShell.cs | OnCollisionEnter()
- Missed shot: -0.05f | TankShell.cs | OnCollisionEnter() for wall hits & OnDestroy() for full misses
- Hit a wall or obstacle: -0.05f | TankyAgent.cs | OnCollisionEnter()
- Hit by enemy: -0.25f | TankHealth.cs | TakeDamage()
- Death penalty: -1f | EnvironmentManager.cs | OnTankDestroyed()
- Lose the capture point: -1.5f | EnvironmentManager.cs | OnCapturePointCaptured()

## Continuous Rewards (per second)

- Capture point distance: 0.0525f (0.75f max) | TankyAgent.cs | OnActionReceived()
- Capture point progress: 0.05f (0.5f max) | CapturePoint.cs | Update()
- Turret alignment: ~0.004f (0.25f max) | TankyAgent.cs | OnActionReceived(), gated behind enemyInSight
- Step penalty: -0.0085f naive (-0.51f max) | TankyAgent.cs | OnActionReceived()

## Perfect/Worst Level 1 Estimate

- Capture +1.5f
- Capture progress +0.5f
- Capture distance +0.22f
- Step penalty -0.2f (~20/60s)
  Total: 2.02f

- Capture distance -0.2f
- Wall hits -0.5f (10 collisions)
- Step penalty -0.7f (almost full episode)
  Total: -1.4f

## Perfect/Worst Level 2 Estimate

- Capture +1.5f
- Capture progress +0.5f
- Capture distance +0.35f
- Step penalty -0.35f (~30/60s)
  Total: 2.0f

- Capture distance -0.15f
- Wall/obstacle hits -0.5f (10 collisions)
- Step penalty -0.7f (almost full episode)
  Total: -1.35f

## Perfect/Worst Level 3 Estimate

- Capture +1.5f
- Capture progress +0.5f
- Capture distance +0.4f
- Step penalty -0.35f (~30/60s)
  Total: 2.05f

- Capture distance -0.15f
- Wall/obstacle hits -0.5f (10 collisions)
- Step penalty -0.7f (almost full episode)
  Total: -1.35f

## Perfect/Worst Battle Episode Estimate

- Capture +1.5f
- Capture progress +0.5f
- Capture distance +0.7f (from farther spawns)
- 1 hit +0.5f
- Turret alignment +0.25f
- Step penalty -0.35f (~30/60s)
  Total: 3.10f

- Lost capture point -1.5f
- Capture distance -0.2f (ran away from closer spawn)
- Hit by the enemy -0.25f
- Turret alignment -0.25f (doesn't wanna look at danger!)
- Missed shots -0.4f (8 shots on 7s reload)
- Hit some walls -0.5f
- Step penalty -0.7f (almost full episode)
  Total: -3.8f

## Long-term considerations

- Avoiding detection
- Avoiding obstacles
