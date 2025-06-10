using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthOverlay : MonoBehaviour
{
    [SerializeField]
    private Image overlayImage;

    [SerializeField]
    private float maxAlpha = 0.7f;

    private HealthModule healthModule;
    private readonly int maxHealth = 100;
    private int lastHealth = 100;

    private void Start()
    {
        healthModule = GetComponent<HealthModule>();

        if (overlayImage == null)
            Debug.LogError("No overlay image");

        if (overlayImage != null)
            overlayImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void Update()
    {
        if (healthModule == null || overlayImage == null)
            return;

        int currentHealth = GetCurrentHealth();

        if (currentHealth != lastHealth)
        {
            UpdateOverlay(currentHealth);
            lastHealth = currentHealth;
        }
    }

    private int GetCurrentHealth() => healthModule.CurrentHealth;

    private void UpdateOverlay(int health)
    {
        float healthPercent = (float)health / maxHealth;
        float damagePercent = 1f - healthPercent;
        float alpha = damagePercent * maxAlpha;

        overlayImage.color = new Color(1f, 1f, 1f, alpha);
    }
}
