using UnityEngine;

public class EnemyActivityManager : MonoBehaviour
{
    [SerializeField]
    private MonoBehaviour[] scriptsToToggle;

    [SerializeField]
    private int roomID;

    private RoomManager roomManager;

    private void Start()
    {
        roomManager = FindAnyObjectByType<RoomManager>();
        if (roomManager != null)
        {
            roomManager.OnRoomChanged += OnRoomChanged;

            bool shouldBeEnabled = roomManager.CurrentRoomID == roomID;
            SetScriptsEnabled(shouldBeEnabled);
        }
        else
        {
            SetScriptsEnabled(false);
        }
    }

    private void OnDestroy()
    {
        roomManager.OnRoomChanged -= OnRoomChanged;
    }

    private void OnRoomChanged(int newRoomID)
    {
        bool shouldBeEnabled = newRoomID == roomID;
        SetScriptsEnabled(shouldBeEnabled);

        Debug.Log(
            $"[EnemyActivityManager] Room {roomID}: Scripts {(shouldBeEnabled ? "ENABLED" : "DISABLED")} (Player in room {newRoomID})"
        );
    }

    private void SetScriptsEnabled(bool enabled)
    {
        foreach (MonoBehaviour script in scriptsToToggle)
            if (script != null)
                script.enabled = enabled;
    }
}
