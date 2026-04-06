using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

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
    public TankyAgent[] teamATanks;
    public TankyAgent[] teamBTanks;

    [Header("Capture Point")]
    public CapturePoint capturePoint;

    [Header("Spawn Points")]
    public float spawnAngles = 90f; // Angle range for spawning tanks (e.g., 45 degrees means tanks can spawn within a 90-degree arc)
    public Transform[] teamACloseSpawnPoints;
    public Transform[] teamBCloseSpawnPoints;
    public Transform[] teamAMidSpawnPoints;
    public Transform[] teamBMidSpawnPoints;
    public Transform[] teamAFarSpawnPoints;
    public Transform[] teamBFarSpawnPoints;
    private Transform[] teamASpawnPoints; // Current spawn points for team A based on difficulty
    private Transform[] teamBSpawnPoints; // Current spawn points for team B based on difficulty

    private List<GameObject> spawnedObstacles = new List<GameObject>(); // List to keep track of spawned obstacles
    private bool episodeResetHandled = false; // Flag to ensure episode reset is handled only once

    public void ResetEpisode()
    {
        if (episodeResetHandled)
        {
            episodeResetHandled = false;
            return; // Skip reset if it has already been handled
        }
        episodeResetHandled = true; // Set the flag to indicate reset is being handled
        ClearObstacles(); // Clear existing obstacles
        SpawnTanks(); // Spawn tanks at their respective spawn points
        SpawnObstacles(); // Spawn new obstacles for the episode
        if (capturePoint != null) capturePoint.ResetCapture();
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
                foreach (Transform spawnPoint in teamASpawnPoints)
                {
                    if (Vector3.Distance(spawnPosition, spawnPoint.position) < minDistanceFromTanks)
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (validPosition)
                {
                    foreach (Transform spawnPoint in teamBSpawnPoints)
                    {
                        if (Vector3.Distance(spawnPosition, spawnPoint.position) < minDistanceFromTanks)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                }
            }

            // Instantiate the obstacle at the valid spawn position
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject spawnedObstacle = Instantiate(selectedEntry.prefab, spawnPosition, randomRotation, obstaclesParent);
            spawnedObstacles.Add(spawnedObstacle); // Add the spawned obstacle to the list
        }
    }

    private Transform[] GetSpawnPoints(Transform[] close, Transform[] mid, Transform[] far)
    {
        // Get the current difficulty level from the environment parameters
        float difficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("spawn_difficulty", 0f);

        if (difficulty < 0.5f) return close;
        if (difficulty < 1.5f) return mid;
        return far;
    }

    public List<TankyAgent> GetAllEnemies(TankyAgent requestingAgent)
    {
        List<TankyAgent> enemies = new List<TankyAgent>();

        // Check if requesting agent is on team A
        foreach (TankyAgent agent in teamATanks)
        {
            if (agent == requestingAgent)
            {
                // Requesting agent is team A, so enemies are team B
                foreach (TankyAgent enemy in teamBTanks)
                    enemies.Add(enemy);
                return enemies;
            }
        }

        // Requesting agent must be team B, so enemies are team A
        foreach (TankyAgent enemy in teamATanks)
            enemies.Add(enemy);

        return enemies;
    }

    public void SpawnTanks()
    {
        teamASpawnPoints = GetSpawnPoints(teamACloseSpawnPoints, teamAMidSpawnPoints, teamAFarSpawnPoints);
        teamBSpawnPoints = GetSpawnPoints(teamBCloseSpawnPoints, teamBMidSpawnPoints, teamBFarSpawnPoints);

        // Shuffle team A spawn points
        List<int> teamAIndices = new List<int>();
        for (int i = 0; i < teamASpawnPoints.Length; i++)
            teamAIndices.Add(i);

        for (int i = teamAIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = teamAIndices[i];
            teamAIndices[i] = teamAIndices[randomIndex];
            teamAIndices[randomIndex] = temp;
        }

        // Shuffle team B spawn points
        List<int> teamBIndices = new List<int>();
        for (int i = 0; i < teamBSpawnPoints.Length; i++)
            teamBIndices.Add(i);

        for (int i = teamBIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = teamBIndices[i];
            teamBIndices[i] = teamBIndices[randomIndex];
            teamBIndices[randomIndex] = temp;
        }

        // Assign team A tanks to shuffled spawn points
        for (int i = 0; i < teamATanks.Length; i++)
        {
            float teamAAngle = Random.Range(180 - (spawnAngles / 2), 180 + (spawnAngles / 2)); // from 180 degrees
            teamATanks[i].transform.position = teamASpawnPoints[teamAIndices[i]].position;
            teamATanks[i].transform.rotation = Quaternion.Euler(0, teamAAngle, 0);
        }

        // Assign team B tanks to shuffled spawn points
        for (int i = 0; i < teamBTanks.Length; i++)
        {
            float teamBAngle = Random.Range(-spawnAngles / 2, spawnAngles / 2); // from 0 degrees
            teamBTanks[i].transform.position = teamBSpawnPoints[teamBIndices[i]].position;
            teamBTanks[i].transform.rotation = Quaternion.Euler(0, teamBAngle, 0);
        }
    }

    public void OnCapturePointCaptured(TankyAgent capturingAgent)
    {
        if (capturePoint != null)
            capturePoint.ResetCapture();

        foreach (TankyAgent enemy in GetAllEnemies(capturingAgent))
            enemy.AddReward(-1f);

        foreach (TankyAgent agent in teamATanks)
            agent.EndEpisode();

        foreach (TankyAgent agent in teamBTanks)
            agent.EndEpisode();

        ResetEpisode();
    }

    public void OnTankDestroyed(TankyAgent destroyedTank)
    {
        Debug.Log("Oooh! Son got blowed up!");

        foreach (TankyAgent agent in teamATanks)
        {
            agent.AddReward(agent == destroyedTank ? -0.5f : 0.5f);
            agent.EndEpisode();
        }

        foreach (TankyAgent agent in teamBTanks)
        {
            agent.AddReward(agent == destroyedTank ? -0.5f : 0.5f);
            agent.EndEpisode();
        }
        ResetEpisode();
    }
}
