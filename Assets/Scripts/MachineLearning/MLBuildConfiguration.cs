using UnityEngine;

[CreateAssetMenu(fileName = "MLBuildConfiguration", menuName = "AI/ML Build Configuration")]
public class MLBuildConfiguration : ScriptableObject
{
    [SerializeField]
    private bool enableLearning = true;

    [SerializeField]
    private bool enableDataCollection = true;

    [SerializeField]
    private bool enableDetailedScoring = true;

    [SerializeField]
    private bool enableMLDebugLogs = false;

    [SerializeField]
    private bool enableFileIO = true;

    public bool IsLearningEnabled => enableLearning;
    public bool IsDataCollectionEnabled => enableDataCollection;
    public bool IsDetailedScoringEnabled => enableDetailedScoring;
    public bool IsMLDebugLogsEnabled => enableMLDebugLogs;
    public bool IsFileIOEnabled => enableFileIO;

    [ContextMenu("Configure for Production Build")]
    public void ConfigureForProduction()
    {
        enableLearning = false;
        enableDataCollection = false;
        enableDetailedScoring = false;
        enableMLDebugLogs = false;
        enableFileIO = false;

        Debug.Log("[MLBuildConfiguration] Configured for PRODUCTION build - All ML features disabled for performance.");
    }

    [ContextMenu("Configure for Development Build")]
    public void ConfigureForDevelopment()
    {
        enableLearning = true;
        enableDataCollection = true;
        enableDetailedScoring = true;
        enableMLDebugLogs = true;
        enableFileIO = true;

        Debug.Log("[MLBuildConfiguration] Configured for DEVELOPMENT build - All ML features enabled.");
    }

    [ContextMenu("Configure for Testing Build")]
    public void ConfigureForTesting()
    {
        enableLearning = false;
        enableDataCollection = false;
        enableDetailedScoring = true;
        enableMLDebugLogs = false;
        enableFileIO = false;

        Debug.Log("[MLBuildConfiguration] Configured for TESTING build - Core features enabled, expensive features disabled.");
    }
}
