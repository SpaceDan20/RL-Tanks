using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureTime = 10f;

    [Header("References")]
    public Slider captureSlider;
    public EnvironmentManager environmentManager;

    private HashSet<TankyAgent> tanksInZone = new HashSet<TankyAgent>();
    private TankyAgent capturingAgent;
    private float captureProgress = 0f;
    private bool captured = false;

    private void Awake()
    {
        if (environmentManager == null)
            environmentManager = GetComponentInParent<EnvironmentManager>();

        if (environmentManager == null)
            Debug.LogError($"CapturePoint '{name}' has no EnvironmentManager assigned and none found in parent hierarchy.", this);
    }

    private void Update()
    {
        if (captured) return;

        if (tanksInZone.Count == 1)
        {
            foreach (TankyAgent tank in tanksInZone)
                capturingAgent = tank;

            captureProgress += Time.deltaTime;
            capturingAgent.AddCaptureProgressReward(0.5f * Time.deltaTime / captureTime);
        }
        else if (tanksInZone.Count == 0 && captureProgress > 0f)
        {
            float decay = Mathf.Min(captureProgress, Time.deltaTime);
            captureProgress -= decay;
            if (capturingAgent != null)
                capturingAgent.AddCaptureProgressReward(-0.5f * decay / captureTime);
            if (captureProgress <= 0f)
                capturingAgent = null;
        }

        if (captureSlider != null)
            captureSlider.value = Mathf.Clamp01(captureProgress / captureTime);

        if (captureProgress >= captureTime)
            OnCaptured();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Tank")) return;

        TankyAgent agent = other.GetComponentInParent<TankyAgent>();
        if (agent != null)
            tanksInZone.Add(agent);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Tank")) return;

        TankyAgent agent = other.GetComponentInParent<TankyAgent>();
        if (agent == null) return;

        tanksInZone.Remove(agent);
    }

    private void OnCaptured()
    {
        Debug.Log($"Capture Point captured by {capturingAgent.name}!");
        captured = true;
        if (captureSlider != null) captureSlider.value = 1f;
        capturingAgent.AddReward(1.5f);
        environmentManager.OnCapturePointCaptured(capturingAgent);
    }

    public float CaptureProgress => captureProgress;

    public bool IsBeingCapturedBy(TankyAgent agent)
    {
        return tanksInZone.Count == 1 && capturingAgent == agent;
    }

    public bool IsBeingCapturedByEnemy(TankyAgent agent)
    {
        if (tanksInZone.Count != 1) return false;
        return capturingAgent != null && capturingAgent != agent;
    }

    public void ResetCapture()
    {
        tanksInZone.Clear();
        capturingAgent = null;
        captureProgress = 0f;
        captured = false;
        if (captureSlider != null) captureSlider.value = 0f;
    }
}
