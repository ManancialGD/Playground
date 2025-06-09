using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

public class Door : Interactable
{
    [field: SerializeField]
    public int RoomID { get; private set; }

    [SerializeField]
    private Transform doorEnterTrigger;
    [SerializeField]
    private float triggerRadius = 0.25f;

    [SerializeField]
    private bool canOpen = true;

    [SerializeField]
    private bool isKillRoom = false;

    public Action<int> EnterRoom;

    [SerializeField]
    private UnityEvent EnterRoomEvent;


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
                    EnterRoomEvent?.Invoke();
                    Invoke(nameof(EnableColl), 0.25f);
                    enteredRoom = true;
                    anim.SetTrigger("Shut");
                    if (isKillRoom)
                    {
                        FindAnyObjectByType<RoomManager>()?.KillWithLaser();
                    }
                    break;
                }
            }
        }
    }

    private void EnableColl()
    {
        Array.ForEach(collisions, c => c.enabled = true);
    }


    public override void Interact()
    {
        base.Interact();

        if (canOpen)
            OpenDoor();
    }

    public void OpenDoor()
    {
        if (FindAnyObjectByType<RoomManager>()?.TryOpenDoor(RoomID, isKillRoom) == true)
        {
            Array.ForEach(collisions, c => c.enabled = false);
            anim.SetTrigger("Open");
        }
    }

    public void SetCanOpen(bool canOpen)
    {
        this.canOpen = canOpen;
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
