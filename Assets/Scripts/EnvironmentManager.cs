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
    public Transform[] teamALevel1SpawnPoints; // 2 entries: index 0 = 7m (1a), 1 = 12m (1b)
    public Transform[] teamBLevel1SpawnPoints; // 2 entries: index 0 = 7m (1a), 1 = 12m (1b)
    public CapturePoint level1AreaACapturePoint;
    public CapturePoint level1AreaBCapturePoint;

    [Header("Level 2")]
    public Transform[] teamALevel2SpawnPoints; // 1 entry
    public Transform[] teamBLevel2SpawnPoints; // 1 entry
    public CapturePoint level2AreaACapturePoint;
    public CapturePoint level2AreaBCapturePoint;

    [Header("Level 3")]
    public Transform[] teamALevel3SpawnPoints; // 4 entries
    public Transform[] teamBLevel3SpawnPoints; // 4 entries
    public CapturePoint level3AreaACapturePoint;
    public CapturePoint level3AreaBCapturePoint;

    private bool episodeResetHandled = false;

    // Pre-computed spawn assignments — applied per-agent in SpawnSelf()
    private Dictionary<TankyAgent, Vector3> pendingPositions = new Dictionary<TankyAgent, Vector3>();
    private Dictionary<TankyAgent, Quaternion> pendingRotations = new Dictionary<TankyAgent, Quaternion>();
    private Dictionary<TankyAgent, CapturePoint> pendingCapturePoints = new Dictionary<TankyAgent, CapturePoint>();

    // -------------------------------------------------------------------------
    // Stage helpers
    // -------------------------------------------------------------------------

    private float Stage =>
        Academy.Instance.EnvironmentParameters.GetWithDefault("stages", 0f);

    // Level 1 stages are 0.0 and 1.0; level 2 is 2.0; level 3 is 3.0; main battlefield is 4.0
    private bool IsLevel1Stage => Stage < 1.5f;
    private bool IsLevel2Stage => Stage >= 1.5f && Stage < 2.5f;
    private bool IsLevel3Stage => Stage >= 2.5f && Stage < 3.5f;

    // Combat is enabled only on the main battlefield (stage 4.0)
    public bool CombatEnabled => Stage >= 3.5f;

    // Returns 0 for level_1a (7m spawns) or 1 for level_1b (12m spawns)
    private int Level1SpawnIndex() => Stage < 0.5f ? 0 : 1;

    // Half-arc (degrees) for front-facing spawn orientation per stage
    private float SpawnArcHalfAngle()
    {
        float stage = Stage;
        if (stage < 0.5f) return 15f;  // level_1a: 30° arc
        if (stage < 1.5f) return 30f;  // level_1b: 60° arc
        if (stage < 2.5f) return 30f;  // level_2:  60° arc
        if (stage < 3.5f) return 45f;  // level_3:  90° arc
        return spawnAngles / 2f;       // main battle: inspector value
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
        else if (IsLevel2Stage)
        {
            if (level2AreaACapturePoint != null) level2AreaACapturePoint.ResetCapture();
            if (level2AreaBCapturePoint != null) level2AreaBCapturePoint.ResetCapture();
        }
        else if (IsLevel3Stage)
        {
            if (level3AreaACapturePoint != null) level3AreaACapturePoint.ResetCapture();
            if (level3AreaBCapturePoint != null) level3AreaBCapturePoint.ResetCapture();
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
        else if (IsLevel2Stage)
            ComputeLevel2Assignments();
        else if (IsLevel3Stage)
            ComputeLevel3Assignments();
        else
            ComputeMainAssignments();
    }

    private void ComputeLevel1Assignments()
    {
        int index = Level1SpawnIndex();
        float halfArc = SpawnArcHalfAngle();

        foreach (TankyAgent tank in teamATanks)
        {
            Transform sp = teamALevel1SpawnPoints[index];
            pendingPositions[tank] = sp.position;
            pendingRotations[tank] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[tank] = level1AreaACapturePoint;
        }

        foreach (TankyAgent tank in teamBTanks)
        {
            Transform sp = teamBLevel1SpawnPoints[index];
            pendingPositions[tank] = sp.position;
            pendingRotations[tank] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[tank] = level1AreaBCapturePoint;
        }
    }

    private void ComputeLevel2Assignments()
    {
        float halfArc = SpawnArcHalfAngle();

        foreach (TankyAgent tank in teamATanks)
        {
            Transform sp = teamALevel2SpawnPoints[0];
            pendingPositions[tank] = sp.position;
            pendingRotations[tank] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[tank] = level2AreaACapturePoint;
        }

        foreach (TankyAgent tank in teamBTanks)
        {
            Transform sp = teamBLevel2SpawnPoints[0];
            pendingPositions[tank] = sp.position;
            pendingRotations[tank] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[tank] = level2AreaBCapturePoint;
        }
    }

    private void ComputeLevel3Assignments()
    {
        float halfArc = SpawnArcHalfAngle();

        List<int> teamAIndices = ShuffledIndices(teamALevel3SpawnPoints.Length);
        List<int> teamBIndices = ShuffledIndices(teamBLevel3SpawnPoints.Length);

        for (int i = 0; i < teamATanks.Length; i++)
        {
            Transform sp = teamALevel3SpawnPoints[teamAIndices[i % teamAIndices.Count]];
            pendingPositions[teamATanks[i]] = sp.position;
            pendingRotations[teamATanks[i]] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[teamATanks[i]] = level3AreaACapturePoint;
        }

        for (int i = 0; i < teamBTanks.Length; i++)
        {
            Transform sp = teamBLevel3SpawnPoints[teamBIndices[i % teamBIndices.Count]];
            pendingPositions[teamBTanks[i]] = sp.position;
            pendingRotations[teamBTanks[i]] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[teamBTanks[i]] = level3AreaBCapturePoint;
        }
    }

    private void ComputeMainAssignments()
    {
        float halfArc = SpawnArcHalfAngle();
        List<int> teamAIndices = ShuffledIndices(teamABattleSpawnPoints.Length);
        List<int> teamBIndices = ShuffledIndices(teamBBattleSpawnPoints.Length);

        for (int i = 0; i < teamATanks.Length; i++)
        {
            Transform sp = teamABattleSpawnPoints[teamAIndices[i]];
            pendingPositions[teamATanks[i]] = sp.position;
            pendingRotations[teamATanks[i]] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[teamATanks[i]] = capturePoint;
        }

        for (int i = 0; i < teamBTanks.Length; i++)
        {
            Transform sp = teamBBattleSpawnPoints[teamBIndices[i]];
            pendingPositions[teamBTanks[i]] = sp.position;
            pendingRotations[teamBTanks[i]] = Quaternion.Euler(0, sp.eulerAngles.y + Random.Range(-halfArc, halfArc), 0);
            pendingCapturePoints[teamBTanks[i]] = capturePoint;
        }
    }

    private List<int> ShuffledIndices(int count)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < count; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[r]; indices[r] = tmp;
        }
        return indices;
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

        if (CombatEnabled)
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
