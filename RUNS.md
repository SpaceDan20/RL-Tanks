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
