using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Tanks")]
    public TankyAgent[] teamATanks;
    public TankyAgent[] teamBTanks;

    [Header("Main Capture Point")]
    public CapturePoint capturePoint;

    [Header("Main Spawn Points")]
    public float spawnAngles = 30f;
    public Transform[] teamABattleSpawnPoints;
    public Transform[] teamBBattleSpawnPoints;

    [Header("Level 1 Areas")]
    public Transform[] teamALevel1SpawnPoints; // 3 entries: index 0 = 1a, 1 = 1b, 2 = 1c
    public Transform[] teamBLevel1SpawnPoints; // 3 entries: index 0 = 1a, 1 = 1b, 2 = 1c
    public CapturePoint level1AreaACapturePoint;
    public CapturePoint level1AreaBCapturePoint;

    private bool episodeResetHandled = false;

    // Pre-computed spawn assignments — applied per-agent in SpawnSelf()
    private Dictionary<TankyAgent, Vector3> pendingPositions = new Dictionary<TankyAgent, Vector3>();
    private Dictionary<TankyAgent, Quaternion> pendingRotations = new Dictionary<TankyAgent, Quaternion>();
    private Dictionary<TankyAgent, CapturePoint> pendingCapturePoints = new Dictionary<TankyAgent, CapturePoint>();

    // -------------------------------------------------------------------------
    // Stage helpers
    // -------------------------------------------------------------------------

    private float Stage =>
        Academy.Instance.EnvironmentParameters.GetWithDefault("stages", 3f);

    // Level 1 stages are 0.0, 1.0, 2.0; main battlefield is 3.0
    private bool IsLevel1Stage => Stage < 2.5f;

    // Combat is enabled only on the main battlefield (stage 3.0)
    public bool CombatEnabled => Stage >= 2.5f;

    // Returns 0 / 1 / 2 for level_1a (0.0) / level_1b (1.0) / level_1c (2.0)
    private int Level1SpawnIndex()
    {
        float stage = Stage;
        if (stage < 0.5f) return 0;
        if (stage < 1.5f) return 1;
        return 2;
    }

    // -------------------------------------------------------------------------
    // Episode reset
    // -------------------------------------------------------------------------

    // Pre-computes spawn assignments and resets capture points.
    // Each agent applies its own spawn via SpawnSelf() in OnEpisodeBegin().
    public void ResetEpisode()
    {
        if (episodeResetHandled)
        {
            episodeResetHandled = false;
            return;
        }
        episodeResetHandled = true;

        ComputeSpawnAssignments();
        ResetCapturePoints();
    }

    // Applies the pre-computed spawn for a single agent only.
    public void SpawnSelf(TankyAgent agent)
    {
        if (pendingPositions.TryGetValue(agent, out Vector3 pos))
            agent.transform.position = pos;
        if (pendingRotations.TryGetValue(agent, out Quaternion rot))
            agent.transform.rotation = rot;
        if (pendingCapturePoints.TryGetValue(agent, out CapturePoint cp))
            agent.capturePoint = cp;
    }

    private void ResetCapturePoints()
    {
        if (IsLevel1Stage)
        {
            if (level1AreaACapturePoint != null) level1AreaACapturePoint.ResetCapture();
            if (level1AreaBCapturePoint != null) level1AreaBCapturePoint.ResetCapture();
        }
        else
        {
            if (capturePoint != null) capturePoint.ResetCapture();
        }
    }

    // -------------------------------------------------------------------------
    // Spawn assignment computation (positions only — not applied to transforms)
    // -------------------------------------------------------------------------

    private void ComputeSpawnAssignments()
    {
        if (IsLevel1Stage)
            ComputeLevel1Assignments();
        else
            ComputeMainAssignments();
    }

    private void ComputeLevel1Assignments()
    {
        int index = Level1SpawnIndex();

        foreach (TankyAgent tank in teamATanks)
        {
            pendingPositions[tank] = teamALevel1SpawnPoints[index].position;
            pendingRotations[tank] = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            pendingCapturePoints[tank] = level1AreaACapturePoint;
        }

        foreach (TankyAgent tank in teamBTanks)
        {
            pendingPositions[tank] = teamBLevel1SpawnPoints[index].position;
            pendingRotations[tank] = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            pendingCapturePoints[tank] = level1AreaBCapturePoint;
        }
    }

    private void ComputeMainAssignments()
    {
        // Shuffle team A indices
        List<int> teamAIndices = new List<int>();
        for (int i = 0; i < teamABattleSpawnPoints.Length; i++) teamAIndices.Add(i);
        for (int i = teamAIndices.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = teamAIndices[i]; teamAIndices[i] = teamAIndices[r]; teamAIndices[r] = tmp;
        }

        // Shuffle team B indices
        List<int> teamBIndices = new List<int>();
        for (int i = 0; i < teamBBattleSpawnPoints.Length; i++) teamBIndices.Add(i);
        for (int i = teamBIndices.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = teamBIndices[i]; teamBIndices[i] = teamBIndices[r]; teamBIndices[r] = tmp;
        }

        for (int i = 0; i < teamATanks.Length; i++)
        {
            float angle = Random.Range(180 - spawnAngles / 2, 180 + spawnAngles / 2);
            pendingPositions[teamATanks[i]] = teamABattleSpawnPoints[teamAIndices[i]].position;
            pendingRotations[teamATanks[i]] = Quaternion.Euler(0, angle, 0);
            pendingCapturePoints[teamATanks[i]] = capturePoint;
        }

        for (int i = 0; i < teamBTanks.Length; i++)
        {
            float angle = Random.Range(-spawnAngles / 2, spawnAngles / 2);
            pendingPositions[teamBTanks[i]] = teamBBattleSpawnPoints[teamBIndices[i]].position;
            pendingRotations[teamBTanks[i]] = Quaternion.Euler(0, angle, 0);
            pendingCapturePoints[teamBTanks[i]] = capturePoint;
        }
    }

    // -------------------------------------------------------------------------
    // Team queries
    // -------------------------------------------------------------------------

    public List<TankyAgent> GetAllEnemies(TankyAgent requestingAgent)
    {
        List<TankyAgent> enemies = new List<TankyAgent>();

        foreach (TankyAgent agent in teamATanks)
        {
            if (agent == requestingAgent)
            {
                foreach (TankyAgent enemy in teamBTanks) enemies.Add(enemy);
                return enemies;
            }
        }

        foreach (TankyAgent enemy in teamATanks) enemies.Add(enemy);
        return enemies;
    }

    // -------------------------------------------------------------------------
    // Episode events
    // -------------------------------------------------------------------------

    public void OnCapturePointCaptured(TankyAgent capturingAgent)
    {
        ResetCapturePoints();

        if (!IsLevel1Stage)
        {
            foreach (TankyAgent enemy in GetAllEnemies(capturingAgent))
                enemy.AddReward(-1.5f);
        }

        foreach (TankyAgent agent in teamATanks) { agent.LogEpisodeStats(); agent.EndEpisode(); }
        foreach (TankyAgent agent in teamBTanks) { agent.LogEpisodeStats(); agent.EndEpisode(); }
    }

    public void OnTankDestroyed(TankyAgent destroyedTank)
    {
        Debug.Log("Oooh! Son got blowed up!");

        foreach (TankyAgent agent in teamATanks)
        {
            agent.AddReward(agent == destroyedTank ? -1f : 1f);
            agent.LogEpisodeStats();
            agent.EndEpisode();
        }
        foreach (TankyAgent agent in teamBTanks)
        {
            agent.AddReward(agent == destroyedTank ? -1f : 1f);
            agent.LogEpisodeStats();
            agent.EndEpisode();
        }
    }
}
