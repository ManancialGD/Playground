using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    [SerializeField]
    private int currentRoomID = 0;

    public int CurrentRoomID => currentRoomID;

    public event Action<int> OnRoomChanged;

    private int previusRoomID = 0;

    public int startAtID = 0;

    private Room[] rooms;

    [SerializeField]
    private CustomCharacterController character;

    [SerializeField]
    private Transform head;

    [SerializeField]
    private LineRenderer laser;

    [SerializeField]
    private string gameSceneName = "Game";

    [SerializeField]
    private string menuSceneName = "MainMenu";

    [SerializeField]
    private Button continueButton;

    private Room currentRoom;

    private float timer;
    private bool killing = false;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == menuSceneName && continueButton != null)
        {
            int i = PlayerPrefs.GetInt("StartID", 0);
            // Debug.Log("[RoomManager] StartID from PlayerPrefs: " + i);

            if (i == 0)
                continueButton.gameObject.SetActive(false);
            else
                continueButton.gameObject.SetActive(true);
        }

        // Debug.Log(
        //     "[RoomManager] Active scene:  {SceneManager.GetActiveScene().name}, Game Scene {gameSceneName}"
        // );
        if (SceneManager.GetActiveScene().name != gameSceneName)
        {
            // Debug.Log("[RoomManager] Not in game scene, skipping initialization.");
            return;
        }

        startAtID = PlayerPrefs.GetInt("StartID", 0);

        laser.gameObject.SetActive(false);
        head = character
            .GetComponentsInChildren<RagDollLimb>()
            .FirstOrDefault(l => l.ThisLimbType == LimbType.Head)
            ?.transform;

        // Debug.Log("[RoomManager] Initializing rooms...");
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
            // Debug.LogError("[RoomManager] No valid start room with a SpawnPosition found!");
        }

        // Notify all EnemyActivityManagers BEFORE any room destruction
        currentRoomID = spawnRoomID;
        NotifyAllEnemyActivityManagers(currentRoomID);

        foreach (Room room in rooms)
        {
            if (room == null)
                continue;

            if (room.ID == spawnRoomID)
            {
                // Debug.Log($"[RoomManager] Activating start room with ID {spawnRoomID}");
                room.gameObject.SetActive(true);
                currentRoom = room;
                timer = room.Time;
            }
            else if (room.ID < spawnRoomID)
            {
                // Debug.Log($"[RoomManager] Destroying room with ID {room.ID} (before start)");
                Destroy(room.gameObject);
            }
            else
            {
                // Debug.Log($"[RoomManager] Deactivating room with ID {room.ID}");
                room.gameObject.SetActive(false);
            }
        }

        foreach (Door door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            // Debug.Log($"[RoomManager] Subscribing to door in room {door.RoomID}");
            door.EnterRoom += OnEnterRoom;
        }

        if (character == null)
        {
            // Debug.Log("[RoomManager] Character not set, searching in scene...");
            character = FindAnyObjectByType<CustomCharacterController>();
        }

        if (character != null)
        {
            // Debug.Log(
            //     $"[RoomManager] Placing character at spawn position of room {currentRoom.ID}"
            // );
            character.transform.position = currentRoom.SpawnPosition.position;
            character.transform.rotation = currentRoom.SpawnPosition.rotation;
        }
        else
        {
            // Debug.LogWarning("[RoomManager] Character controller not found!");
        }

        OnRoomChanged?.Invoke(currentRoomID);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        if (killing)
        {
            Vector3 laserStart = head.transform.position;
            laserStart.y = laser.GetPosition(0).y;
            laser.SetPosition(0, laserStart);

            Vector3 laserEnd = head.transform.position;
            laserEnd.y = laser.GetPosition(1).y;
            laser.SetPosition(1, laserEnd);
        }

        if (currentRoom == null)
            return;

        if (!currentRoom.HasTimer)
            return;

        timer -= Time.deltaTime;

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.CeilToInt(timer % 60f);

        string timeText;

        if (timer > 0)
            timeText = string.Format("{0:00}:{1:00}", minutes, seconds);
        else
            timeText = "00:00";

        foreach (TextMeshProUGUI timerText in currentRoom.TimerTexts)
        {
            timerText.text = timeText;
        }

        if (timer <= 0f && !killing)
        {
            KillWithLaser();
        }
    }

    public void KillWithLaser()
    {
        laser.gameObject.SetActive(true);
        Vector3 laserStart = head.transform.position;
        laserStart.y = laser.GetPosition(0).y;
        laser.SetPosition(0, laserStart);

        Vector3 laserEnd = head.transform.position;
        laserEnd.y = laser.GetPosition(1).y;
        laser.SetPosition(1, laserEnd);

        killing = true;
        Invoke(nameof(Kill), 2f);
    }

    private void Kill()
    {
        if (character.TryGetComponent(out HealthModule healthModule))
        {
            killing = false;
            Vector3 laserStart = new(500, 0, 500);
            laserStart.y = laser.GetPosition(0).y;
            laser.SetPosition(0, laserStart);

            Vector3 laserEnd = new(500, 0, 500);
            laserEnd.y = laser.GetPosition(1).y;
            laser.SetPosition(1, laserEnd);

            laser.gameObject.SetActive(false);
            healthModule.DieByLaser();
        }
    }

    public bool TryOpenDoor(int roomID, bool isDeadRoom)
    {
        Debug.Log($"[RoomManager] Trying to open door to room {roomID} (currentRoomID: {currentRoomID})");

        if (roomID == 100) return true;
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

        if (isDeadRoom)
            return true;

        previusRoomID = currentRoomID;

        currentRoom = room;
        currentRoomID = roomID;

        PlayerPrefs.SetInt("StartID", currentRoomID);
        PlayerPrefs.Save();

        timer = room.Time;

        // Ensure all EnemyActivityManagers are notified
        NotifyAllEnemyActivityManagers(currentRoomID);

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

    public void EnterNewGame()
    {
        startAtID = 0;

        currentRoomID = startAtID;
        previusRoomID = 0;

        PlayerPrefs.SetInt("StartID", startAtID);
        PlayerPrefs.Save();

        timer = 0f;
        killing = false;

        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        startAtID = PlayerPrefs.GetInt("StartID", 0);

        currentRoomID = startAtID;
        previusRoomID = 0;

        timer = 0f;
        killing = false;

        SceneManager.LoadScene(gameSceneName);
    }

    public void EndGame()
    {
        PlayerPrefs.SetInt("StartID", 0);
        PlayerPrefs.Save();

        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(menuSceneName);
    }

    private void NotifyAllEnemyActivityManagers(int newRoomID)
    {
        // Find all EnemyActivityManagers in the scene, including those that might be
        // in rooms that are about to be destroyed
        EnemyActivityManager[] allManagers = FindObjectsByType<EnemyActivityManager>(FindObjectsSortMode.None);

        Debug.Log($"[RoomManager] Found {allManagers.Length} EnemyActivityManagers to notify about room change to {newRoomID}");

        foreach (EnemyActivityManager manager in allManagers)
        {
            if (manager != null)
            {
                // Force immediate notification before any destruction occurs
                manager.ForceRoomChanged(newRoomID);
            }
        }
    }
}
