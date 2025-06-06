using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class ScoresFileSystem : MonoBehaviour
{
    public string FilePath => defaultFilePath + "/" + fileName + ".txt";

    [SerializeField]
    private string fileName;
    private string defaultFilePath = Application.dataPath + "/StreamingAssets/DataFiles/";

    [SerializeField]
    private SimulationControl simulationControl;

    [SerializeField]
    private ScoresLearner scoresLearner;

    private static readonly HashSet<string> usedFilePaths = new HashSet<string>();

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogError("ScoresFileSystem: O nome do ficheiro não está especificado. O componente será desativado.", this);
            enabled = false;
            return;
        }

        string fullPath = Path.GetFullPath(FilePath);
        if (usedFilePaths.Contains(fullPath))
        {
            Debug.LogError($"ScoresFileSystem: Detetado caminho de ficheiro duplicado: {fullPath}. Por favor, use um nome de ficheiro único para cada sala do SimulationControl. O componente será desativado.", this);
            enabled = false;
            return;
        }
        usedFilePaths.Add(fullPath);
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            string fullPath = Path.GetFullPath(FilePath);
            usedFilePaths.Remove(fullPath);
        }
    }

    private void OnApplicationQuit() => SaveDataInDevice();

    private void SaveDataInDevice()
    {
        if (!FileSystem.FolderExists(defaultFilePath))
            FileSystem.CreateFolder(defaultFilePath);

        string filePath = FilePath;
        string data = WriteData(simulationControl.HeuristicDatabase);

        if (FileSystem.FileExists(filePath))
            FileSystem.RemoveFile(filePath);

        FileSystem.CreateFile(filePath, data);
    }

    private string WriteData(ScoresDatabase data)
    {
        HashSet<Vector3> uniquePositions = new HashSet<Vector3>();
        string newData = "";

        foreach (var point in data.ToDictionary)
        {
            Vector3 roundedPosition = PublicMethods.RoundVector3(point.Key.Position);

            if (!uniquePositions.Contains(roundedPosition))
            {
                uniquePositions.Add(roundedPosition);
                newData +=
                    $"{PublicMethods.Vector3ToString(roundedPosition)}|{point.Value.ToString(CultureInfo.InvariantCulture)}|{point.Key.Report.InteractionsNumber}\n";
            }
            else
                Debug.LogWarning($"Posição duplicada detectada ao salvar: {point.Key.Position}");
        }

        return newData;
    }

    private Dictionary<HidePoint, float> ReadData(string data)
    {
        string[] dataLines = data.Split('\n');
        Dictionary<HidePoint, float> newData = new Dictionary<HidePoint, float>();

        foreach (string line in dataLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] lineData = line.Split('|');
            Vector3 position = PublicMethods.RoundVector3(
                PublicMethods.StringToVector3(lineData[0])
            );
            float score = float.Parse(lineData[1], CultureInfo.InvariantCulture);
            int interactions = int.Parse(lineData[2], CultureInfo.InvariantCulture);

            HidePoint point = new HidePoint(
                position,
                simulationControl.CurrentConfig,
                simulationControl.TrainingEntity,
                simulationControl
            );

            point.Report.SetInteractionsNumber(interactions);

            if (!newData.ContainsKey(point))
                newData.Add(point, score);
            else
                Debug.LogWarning($"Posição duplicada ignorada ao carregar: {position}");
        }

        return newData;
    }

    public ScoresDatabase LoadData()
    {
        ScoresDatabase database;

        if (FileSystem.FolderExists(defaultFilePath) && FileSystem.FileExists(FilePath))
            database = new ScoresDatabase(ReadData(File.ReadAllText(FilePath)));
        else
            return new ScoresDatabase(); // Return empty database

        Debug.LogWarning(
            $"File loaded from: {FilePath} with {database.ToDictionary.Count} points."
        );

        return database;
    }
}
