using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public class PlayerSettings : ScriptableObject
{
    [field: SerializeField] public float SensitivityX { get; set; } = 1;
    [field: SerializeField] public float SensitivityY { get; set; } = .5f;
    [field: SerializeField] public float AimMultiplier { get; set; } = .25f;
    [field: SerializeField] public bool InvertMouseY { get; set; } = false;

    public float MainVolume
    {
        set
        {
            SetAudioVolume("MainVol", value);
        }
    }
    public float MusicVolume
    {
        set
        {
            SetAudioVolume("MusicVol", value);
        }
    }
    public float SoundEffectsVolume
    {
        set
        {
            SetAudioVolume("SoundEffectsVol", value);
        }
    }

    private static AudioMixer mainAudioMixer;

    public static void SetAudioVolume(string volumeParam, float value)
    {
        if (mainAudioMixer == null)
            if (!LoadAudioMixer()) // This is expensive, so we hope to do this only once, max.
                return;

        // [0-100] to [0-1]
        value *= .01f;

        // Ensure the volume is not too small or negative to avoid invalid calculations
        if (value <= 1e-5f)
            value = 1e-5f; // Clamp to the lower bound

        // Convert the linear volume (0.0 to 1.0) to a decibel scale using a logarithmic function
        // Unity’s audio mixer expects decibel values. `20 * Mathf.Log10(v)` converts:
        // - Linear input of 1.0 to 0 dB (no attenuation).
        // - Linear input < 1.0 to negative decibels (attenuated volume).
        // - Values near 0 are clamped to approximately -80 dB.
        mainAudioMixer.SetFloat(volumeParam, Mathf.Log10(value) * 20);
    }

    /// <summary>
    /// Try to load main mixer from Resources folder.
    /// </summary>
    /// <returns>True if successfull, False if failed to load</returns>
    private static bool LoadAudioMixer()
    {
        try
        {
            mainAudioMixer = Resources.Load<AudioMixer>("MainMixer");
            if (mainAudioMixer == null)
            {
                Debug.LogError("Failed to load MainMixer");
                return false;
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception occurred while loading MainMixer: {ex.Message}");
            return false;
        }
    }
}
