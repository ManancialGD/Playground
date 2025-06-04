using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour, IPooledObject
{
    private AudioSource audioSource;

    private bool isPlaying = false;

    private ObjectPool thisObjectPool;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetPool(ObjectPool pool)
    {
        thisObjectPool = pool;
    }

    public void ReturnToPoll()
    {
        if (thisObjectPool == null) Destroy(gameObject);
        
        gameObject.SetActive(false);
        audioSource.outputAudioMixerGroup = null;
        thisObjectPool.ReturnObject(gameObject);
    }

    public void StartObject()
    {
        if (audioSource.isPlaying)
        {
            isPlaying = true;
        }
    }

    public void StopObject()
    {
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying && audioSource.isPlaying)
        {
            isPlaying = true;
        }
        else if (isPlaying && !audioSource.isPlaying)
        {
            isPlaying = false;
            ReturnToPoll();
        }
    }
}
