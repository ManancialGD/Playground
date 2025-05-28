using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Door : Interactable
{
    [field: SerializeField]
    public int RoomID { get; private set; }

    [SerializeField]
    private Transform doorEnterTrigger;
    [SerializeField]
    private float triggerRadius = 0.25f;

    public Action<int> EnterRoom;


    private bool enteredRoom = false;

    [SerializeField] Collider[] collisions;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (doorEnterTrigger != null && !enteredRoom)
        {
            Collider[] hits = Physics.OverlapSphere(doorEnterTrigger.position, triggerRadius);
            foreach (var hit in hits)
            {
                if (hit.GetComponent<CustomCharacterController>() != null)
                {
                    EnterRoom?.Invoke(RoomID);
                    Array.ForEach(collisions, c => c.enabled = true);
                    enteredRoom = true;
                    anim.SetTrigger("Shut");
                    break;
                }
            }
        }
    }


    public override void Interact()
    {
        base.Interact();

        if (RoomManager.Instance.TryOpenDoor(RoomID))
        {
            Array.ForEach(collisions, c => c.enabled = false);
            anim.SetTrigger("Open");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (doorEnterTrigger == null) return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(doorEnterTrigger.position, triggerRadius);
    }
#endif
}
