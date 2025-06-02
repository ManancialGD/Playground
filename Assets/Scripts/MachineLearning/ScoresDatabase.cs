using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoresDatabase
{
    public Dictionary<HidePoint, float> Scores { get; private set; }

    public ScoresDatabase(Dictionary<HidePoint, float> scores)
    {
        Scores = new Dictionary<HidePoint, float>(scores);
    }

    public ScoresDatabase()
    {
        Scores = new Dictionary<HidePoint, float>();
    }

    public bool HasPoint(HidePoint point) => point != null && Scores.ContainsKey(point);

    public bool HasPosition(Vector3 position, out HidePoint hidePointFound)
    {
        Vector3 inputRoundedPosition = PublicMethods.RoundVector3(position);
        hidePointFound = Scores.Keys.FirstOrDefault(hp =>
            PublicMethods.RoundVector3(hp.Position) == inputRoundedPosition
        );

        if (hidePointFound != null)
            return true;
        else
        {
            Debug.LogWarning($"HidePoint with position {position} not found in ScoresDatabase.");
            return false;
        }
    }

    public HidePoint CurrentBestPoint => UpdateDatabaseEvent();

    public ScoresDatabase normalized
    {
        get
        {
            if (Scores.Count == 0)
                return new ScoresDatabase();

            float min = Scores.Values.Min();
            float max = Scores.Values.Max();

            if (max == min) // Se todos os valores forem iguais, normaliza para 0
                return new ScoresDatabase(Scores.ToDictionary(kvp => kvp.Key, kvp => 0f));

            return new ScoresDatabase(
                Scores.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value - min) / (max - min))
            );
        }
    }

    public Dictionary<HidePoint, float> ToDictionary
    {
        get
        {
            return Scores
                .OrderByDescending(x => x.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public void SetScore(HidePoint hidingSpot, float score = 0)
    {
        if (HasPoint(hidingSpot))
        {
            if (score == 0)
                return;
            Scores[hidingSpot] = score;
        }
        else
            Scores.Add(hidingSpot, score);
    }

    public void RemovePoint(HidePoint hidePoint)
    {
        if (Scores.ContainsKey(hidePoint))
            Scores.Remove(hidePoint);
        else
            Debug.LogWarning($"HidePoint {hidePoint.Position} not found in ScoresDatabase.");
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

    private HidePoint UpdateDatabaseEvent()
    {
        ScoresDatabase db_copy = new ScoresDatabase(Scores);
        foreach (HidePoint hp in db_copy.Scores.Keys)
        {
            Scores[hp] = hp.Score; // Atualiza o score do HidePoint
        }

        return Scores.Keys.OrderByDescending(x => x.Score).First();
    }

    public ScoresDatabase Clear()
    {
        ScoresDatabase databaseCopy = new ScoresDatabase(Scores);
        for (int i = 0; i < databaseCopy.Scores.Count; i++)
        {
            HidePoint point = databaseCopy.Scores.Keys.ElementAt(i);
            Scores[point] = 0f; // Zera os scores
        }
        return databaseCopy;
    }
}
