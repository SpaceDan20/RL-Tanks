Tanky Training Runs

Run 01 --
Kill reward +1, Hit reward +0.5
Sight reward +0.001
Step penalty -0.001
Death penalty -1

01 result:
Tanks didn't learn very well. Tanks may have been reward hacking the sight reward, and they spam fired the whole time.

---

Run 02 --
Hypothesis:
Sight reward was potentially hacked. No penalties for missed shots meant the agents simply learned to keep holding down the trigger, which is fine for now but bad once ammo is limited later on.
Changes:
Increased kill reward: +1.0 --> +2.5
Increased hit reward: +0.5 --> +1.0
Added missed shot penalty: -0.01
Added getting hit penalty: -0.25

02 result:
Tanks still didn't learn. They followed the same trajectory as before.

---

Run 03 --
Hypothesis:
Rewards are okay, but the agents may not have enough learning capacity to accurately learn what needs to be learned in the environment.
Changes:
YAML reconfigued:
Buffer Size increased: 20,000 --> 50,000
Batch Size: 512 --> 1024
Time Horizon: 128 --> 256

03 result:
No meaningful improvement over run 02

---

Run 04 --
**Same config, testing with new tank model**

04 result:
No meaningful improvement over run 03

---

Run 05 --
Hypothesis:
The tanks initial spawns are too far away, making it hard for them to learn.
Changes:
Curriculum learning introduced. 3 sets of spawn points were added: close, medium, and far.

05 result:
Curriculum advanced too quickly. Tanks likely got lucky in the small amount of episodes (20) required to advance and spiked the smoothed average high enough to advance early.

---

Run 06 --
Hypothesis:
Curriculum's current config causes advancement to come too quickly.
Changes:
Curriculum reconfigured:
min_lesson_length increased: 20 --> 50
thresholds increased: -3.5 --> -2.5 for medium and -2.5 --> -1.0 for far

06 result:
Curriculum did not advance too quickly, but tanks still struggled to learn. Spam firing is still an issue, and they are struggling to center each other properly.

---

Run 07 --
Hypothesis:
Sparse rewards are making learning difficult for the tanks.
Changes:
PBRS reward for turret aiming introduced.
Rewards tweaked:
Kill reward decreased: 2.5 --> 0.5
Hit reward decreased: 1.0 --> 0.5
Missed shot penalty increased: 0.01 --> 0.05
Death penalty decreased: -1 --> -0.5
Curriculum YAML tweaked:
min_lesson_length increased: 50 --> 100

07 result:
Chaotic. Tanks struggle still, but the PBRS turret aiming reward clearly helped. Tanks can now somewhat track their opponent with their turrets.

---

Run 08 --
Hypothesis:
The reward structure, PBRS turret aiming reward, and curriculum are all working. The agents may just need more steps overall. This run will also test if the agents can learn with the new projectile system over the prior raycast system.
Changes:
YAML max_steps increased: 500k --> 1M

08 result:
Tanks learned well and got effective until they advanced to the far spawns. Once they were too far away for their sensors to quickly pick up the enemy, their effectiveness dropped out.

---

Run 09 --
Hypothesis:
The tanks are struggling to learn movement, which is required once the spawnpoints are far enough away. By adding a capture point and a big reward for capturing it, the tanks will have an incentive to move. Once curriculum advances to the far spawns, the tanks will likely have learned to move, eliminating the problem of not being able to find one another.

Changes:

- Added capture point
- New reward for capturing point: +1f
- New penalty for losing point: -1f
- Missed shot now counts all missed shots
- Observation for own health added
- Observation for distance to capture point added
- Observation for agent capturing added
- Observation for enemy capturing added
- Total observation space increased: 204 --> 208

09 result:
The tanks struggled to learn movement still. Although they occasionally captured the point, they did not develop the movement behavior necessary to consistently capture the point. Most episodes ended by timeout or kills.

---

Run 10 --
Hypothesis:
The sparse reward structure for capturing the point, combined with the overall complexity of the learning task at hand, likely made it very difficult for the agent to learn that moving toward the capture point is a good behavior. The added complexity of the capture point may need a curriculum adjustment in the future. For now, a simple PBRS reward for closing distance to the capture point may introduce movement finally.

Changes:

- Added new PBRS reward for closing distance to the capture circle
- Lower strength multiplier of PBRS rewards

10 result:
The agents were doing well and learning reliably up until the spawn distance increased from close to medium, as seen in other runs. They did not learn how to reliably capture the point, but they did learn how to center their turrets once an enemy was detected.

---

Run 11 --
Hypothesis:
The sparse reward for capturing a point (+1f) after a long ten seconds makes it hard for the agents to learn that capturing a point is a good behavior. It is also overshadowed by destroying an enemy, as that rewards the agent more (+1.5f just in discrete rewards). The environment is inherently complex and may need a staged approach. More importantly, the PBRS rewards are not optimal for the scenario. Turret alignment does not guarantee an immediate episode finish, and neither does closing distance, which makes the 'optimal' states not fully optimal. The gamma discount factor causes the shaped rewards to almost always come out negative. Changing out the PBRS rewards for naive-shaped rewards that still use potential-based logic should fix the overall reward structure problem. An additional continuous reward for capture zone progress will also solve the credit assignment problem of the agent previously having to wait 10s for a small 1f reward.

Changes:

- Converted turret alignment and capture point distance PBRS rewards to naive rewards
- Added continuous capture progress reward

11 result:
TBD
