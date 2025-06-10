using System.Collections;
using UnityEngine;

public class EnemyActivityManager : MonoBehaviour
{
    [SerializeField]
    private MonoBehaviour[] scriptsToToggle;

    [SerializeField]
    private int roomID;

    private RoomManager roomManager;
    private bool isSubscribed = false;

    private IEnumerator Start()
    {
        yield return null;

        roomManager = FindAnyObjectByType<RoomManager>();
        if (roomManager != null)
        {
            roomManager.OnRoomChanged += OnRoomChanged;
            isSubscribed = true;

            bool shouldBeEnabled = roomManager.CurrentRoomID == roomID;
            SetScriptsEnabled(shouldBeEnabled);

            Debug.Log($"[EnemyActivityManager] Room {roomID}: Initialized, scripts {(shouldBeEnabled ? "ENABLED" : "DISABLED")} (Player in room {roomManager.CurrentRoomID})");
        }
        else
        {
            SetScriptsEnabled(false);
            Debug.LogWarning($"[EnemyActivityManager] Room {roomID}: RoomManager not found, disabling scripts");
        }
    }

    private void OnDestroy()
    {
        if (roomManager != null && isSubscribed)
        {
            roomManager.OnRoomChanged -= OnRoomChanged;
            isSubscribed = false;
            Debug.Log($"[EnemyActivityManager] Room {roomID}: Unsubscribed from RoomManager events");
        }
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

    /// <summary>
    /// Force immediate room change notification, used by RoomManager to ensure
    /// all managers are notified before any destruction occurs
    /// </summary>
    public void ForceRoomChanged(int newRoomID)
    {
        OnRoomChanged(newRoomID);
    }
}
