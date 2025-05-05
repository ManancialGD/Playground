using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onInteractEvent;

    public virtual void Interact()
    {
        onInteractEvent?.Invoke();
    }
}