Wheely ML Agents Project

Project Summary:
A single-agent sandbox environment where Wheely, my first robot, learns to navigate a 50x50 sandbox.
In the sandbox is a SphereOfInterest that Wheely must learn to touch.

What worked:
A sparse reward structure worked better than a dense reward structure as quirky behaviors took over when using the dense reward structure.
Increasing Wheely's sensor range from 10 units to 20 units allowed Wheely to learn faster and more effectively given the large sandbox size.
Keeping curiosity and beta values low (~0.005) allowed Wheely to reap the benefits of intrinsic motivation without it dominating the learning process.

What didn't work:
A dense reward structure with intermediate rewards caused Wheely to learn quirky behaviors that were not aligned with the ultimate goal of touching the SphereOfInterest.
Larger penalties for hitting the walls caused Wheely to learn to avoid the walls at all costs, which hindered exploration and learning.

Current State:
Run 12 finished at 750,000 steps. Wheely struggles to learn how to navigate away from walls, but is very good at finding and touching the SphereOfInterest.
