# Reward Structure

## Discrete Rewards

- Capture the point: 2f | CapturePoint.cs | OnCaptured()
- Destroy the enemy: 1f | EnvironmentManager.cs | OnTankDestroyed()
- Hit the enemy: 0.5f | TankShell.cs | OnCollisionEnter()
- Missed shot: -0.05f | TankShell.cs | OnCollisionEnter() for wall hits & OnDestroy() for full misses
- Hit a wall: -0.05f | TankyAgent.cs | OnCollisionEnter()
- Hit by enemy: -0.25f | TankHealth.cs | TakeDamage()
- Death penalty: -1f | EnvironmentManager.cs | OnTankDestroyed()
- Lose the capture point: -1.5f | EnvironmentManager.cs | OnCapturePointCaptured()

## Continuous Rewards (per second)

- Capture point progress: 0.1f (1f total) | TankyAgent.cs | OnActionReceived()
- Capture point distance: 0.0525f (0.75f max) | TankyAgent.cs | OnActionReceived()
- Turret alignment: ~0.004f (0.25f max) | TankyAgent.cs | OnActionReceived(), gated behind enemyInSight
- Step penalty: -0.0085f naive (0.51f max) | TankyAgent.cs | OnActionReceived()

## Perfect Capture-Only Episode Estimate

- Capture +2f
- Capture progress +1f
- Capture distance +0.45f (from far spawn)
- Step penalty -0.2f (~20-25/60s)

### Total = 3.25f

## Worst Capture-Only Episode Estimate

- Wall hits -1f (kept hitting walls)
- Capture distance -0.4f (ran away from close spawn)
- Step penalty -0.5f (almost full episode)

### Total = -1.9f

## Perfect Episode Estimate

- Capture +2f
- Capture progress +1f
- Capture distance +0.7f (from far spawns)
- 1 hit +0.5f
- Turret alignment +0.25f
- Step penalty -0.3f (~35/60s)

### Total: 4.15f

## Worst Episode Estimate

- Lost capture point -1.5f
- Capture distance -0.25f (ran away from mid spawn)
- Hit by the enemy -0.25f
- Turret alignment -0.25f (doesn't wanna look at danger!)
- Missed shots -0.4f (8 shots on 7s reload)
- Hit some walls -0.5f
- Step penalty -0.5f (almost full episode)

### Total: -3.65f

## Long-term considerations

- Avoiding detection
- Avoiding obstacles
