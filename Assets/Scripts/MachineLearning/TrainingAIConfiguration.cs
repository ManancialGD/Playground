using UnityEngine;

public class TrainingAIConfiguration : MonoBehaviour
{
    [Header("Parameters importance")]
    [SerializeField, Range(0, 1)]
    public float DistanceImportance = 0.5f;

    [SerializeField, Range(0, 1)]
    public float ExpositionImportance = 0.5f;

    [SerializeField, Range(0, 1)]
    public float ReactionImportance = 0.5f;

    [SerializeField, Range(0, 1)]
    public float BaseLearningRate = 0.1f;

    [SerializeField, Range(0, 1)]
    public float LearningRateDecay = 0.001f;
}
