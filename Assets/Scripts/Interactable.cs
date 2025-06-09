using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onInteractEvent;

    public virtual void Interact()
    {
        Debug.Log($"{gameObject.name} was interacted with.");
        onInteractEvent?.Invoke();
    }
}
