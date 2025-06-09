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
        healthModule = FindFirstObjectByType<HealthModule>();

        if (overlayImage == null)
            overlayImage = GetComponent<Image>();

        if (overlayImage != null)
            overlayImage.color = new Color(1f, 0f, 0f, 0f);
    }

    private void Update()
    {
        if (healthModule == null || overlayImage == null) return;

        int currentHealth = GetCurrentHealth();

        if (currentHealth != lastHealth)
        {
            UpdateOverlay(currentHealth);
            lastHealth = currentHealth;
        }
    }

    private int GetCurrentHealth()
    {
        var field = typeof(HealthModule).GetField("currentHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(healthModule) : maxHealth;
    }

    private void UpdateOverlay(int health)
    {
        float healthPercent = (float)health / maxHealth;
        float damagePercent = 1f - healthPercent;
        float alpha = damagePercent * maxAlpha;

        overlayImage.color = new Color(1f, 0f, 0f, alpha);
    }
}