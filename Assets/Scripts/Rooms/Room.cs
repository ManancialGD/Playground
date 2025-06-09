using TMPro;
using UnityEngine;

public class Room : MonoBehaviour
{
    [field: SerializeField] public int ID { get; private set; }
    [field: SerializeField] public Transform SpawnPosition { get; private set; }
    [field: SerializeField] public float Time { get; private set; }
    [field: SerializeField] public bool HasTimer { get; private set; } = true;
    [field: SerializeField] public TextMeshProUGUI[] TimerTexts { get; private set; }
}
