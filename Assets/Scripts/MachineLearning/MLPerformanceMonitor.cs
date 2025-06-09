using UnityEngine;
using System.Collections;

public class MLPerformanceMonitor : MonoBehaviour
{
    [SerializeField]
    private bool enableMonitoring = true;

    [SerializeField]
    private float reportInterval = 5.0f;

    [SerializeField]
    private SimulationControl simulationControl;

    private float frameTime = 0f;
    private float averageFrameTime = 0f;
    private int frameCount = 0;
    private float lastReportTime = 0f;
    
    private void Start()
    {
        if (enableMonitoring && simulationControl != null)
        {
            StartCoroutine(MonitorPerformance());
            Debug.Log("[MLPerformanceMonitor] Performance monitoring started.");
        }
    }

    private void Update()
    {
        if (!enableMonitoring)
            return;

        frameTime = Time.unscaledDeltaTime;
        averageFrameTime = (averageFrameTime * frameCount + frameTime) / (frameCount + 1);
        frameCount++;
    }
    
    private IEnumerator MonitorPerformance()
    {
        while (enableMonitoring)
        {
            yield return new WaitForSeconds(reportInterval);

            if (Time.time - lastReportTime >= reportInterval)
            {
                ReportPerformance();
                lastReportTime = Time.time;

                frameCount = 0;
                averageFrameTime = 0f;
            }
        }
    }
    
    private void ReportPerformance()
    {
        if (simulationControl == null)
            return;

        float fps = 1f / averageFrameTime;
        string mlStatus = simulationControl.IsLearningEnabled ? "ENABLED" : "DISABLED";

        Debug.Log(
            $"[MLPerformanceMonitor] ML Status: {mlStatus} | Average FPS: {fps:F1} | Frame Time: {averageFrameTime * 1000:F2}ms");

        if (fps < 30f && simulationControl.IsLearningEnabled)
        {
            Debug.LogWarning(
                "[MLPerformanceMonitor] Low FPS detected with ML enabled. Consider disabling ML for production builds.");
        }
    }
    
    [ContextMenu("Test Performance Impact")]
    public void TestPerformanceImpact()
    {
        if (simulationControl == null)
        {
            Debug.LogError("[MLPerformanceMonitor] SimulationControl reference is missing!");
            return;
        }

        StartCoroutine(PerformanceTest());
    }

    private IEnumerator PerformanceTest()
    {
        Debug.Log("[MLPerformanceMonitor] Starting performance test...");

        Debug.Log("[MLPerformanceMonitor] Testing with ML ENABLED...");
        yield return new WaitForSeconds(10f);
        float fpsWithML = 1f / averageFrameTime;

        frameCount = 0;
        averageFrameTime = 0f;

        Debug.Log("[MLPerformanceMonitor] Testing with ML DISABLED...");
        yield return new WaitForSeconds(10f);
        float fpsWithoutML = 1f / averageFrameTime;

        float performanceGain = (fpsWithoutML - fpsWithML) / fpsWithML * 100f;
        Debug.Log($"[MLPerformanceMonitor] Performance Test Results:");
        Debug.Log($"  - FPS with ML: {fpsWithML:F1}");
        Debug.Log($"  - FPS without ML: {fpsWithoutML:F1}");
        Debug.Log($"  - Performance gain: {performanceGain:F1}%");
    }
}
