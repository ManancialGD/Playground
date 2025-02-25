using UnityEngine;

public class CharacterShooter : MonoBehaviour
{
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cam;

    [SerializeField] private float maxDistance = 150f;

    public void UpdateAim()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxDistance))
        {
            aimTarget.position = hit.point;
        }
        else
            aimTarget.position = cam.forward * maxDistance;
    }
}
