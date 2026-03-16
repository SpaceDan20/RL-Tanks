using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstacleEntry
{
    public GameObject prefab; // The obstacle prefab to spawn
    public float spawnHeight; // The height at which to spawn the obstacle
}

public class EnvironmentManager: MonoBehaviour
{
    [Header ("Obstacles")]
    public ObstacleEntry[] obstacles; // Array of obstacle prefabs to spawn
    public int minObstacles = 10;
    public int maxObstacles = 25;

    [Header("Spawn Settings")]
    public Transform groundPlane;
    public Transform obstaclesParent;
    public float spawnPadding = 5f; // Padding from the edges of the ground plane
    public float minDistanceFromTanks; // Minimum distance from tanks to spawn obstacles

    [Header("Tanks")]
    public Transform[] tankSpawnPoints;
    public TankyAgent[] tanks;

    private List<GameObject> spawnedObstacles = new List<GameObject>(); // List to keep track of spawned obstacles

    private void Start()
    {
        SpawnObstacles();
    }

    public void ClearObstacles()
    {
        // Clear existing obstacles
        foreach (GameObject obstacle in spawnedObstacles)
        {
            Destroy(obstacle);
        }
        spawnedObstacles.Clear(); // Clear the list after destroying obstacles
    }

    public void SpawnObstacles()
    {
        // Randomly determine the number of obstacles to spawn within the specified range
        int obstacleCount = Random.Range(minObstacles, maxObstacles + 1);
        
        for (int i = 0; i < obstacleCount; i++)
        {
            // Randomly select an obstacle prefab
            ObstacleEntry selectedEntry = obstacles[Random.Range(0, obstacles.Length)];
            // Generate a random position within the ground plane bounds, considering padding
            Vector3 spawnPosition = Vector3.zero;
            int attempts = 0;
            int maxAttempts = 100; // To prevent infinite loops in case of tight spaces
            bool validPosition = false;
            while (!validPosition && attempts < maxAttempts)
            {
                attempts++;
                float xPos = Random.Range(groundPlane.position.x - groundPlane.localScale.x * 10f / 2f + spawnPadding,
                                            groundPlane.position.x + groundPlane.localScale.x * 10f / 2f - spawnPadding);
                float zPos = Random.Range(groundPlane.position.z - groundPlane.localScale.z * 10f / 2f + spawnPadding,
                                            groundPlane.position.z + groundPlane.localScale.z * 10f / 2f - spawnPadding);
                spawnPosition = new Vector3(xPos, groundPlane.position.y + selectedEntry.spawnHeight, zPos);
                // Check if the spawn position is far enough from all tank spawn points
                validPosition = true;
                foreach (Transform tankSpawn in tankSpawnPoints)
                {
                    if (Vector3.Distance(spawnPosition, tankSpawn.position) < minDistanceFromTanks)
                    {
                        validPosition = false;
                        break;
                    }
                }
            }
            // Instantiate the obstacle at the valid spawn position
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject spawnedObstacle = Instantiate(selectedEntry.prefab, spawnPosition, randomRotation, obstaclesParent);
            spawnedObstacles.Add(spawnedObstacle); // Add the spawned obstacle to the list
        }
    }

    public void SpawnTanks(TankyAgent[] tanks)
    {
        for (int i = 0; i < tanks.Length; i++)
        {
            tanks[i].transform.position = tankSpawnPoints[i].position;
            tanks[i].transform.rotation = tankSpawnPoints[i].rotation;
            tanks[i].OnEpisodeBegin(); // Reset the tank's state for the new episode
        }

    }


    public void OnTankDestroyed(TankyAgent destroyedTank)
    {
        // Reward surviving tanks and penalize the destroyed tank
        foreach (TankyAgent agent in tanks)
        {
            if (agent == destroyedTank)
            {
                agent.AddReward(-1f); // Penalize the destroyed tank
            }
            else
            {
                agent.AddReward(1f); // Reward the surviving tank
            }
            agent.EndEpisode(); // End the episode for all tanks
        }

        // Clear obstacles
        ClearObstacles();

        // Reposition tanks at spawn points
        for (int i = 0; i < tanks.Length; i++)
        {
            tanks[i].transform.position = tankSpawnPoints[i].position;
            tanks[i].transform.rotation = tankSpawnPoints[i].rotation;
        }

        // Respawn obstacles
        SpawnObstacles();
    }
}
