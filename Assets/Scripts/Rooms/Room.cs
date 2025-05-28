using UnityEngine;

public class Room : MonoBehaviour
{
    [field: SerializeField] public int ID { get; private set; }
    [field: SerializeField] public Transform SpawnPosition { get; private set; }
}
