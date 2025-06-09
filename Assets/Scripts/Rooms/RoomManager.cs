using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [SerializeField]
    private int currentRoomID = 0;

    public int CurrentRoomID => currentRoomID;

    public static event Action<int> OnRoomChanged;

    private int previusRoomID = 0;

    public int startAtID = 0;

    private Room[] rooms;

    [SerializeField]
    private CustomCharacterController character;

    private Room currentRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[RoomManager] Initializing rooms...");
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

        currentRoomID = startAtID;

        // Find the starting room with a valid SpawnPosition, lowering the ID if needed
        int spawnRoomID = startAtID;
        Room spawnRoom = null;
        while (spawnRoomID >= 0)
        {
            spawnRoom = rooms.FirstOrDefault(r =>
                r.ID == spawnRoomID && r != null && r.SpawnPosition != null
            );
            if (spawnRoom != null)
            {
                break;
            }
            spawnRoomID--;
        }

        if (spawnRoom == null)
        {
            Debug.LogError("[RoomManager] No valid start room with a SpawnPosition found!");
        }

        foreach (Room room in rooms)
        {
            if (room == null)
                continue;

            if (room.ID == spawnRoomID)
            {
                Debug.Log($"[RoomManager] Activating start room with ID {spawnRoomID}");
                room.gameObject.SetActive(true);
                currentRoom = room;
                currentRoomID = spawnRoomID;
            }
            else if (room.ID < spawnRoomID)
            {
                Debug.Log($"[RoomManager] Destroying room with ID {room.ID} (before start)");
                Destroy(room.gameObject);
            }
            else
            {
                Debug.Log($"[RoomManager] Deactivating room with ID {room.ID}");
                room.gameObject.SetActive(false);
            }
        }

        foreach (Door door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            Debug.Log($"[RoomManager] Subscribing to door in room {door.RoomID}");
            door.EnterRoom += OnEnterRoom;
        }

        if (character == null)
        {
            Debug.Log("[RoomManager] Character not set, searching in scene...");
            character = FindAnyObjectByType<CustomCharacterController>();
        }

        if (character != null)
        {
            Debug.Log(
                $"[RoomManager] Placing character at spawn position of room {currentRoom.ID}"
            );
            character.transform.position = currentRoom.SpawnPosition.position;
            character.transform.rotation = currentRoom.SpawnPosition.rotation;
        }
        else
        {
            Debug.LogWarning("[RoomManager] Character controller not found!");
        }

        OnRoomChanged?.Invoke(currentRoomID);
    }

    public bool TryOpenDoor(int roomID)
    {
        Debug.Log(
            $"[RoomManager] Trying to open door to room {roomID} (currentRoomID: {currentRoomID})"
        );
        if (roomID <= currentRoomID)
        {
            Debug.Log(
                $"[RoomManager] Cannot open door to room {roomID}: already visited or invalid."
            );
            return false;
        }

        Room room = rooms.First(r => r.ID == roomID);

        Debug.Log($"[RoomManager] Activating room {roomID}");
        room.gameObject.SetActive(true);

        previusRoomID = currentRoomID;

        currentRoom = room;
        currentRoomID = roomID;

        OnRoomChanged?.Invoke(currentRoomID);

        return true;
    }

    public void OnEnterRoom(int roomID)
    {
        Debug.Log($"[RoomManager] Entered room {roomID}, destroying previous room {previusRoomID}");
        Room room = rooms.FirstOrDefault(r => r.ID == previusRoomID);

        if (room != null)
        {
            Destroy(room.gameObject);
            Debug.Log($"[RoomManager] Destroyed room {previusRoomID}");
        }
        else
        {
            Debug.LogWarning(
                $"[RoomManager] Previous room {previusRoomID} not found for destruction."
            );
        }
    }
}
