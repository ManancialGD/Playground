using UnityEngine;
using UnityEngine.Events;

public class TriggerEnterEvent : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CustomCharacterController>() != null)
        {
            onPlayerEnter?.Invoke();
        }
    }
}
