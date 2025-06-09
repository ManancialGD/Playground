using UnityEngine;
using UnityEngine.Events;

public class TriggerEnterEvent : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other collider has a Rigidbody component
        if (other.GetComponent<CustomCharacterController>() != null)
        {
            onPlayerEnter?.Invoke();
        }
    }
}
