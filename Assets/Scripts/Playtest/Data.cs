using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DataSpace
{
    public class Data
    {
        public enum Type
        {
            SIMPLE,
            COMPLEX,
        }

        public enum GraphFormat
        {
            DEFAULT,
            ASCENDING,
        }

        public string Name { get; set; } = "DataDisplay";
        private string defaultFilePath = Application.dataPath + "/StreamingAssets/DataFiles/";
        public List<(float, float)> Values { get; private set; }
        public Type DataType { get; private set; }

        public Data(string name, string data = null, Type dataType = Type.SIMPLE, bool save = false)
        {
            Name = name;
            Values = new List<(float, float)>();
            DataType = dataType;

            if (data != null)
                RewriteData(data);

            if (save)
                SaveDataInDevice();
            Application.quitting += OnApplicationQuit;
        }

        public List<(float, float)> GetValues() => Values;

        public float GetTime(int index) => GetValues()[index].Item1;

        public float GetValue(int index) => GetValues()[index].Item2;

        public void RewriteData(string data)
        {
            Values = ReadData(data);
        }

        public void Feed((float time, (float v1, float v2) value) data)
        {
            if (data.value.v2 != 0 && DataType == Type.SIMPLE)
            {
                Debug.LogWarning("Data fed with COMPLEX data, but data type is SIMPLE");
            }
            Values.Add((data.time, ProcessData(data.value)));
        }

        private float ProcessData((float, float) data)
        {
            switch (DataType)
            {
                case Type.SIMPLE:
                    return data.Item1;

                case Type.COMPLEX:
                    return data.Item1 - data.Item2;

                default:
                    Debug.LogError("ERROR - Invalid data type input");
                    return 0;
            }
        }

        private List<(float, float)> ReadData(string data)
        {
            string[] dataLines = data.Split('\n');
            Debug.Log("File has " + dataLines.Length + " lines");

            var newData = new List<(float, float)>();

            foreach (string line in dataLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] lineData = line.Split('|');
                Debug.Log("Data || Time: " + lineData[0] + " | Value: " + lineData[1]);

                // Ensure the data is correctly formatted with decimal points
                float time = float.Parse(lineData[0], CultureInfo.InvariantCulture);
                float value = float.Parse(lineData[1], CultureInfo.InvariantCulture);

                (float, float) dataPoint = (time, value);

                newData.Add(dataPoint);
            }

            return newData;
        }

        public string WriteData(List<(float, float)> data)
        {
            string newData = "";

            foreach (var point in data)
            {
                newData +=
                    $"{point.Item1.ToString(CultureInfo.InvariantCulture)}|{point.Item2.ToString(CultureInfo.InvariantCulture)}\n";
            }

            return newData;
        }

        public void SaveDataInDevice()
        {
            if (!FolderExists(defaultFilePath))
                CreateFolder(defaultFilePath);

            string filePath = defaultFilePath + Name + ".txt";

            if (!FileExists(filePath))
            {
                CreateFile(filePath, WriteData(Values));
            }
            else
            {
                EditFile(filePath, WriteData(Values));
            }
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        private void SaveData()
        {
            SaveDataInDevice();
        }

        private bool FolderExists(string path) => Directory.Exists(path);

        private void CreateFolder(string path) => Directory.CreateDirectory(path);

        private bool FileExists(string path) => File.Exists(path);

        private void CreateFile(string path, string content) => File.WriteAllText(path, content);

        private void EditFile(string path, string content) => File.WriteAllText(path, content);

        public Data NormalizeData(Data data, int keyframes) // normalized format mode
        {
            List<(float time, float value)> dataValues = data.GetValues();
            float total_time = dataValues[dataValues.Count - 1].time;

            List<(float, float)> interpolatedValues = new List<(float, float)>();

            float timeInterval = total_time / keyframes;

            for (float time = 0; time < total_time; time += timeInterval)
            {
                // Encontrar os pontos anterior e posterior mais próximos
                (float time, float value) prevPoint = (0, 0);
                (float time, float value) nextPoint = (total_time, 0);

                for (int i = 0; i < dataValues.Count; i++)
                {
                    if (dataValues[i].time <= time)
                        prevPoint = dataValues[i];
                    else
                    {
                        nextPoint = dataValues[i];
                        break;
                    }
                }

                // Interpolação linear entre os pontos
                float t = (time - prevPoint.time) / (nextPoint.time - prevPoint.time);
                float interpolatedValue = Mathf.Lerp(prevPoint.value, nextPoint.value, t);

                interpolatedValues.Add((time, interpolatedValue));
            }

            Debug.LogWarning(
                "Data normalized(" + keyframes + " keyframes): " + interpolatedValues.Count
            );

            return new Data(data.Name, WriteData(interpolatedValues));
        }

        /* public (int, int) GetClosestKeyframeToValue(){
             int closestSlot = 0;
             float closestValue = 0;
             for(int i = 0; i < Values.Count; i++){
                 if(Value)
             }
         }*/

        List<(float, float)> ApplyGaussianBlur(List<(float, float)> data, float kernelPercent)
        {
            List<(float, float)> smoothedData = new List<(float, float)>();

            // Define um kernelSize limitado para evitar distorções
            int kernelSize = Mathf.RoundToInt(data.Count * (kernelPercent / 100f));
            kernelSize = Mathf.Clamp(kernelSize, 3, data.Count / 2); // Limita o tamanho do kernel
            if (kernelSize % 2 == 0)
                kernelSize++; // Garantir tamanho ímpar

            float sigma = (kernelSize - 1) / 4.0f; // Ajuste de sigma
            float[] kernel = new float[kernelSize];
            float sum = 0;

            // Criando o kernel gaussiano
            for (int i = 0; i < kernelSize; i++)
            {
                float x = i - (kernelSize - 1) / 2.0f;
                kernel[i] = Mathf.Exp(-0.5f * (x * x) / (sigma * sigma));
                sum += kernel[i];
            }

            // Normalizando o kernel
            for (int i = 0; i < kernelSize; i++)
                kernel[i] /= sum;

            int halfSize = kernelSize / 2;

            // Aplicação do filtro gaussiano
            for (int i = 0; i < data.Count; i++)
            {
                float smoothedValue = 0;
                float weightSum = 0;

                for (int j = -halfSize; j <= halfSize; j++)
                {
                    int index = Mathf.Clamp(i + j, 0, data.Count - 1);
                    smoothedValue += data[index].Item2 * kernel[j + halfSize];
                    weightSum += kernel[j + halfSize];
                }

                smoothedData.Add((data[i].Item1, smoothedValue / weightSum));
            }

            return smoothedData;
        }

        public Data GaussianBlur(float kernelSize)
        {
            if (kernelSize <= 0)
            {
                Debug.LogError("Value needs to be bigger than 0");
                return null;
            }
            Data data = this;
            List<(float, float)> appliedData = ApplyGaussianBlur(data.Values, kernelSize);
            Data newData = new Data(data.Name, WriteData(appliedData));
            return newData;
        }
    }
}
