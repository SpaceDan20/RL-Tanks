# Tanky Training Runs

## Run 01

- Kill reward +1, Hit reward +0.5
- Sight reward +0.001
- Step penalty -0.001
- Death penalty -1

### Result:

Tanks didn't learn very well. Tanks may have been reward hacking the sight reward, and they spam fired the whole time.

## Run 02

### Hypothesis:

Sight reward was potentially hacked. No penalties for missed shots meant the agents simply learned to keep holding down the trigger, which is fine for now but bad once ammo is limited later on.

### Changes:

- Increased kill reward: +1.0 --> +2.5
- Increased hit reward: +0.5 --> +1.0
- Added missed shot penalty: -0.01
- Added getting hit penalty: -0.25

### Result:

Tanks still didn't learn. They followed the same trajectory as before.

## Run 03

### Hypothesis:

Rewards are okay, but the agents may not have enough learning capacity to accurately learn what needs to be learned in the environment.

### Changes:

- YAML reconfigued:
  - Buffer Size increased: 20,000 --> 50,000
  - Batch Size: 512 --> 1024
  - Time Horizon: 128 --> 256

### Result:

No meaningful improvement over run 02

## Run 04

**Same config, testing with new tank model**

### Result:

No meaningful improvement over run 03

## Run 05

### Hypothesis:

The tanks initial spawns are too far away, making it hard for them to learn.

### Changes:

Curriculum learning introduced. 3 sets of spawn points were added: close, medium, and far.

### Result:

Curriculum advanced too quickly. Tanks likely got lucky in the small amount of episodes (20) required to advance and spiked the smoothed average high enough to advance early.

## Run 06

### Hypothesis:

Curriculum's current config causes advancement to come too quickly.

### Changes:

- Curriculum reconfigured:
  - min_lesson_length increased: 20 --> 50
  - thresholds increased: -3.5 --> -2.5 for medium and -2.5 --> -1.0 for far

### Result:

Curriculum did not advance too quickly, but tanks still struggled to learn. Spam firing is still an issue, and they are struggling to center each other properly.

## Run 07

### Hypothesis:

Sparse rewards are making learning difficult for the tanks.

### Changes:

- PBRS reward for turret aiming introduced.
- Rewards tweaked:
  - Kill reward decreased: 2.5 --> 0.5
  - Hit reward decreased: 1.0 --> 0.5
  - Missed shot penalty increased: 0.01 --> 0.05
  - Death penalty decreased: -1 --> -0.5
- Curriculum YAML tweaked:
  - min_lesson_length increased: 50 --> 100

### Result:

Chaotic. Tanks struggle still, but the PBRS turret aiming reward clearly helped. Tanks can now somewhat track their opponent with their turrets.

## Run 08

### Hypothesis:

The reward structure, PBRS turret aiming reward, and curriculum are all working. The agents may just need more steps overall. This run will also test if the agents can learn with the new projectile system over the prior raycast system.

### Changes:

- YAML max_steps increased: 500k --> 1M

### Result:

Tanks learned well and got effective until they advanced to the far spawns. Once they were too far away for their sensors to quickly pick up the enemy, their effectiveness dropped out.

## Run 09

### Hypothesis:

The tanks are struggling to learn movement, which is required once the spawnpoints are far enough away. By adding a capture point and a big reward for capturing it, the tanks will have an incentive to move. Once curriculum advances to the far spawns, the tanks will likely have learned to move, eliminating the problem of not being able to find one another.

### Changes:

- Added capture point
- New reward for capturing point: +1f
- New penalty for losing point: -1f
- Missed shot now counts all missed shots
- Observation for own health added
- Observation for distance to capture point added
- Observation for agent capturing added
- Observation for enemy capturing added
- Total observation space increased: 204 --> 208

### Result:

The tanks struggled to learn movement still. Although they occasionally captured the point, they did not develop the movement behavior necessary to consistently capture the point. Most episodes ended by timeout or kills.

## Run 10

### Hypothesis:

The sparse reward structure for capturing the point, combined with the overall complexity of the learning task at hand, likely made it very difficult for the agent to learn that moving toward the capture point is a good behavior. The added complexity of the capture point may need a curriculum adjustment in the future. For now, a simple PBRS reward for closing distance to the capture point may introduce movement finally.

### Changes:

- Added new PBRS reward for closing distance to the capture circle
- Lower strength multiplier of PBRS rewards

### Result:

The agents were doing well and learning reliably up until the spawn distance increased from close to medium, as seen in other runs. They did not learn how to reliably capture the point, but they did learn how to center their turrets once an enemy was detected.

