using UnityEngine;

public class EnemyActivityManager : MonoBehaviour
{
    [SerializeField]
    private MonoBehaviour[] scriptsToToggle;

    [SerializeField]
    private int roomID;

    private void Start()
    {
        RoomManager.OnRoomChanged += OnRoomChanged;
        SetScriptsEnabled(false);

        Debug.Log(
            $"[EnemyActivityManager] Room {roomID}: Waiting for RoomManager initialization..."
        );
    }

    private void OnDestroy() => RoomManager.OnRoomChanged -= OnRoomChanged;

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
