using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
}
