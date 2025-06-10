using UnityEngine;

public class BootSounds : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] bootSounds;

    private AudioSource source;

    private bool canPlay = true;

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void Play()
    {
        if (!canPlay) return;

        AudioClip clip = bootSounds[Random.Range(0, bootSounds.Length)];
        if (clip != null)
        {
            if (source != null)
            {
                source.clip = clip;
                source.Play();
                canPlay = false;
                Invoke(nameof(ResetCanPlay), 0.25f);
            }
            else
            {
                Debug.LogWarning("[BootSounds] No AudioSource component found on the GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("[BootSounds] No boot sound clips available to play.");
        }
    }
    
    private void ResetCanPlay()
    {
        canPlay = true;
    }
}
