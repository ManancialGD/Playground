using UnityEngine;
using UnityEngine.Audio;

public class CharacterSounds : MonoBehaviour
{
    [SerializeField] private ObjectPool audioPool;
    [SerializeField] private Transform gunTip;
    [SerializeField] private CharacterShooter characterShooter;

    [SerializeField] private AudioCollection shootAudioCollection;
    [SerializeField] private AudioMixerGroup shotsAdioMixerGroup;

    private void OnEnable()
    {
        characterShooter.ShootEvent += OnShoot;
    }
    private void OnDisable()
    {
        characterShooter.ShootEvent -= OnShoot;
    }

    public void OnShoot()
    {
        GameObject audioObject = audioPool.GetObject();
        if (audioObject.TryGetComponent<AudioSource>(out var audioSource))
        {
            audioObject.transform.position = gunTip.position;
            audioObject.transform.SetParent(gunTip, true);
            audioSource.outputAudioMixerGroup = shotsAdioMixerGroup;
            audioSource.clip = shootAudioCollection.GetRandomClip();
            // audioSource.pitch = RandomPitch(.9f, 1.1f); // this shouldn't be necessary, the sounds itself should have differentiations
            audioSource.maxDistance = 50;
            audioSource.Play();
        }
    }

    private float RandomPitch(float min = .8f, float max = 1.2f)
    {
        return Random.Range(min, max);
    }

    private void OnValidate()
    {
        if (characterShooter == null)
            characterShooter = GetComponent<CharacterShooter>();
    }
}
