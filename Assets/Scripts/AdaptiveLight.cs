using UnityEngine;

[RequireComponent(typeof(Light))]
public class AdaptiveFlashlight : MonoBehaviour
{
    private Light flashlight;
    [SerializeField] private float maxIntensity = 40000;
    [SerializeField] private float minIntensity = 10f;
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float minDistance = 5;

    private void Awake()
    {
        flashlight = GetComponent<Light>();
    }

    private void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxDistance))
        {
            float distance = Mathf.Clamp(hit.distance, 0, maxDistance);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, (distance - minDistance) / (maxDistance - minDistance));
            flashlight.intensity = intensity;
        }
        else
        {
            flashlight.intensity = maxIntensity;
        }
    }
}
