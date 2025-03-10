using UnityEngine;

[System.Serializable]
public struct AudioCollection
{
    public AudioClip[] audioClips;

    public AudioCollection(AudioClip[] clips)
    {
        if (clips != null)
            audioClips = clips;
        else
            audioClips = new AudioClip[0];
    }

    public readonly AudioClip GetRandomClip()
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            return null;
        }
        return audioClips[Random.Range(0, audioClips.Length)];
    }
}