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

## Run 15

### Hypothesis:

The reward restructuring clearly helped the tanks develop more unique and interesting behavior, but they still struggled to learn basic movement. I have adjusted the curriculum and added new, seperate rooms for the tanks to independently learn how to move toward capture points. By starting off in this simplifed environment and first learning movement and capture without having to contest the point, the agents should be able to learn movement and capturing more effectively. Once they adequately learn how to drive to the point from the farthest spawns (~50m), only then are they brought to the main battlefield to compete for the capture point. Finally, combat can be introduced.

### Changes:

- Curriculum rework
  - Added 3 independent levels in an isolated area focused on capturing prior to main battlefield

### Results:

Progress. After 1M steps, the tanks are far more mobile than they ever were in the past 14 runs. However, they were not able to advance the curriculum inside of the independent, level 1 areas. They stayed in the first curriculum, spawning ~10m from the capture point every episode. Whenever they were spawned facing the capture point, they reliably drove forward and captured it. But when they were spawned facing away, or sideways to it, they struggled to turn into it. They sometimes avoided smacking into walls, but sometimes chose just to smack into them (maybe the capture point is behind them!). I've learned that movement in this environment is more complex to learn that I originally thought.

## Run 16

### Hypothesis:

Teaching the tanks to move in this environment is complex. More importantly, my observation space likely needs a massive rework similar to the reward landscape rework. There is currently only one capture-point-related observation, distance to the capture point, which by itself is not good enough to tell the agent where to go. This lone observation, bad enough as it is, has to also fight 207 other, nonrelevant observations each step when the tank is learning how to move toward the capture point. A few additions that will likely help the agent in this capture-only navigation task are adding observations related to proprioception, like its own velocities, and adding more capture-point-related observations like what angle the agent's front is in relation to the capture point.

### Changes:

- Observation space rework
  - Added linear, lateral, and angular velocity observations
  - Added observation: Angle to the capture point
  - Removed 8 raycast sensors (raycast observations decreased: 204 --> 180)
- Removed 'capture_only' lesson from curriculum

### Results:

We got a pair of curious georges here. The tanks were even more mobile than run 15, and they demonstrated much more advanced movement behavior (driving around, turning, circling around the capture point). They did good and learned to capture the point (occasionally driving toward the center), meaning the shaped distance reward is working properly. However, although they came close, they never advanced to the farther spawns in level 1. The agents learned to generate ~0.1-0.15f curiosity rewards (on the curiosity NN), leading to hyperexploration. They were so eager to explore that they would slam into walls to do so (or maybe they are trying to escape!).

## Run 17

### Hypothesis:

The issue likely lies in reward magnitudes again. In the very first level, tanks spawn ~10m away from the capture point (20m from the center). This means they are only able to generate a maximum 0.1f out of the 0.5f reward for closing distance to the capture point. This is the only shaped reward at this stage that doesn't involve being inside of the capture circle, and it is dwarfed by the step penalty (-1.05f for episode timeout). This encourages exploration, as agents can reliably generate 0.05f-0.1f curiosity rewards per earlier episodes by just driving around recklessly. The 2f capture reward + 1f capture progress reward help them target the point occasionally, but exploration early on pulls their attention away from it (they'll even drive around the backside of the capture point!).

Also, the thresholds for advancing are likely too high, particularly for level 1a. Lowering the thresholds will allow them to advance to farther spawns, which increases their potential closing distance shaped reward maximum from 0.1f --> 0.175f --> 0.3f. This should also, over time, encourage exploitation over exploration.

Lastly, a good portion of time early on in run 16 was spent slamming and driving into walls. They eventually learned to turn and drive alongside the wall, but this behavior only emerged late in training. Adding a small wall/obstacle collision penalty should encourage learning in how to navigate walls, allowing for quicker exploration.

### Changes:

- Rewards reworked
  - Step penalty halved: 1.05f max --> 0.51f
  - Capture point distance reward max increased: 0.5f --> 0.75f
  - Wall collision penalty added --> -0.05f
- Curriculum thresholds tweaked
  - level1a: 1 --> 0.25
  - level1b: 1.5 --> 0.75
  - level1c: 1.8 --> 1.5
  - capture_and_combat: 2.0 --> 2.5

### Results:

The agents continued to show more advanced movement behavior, including backing up into the capture point from 25m away. However, they were not able to advance to the farthest spawn. The curriculum advanced to medium spawns at ~300k steps and slowed them down.

## Run 18

### Hypothesis:

Curiosity seems to be hurting the agents more than helping them. It is likely adding noise to an already packed signal. Given the denser reward environment, curiosity is likely not warranted. Looking back on all of the previous runs shows that curiosity reward has always been a pretty large signal. While confining the agents to the isolated level 1 rooms drastically reduced overall curiosity rewards for runs 15-17, the rewards are still likely interfering with the overall signal. By removing curiosity completely, the agents may stop getting distracted with exploring unimportant sections of the area and more adequately follow the dense reward signals instead.

### Changes:

- Removed curiosity

### Results:

The tanks finally reached the main battlefield. By resuming the training from 1M steps to 3M, enough progress was made to pass the 1.5 mean reward threshold at step ~1.8M. As expected, overall performance dropped out once the agents were introduced to the new battlefield environment. However, their performance mostly stagnated for the remaining 1.2M steps. The agents were more mobile than they've ever been in the main battlefield, but the added combat mechanics started to pull their attention back to little movement and focusing on combat. The agents didn't learn to capture the single point in the main battlefield reliably, leading to the same default back to finding and destroying the enemy instead.

## Run 19

### Hypothesis:

The previous run demonstrated that the learning loop works. My reward structure and observation space reworks allowed the tanks to genuinely learn throughout the run. However, the environment, mainly the main battlefield, is likely in need of a serious rework. Currently, the main battlefield is a chaotic environment with one capture point that spawns 25-35 tank-sized containers (good for cover) randomly throughout the map. Once the agents spawn in this environment, more often than not are they able to immediately see and destroy one another within ~10s (1 reload). If the ultimate goal is getting the agents to capture and contest the point, the tanks should spawn in a somewhat more predictable environment where they cannot be destroyed right off the bat.

TODO:

- Modify the main battlefield
- Modify the random container spawning logic
