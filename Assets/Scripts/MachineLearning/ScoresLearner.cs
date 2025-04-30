using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class ScoresLearner : MonoBehaviour
{
    [SerializeField]
    public SimulationControl simulationControl;
    public ScoresDatabase Scores { get; private set; }

    [SerializeField]
    private string pathName;

    [SerializeField]
    [Range(0.01f, 1f)]
    float learningRate = 0.1f;
    public float LearningRate
    {
        get => learningRate;
        private set => learningRate = value;
    }
    private string defaultFilePath = Application.dataPath + "/StreamingAssets/DataFiles/";

    [SerializeField]
    EnemyAI Player;

    [SerializeField]
    Transform Enemy;

    [Header("Learning Parameters")]
    [SerializeField]
    [Range(0.01f, 1f)]
    float distanceImportance = 1;
    public float DistanceImportance => distanceImportance;

    [SerializeField]
    [Range(0.01f, 1f)]
    float expositionImportance = 1;
    public float ExpositionImportance => expositionImportance;

    [SerializeField]
    [Range(0.01f, 1f)]
    float reactionImportance = 1;
    public float ReactionImportance => reactionImportance;

    private HidePoint lastUpdatedSpot;

    private void Start()
    {
        Scores = simulationControl.HeuristicDatabase;
        StartCoroutine(UpdateData());
    }

    private void OnApplicationQuit()
    {
        SaveDataInDevice();
    }

    private void SaveDataInDevice()
    {
        if (!FileSystem.FolderExists(defaultFilePath))
            FileSystem.CreateFolder(defaultFilePath);

        string filePath = defaultFilePath + pathName + ".txt";

        if (!FileSystem.FileExists(filePath))
        {
            FileSystem.CreateFile(filePath, WriteData(Scores));
        }
        else
        {
            FileSystem.RemoveFile(filePath);
            FileSystem.CreateFile(filePath, WriteData(Scores));
        }
    }

    private string WriteData(ScoresDatabase data)
    {
        HashSet<HidePoint> uniquePositions = new HashSet<HidePoint>();
        string newData = "";

        foreach (KeyValuePair<HidePoint, float> point in data.ToDictionary)
        {
            if (!uniquePositions.Contains(point.Key))
            {
                newData += $"{point.Key.Position}|{point.Value}\n";
                uniquePositions.Add(point.Key);
            }
            else
            {
                Debug.LogWarning($"Posição duplicada detectada ao salvar: {point.Key}");
            }
        }

        return newData;
    }

    private Dictionary<HidePoint, float> ReadData(string data)
    {
        string[] dataLines = data.Split('\n');
        Debug.Log("File has " + dataLines.Length + " linhas.");

        Dictionary<HidePoint, float> newData = new Dictionary<HidePoint, float>();

        foreach (string line in dataLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] lineData = line.Split('|');
            Vector3 position = StringToVector3(lineData[0]);
            HidePoint point = new HidePoint(
                position,
                simulationControl.CurrentConfig,
                Player,
                simulationControl.ScoresDatabase,
                simulationControl
            );
            float score = float.Parse(lineData[1], CultureInfo.InvariantCulture);

            if (!newData.ContainsKey(point))
            {
                newData.Add(point, score);
            }
            else
            {
                Debug.LogWarning($"Posição duplicada ignorada ao carregar: {position}");
            }
        }

        return newData;
    }

    private static Vector3 StringToVector3(string value)
    {
        value = value.Trim('(', ')'); // Remove parênteses
        string[] split = value.Split(',');

        if (split.Length != 3)
            throw new System.FormatException("Formato inválido para Vector3");

        return RoundVector3(
            new Vector3(
                float.Parse(split[0], CultureInfo.InvariantCulture),
                float.Parse(split[1], CultureInfo.InvariantCulture),
                float.Parse(split[2], CultureInfo.InvariantCulture)
            )
        );
    }

    public ScoresDatabase LoadData()
    {
        if (
            FileSystem.FolderExists(defaultFilePath)
            && FileSystem.FileExists(defaultFilePath + pathName + ".txt")
        )
            return new ScoresDatabase(
                ReadData(File.ReadAllText(defaultFilePath + pathName + ".txt"))
            );

        return new ScoresDatabase();
    }

    public void SetScore(HidePoint hidingSpot, float score)
    {
        if (hidingSpot == null)
        {
            Debug.LogError("Invalid input hide point");
            return;
        }
        if (Scores.HasPoint(hidingSpot))
        {
            Scores.Scores[hidingSpot] = Mathf.Lerp(
                Scores.Scores[hidingSpot],
                score,
                Mathf.Clamp01(learningRate)
            );
        }
        else
        {
            Scores.Scores.Add(hidingSpot, score);
        }
    }

    void OnDrawGizmos()
    {
        if (Scores == null || Scores.Scores.Count <= 0)
            return;

        foreach (KeyValuePair<HidePoint, float> spot in Scores.normalized.ToDictionary)
        {
            float value = spot.Value;
            Gizmos.color = new Color(1f - value, value, 0f);
            if (lastUpdatedSpot != spot.Key)
                Gizmos.DrawLine(spot.Key.Position, spot.Key.Position + Vector3.up * 50f);
            else
                Gizmos.DrawLine(spot.Key.Position, spot.Key.Position + Vector3.up * 80f);
        }
    }

    public IEnumerator UpdateData()
    {
        WaitForSeconds wait = new WaitForSeconds(Player.UpdateFrequency);
        // Debug.LogWarning("Update frequency set to: " + Player.UpdateFrequency);

        float tmp = Player.UpdateFrequency;
        while (true)
        {
            yield return wait;
            if (tmp != Player.UpdateFrequency)
            {
                // Debug.LogWarning("Update frequency changed to: " + Player.UpdateFrequency);
                wait = new WaitForSeconds(Player.UpdateFrequency);
                tmp = Player.UpdateFrequency;
            }

            HidePoint closestSpot = GetClosestPoint(Player.transform.position);
            lastUpdatedSpot = closestSpot;

            if (closestSpot == null)
            {
                Debug.LogError("Close point not found");
                continue;
            }

            closestSpot.OnInteract();
        }
    }

    public HidePoint GetClosestPoint(Vector3 target)
    {
        if (
            simulationControl.ScoresDatabase == null
            || simulationControl.ScoresDatabase.Scores.Count == 0
        )
        {
            Debug.LogError("No scores found");
            return null;
        }

        HidePoint closest = simulationControl.ScoresDatabase.Scores.First().Key;
        float minDistance = float.MaxValue;

        foreach (HidePoint point in simulationControl.ScoresDatabase.Scores.Keys)
        {
            float distSq = Vector3.Distance(target, point.Position);
            if (distSq < minDistance)
            {
                minDistance = distSq;
                closest = point;
            }
        }

        return closest;
    }

    public static Vector3 RoundVector3(Vector3 v, int decimalPlaces = 4)
    {
        float multiplier = Mathf.Pow(10, decimalPlaces);
        return new Vector3(
            Mathf.Round(v.x * multiplier) / multiplier,
            Mathf.Round(v.y * multiplier) / multiplier,
            Mathf.Round(v.z * multiplier) / multiplier
        );
    }
}