## Run 11

### Hypothesis:

The sparse reward for capturing a point (+1f) after a long ten seconds makes it hard for the agents to learn that capturing a point is a good behavior. It is also overshadowed by destroying an enemy, as that rewards the agent more (+1.5f just in discrete rewards). The environment is inherently complex and may need a staged approach. More importantly, the PBRS rewards are not optimal for the scenario. Turret alignment does not guarantee an immediate episode finish, and neither does closing distance, which makes the 'optimal' states not fully optimal. The gamma discount factor causes the shaped rewards to almost always come out negative. Changing out the PBRS rewards for naive-shaped rewards that still use potential-based logic should fix the overall reward structure problem. An additional continuous reward for capture zone progress will also solve the credit assignment problem of the agent previously having to wait 10s for a small 1f reward.

### Changes:

- Converted turret alignment and capture point distance PBRS rewards to naive rewards
- Added continuous capture progress reward

### Result:

Search behavior has emerged. When an agent cannot see the enemy, he will spin his turret around until he can find them. The agents did learn to move more at first, but after they moved from close to medium spawns, they quit moving to prioritize finding the enemy. The overall reward structure seems to be working, but the agents struggle with the complex environment. A staged curriculum approach would be best to introduce a natural flow (learn to move --> learn to capture --> learn to engage enemies in combat)

## Run 12

### Hypothesis:

The environment and reward structure are too complex for the agent to learn outright. The current reward structure is optimized for capturing (+3f max) over combat (+1.75f max), but the agent is likely to learn pure combat over capturing given the complexity of movement required to reach and capture the zone. By introducing a staged curriculum (capture only --> capture from farther away --> capture & combat --> capture & combat from afar), the agent should, hypothetically, learn to capture before all else. Once combat is enabled, emergent behaviors for contesting the point should naturally start to develop.

### Changes:

- Introduced a new, staged curriculum where combat is disabled until 400,000 steps (~halfway through training).
  - Only capture-point rewards can be achieved until combat is enabled
  - Spawn points still follow close --> medium --> far, where combat is introduced at medium distances

### Results:

Epic fail. The agents failed to learn capturing, even after 250k steps of spawning right next to the point. Once combat was enabled at 400k steps, they slowly learned how to destroy one another to optimize their rewards. The agents didn't grow very capable in any aspect throughout the 750k step run.

## Run 13

### Hypothesis:

The max step per episode is currently 5000 steps, or 100 seconds, at a Unity fixed timestep of 0.02 (50 updates per second). Lowering this to 3000 steps, 1 minute exactly, would reflect the environment better, as it only takes ~30 seconds maximum to capture a point when spawning from afar in an optimal scenario, where the agent is perfect. The extra 40 seconds is more often than not wasted, simply accumulating step penalties for an agent that cannot find the capture point. Lowering the max steps will allow for more episodes, and hopefully, more learning. If the agents still can't learn in the allotted 400k combat-disabled steps, reward magnitudes will likely need adjustment.

### Changes:

- Lower max steps per episode: 5000 --> 3000

### Results:

No improvements. Like run 12, the tanks failed to learn capturing, movement, and any meaningful combat.

## Run 14

### Hypothesis:

The reward magnitudes and overall structure are likely broken. With a worst-case scenario (~-8f with combat, ~-5f without) greatly exceeding the best-case (~+3.5f with combat, ~+2.75f without), I have created a pessimistic reward landscape. The agents (their policy) have likely spent most of their time avoiding bad behavior rather than pursuing good behavior. This has led to little exploration and little to no movement. By adjusting the reward magnitude and creating a more optimistic reward landscape by amplifying rewards and decreasing penalties, the agents should start to explore good behaviors more reliably. Most notably, the step penalty in the past 13 runs has vastly outweighed the other rewards and penalties, dragging the mean rewards down substantially. With a drastically reduced step penalty and a reweighted reward/penalty landscape, the agent is much less punished and much more rewarded overall.

### Changes:

- Restructured reward magnitudes:
  - Capture reward increased: 1f --> 2f
  - Destroy reward increased: 0.5f --> 1f
  - Death penalty increased: -0.5f --> -1f
  - Losing capture point penalty increased: -1f --> -1.5f
  - Capture point distance reward/penalty decreased: 1f max --> 0.5f
  - **Step penalty severely decreased**: -3f max --> -1.05f

### Results:

Little improvement. The agents were more mobile than they've ever been, but they shimmied more than anything. Once again, the jump from close to medium-range spawnpoints dropped out progress. They agents had not learned movement - they learned to shimmy. But this is not a dance battle.
