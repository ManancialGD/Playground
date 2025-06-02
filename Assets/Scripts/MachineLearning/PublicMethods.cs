using System.Globalization;
using UnityEngine;

public static class PublicMethods
{
    public static Vector3 RoundVector3(Vector3 v, int decimalPlaces = 4)
    {
        float multiplier = Mathf.Pow(10, decimalPlaces);
        return new Vector3(
            Mathf.Round(v.x * multiplier) / multiplier,
            Mathf.Round(v.y * multiplier) / multiplier,
            Mathf.Round(v.z * multiplier) / multiplier
        );
    }

    public static Vector3 StringToVector3(string value)
    {
        value = value.Trim('(', ')');
        string[] split = value.Split(',');

        if (split.Length != 3)
            throw new System.FormatException("Formato inválido para Vector3");

        return RoundVector3(
            new Vector3(
                float.Parse(split[0], CultureInfo.InvariantCulture),
                float.Parse(split[1], CultureInfo.InvariantCulture),
                float.Parse(split[2], CultureInfo.InvariantCulture)
            ),
            4
        );
    }

    public static string Vector3ToString(Vector3 v)
    {
        return $"({v.x.ToString(CultureInfo.InvariantCulture)}, {v.y.ToString(CultureInfo.InvariantCulture)}, {v.z.ToString(CultureInfo.InvariantCulture)})";
    }
}
