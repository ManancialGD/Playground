using System.Collections;
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
