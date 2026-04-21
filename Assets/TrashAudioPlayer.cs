using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrashAudioPlayer : MonoBehaviour
{
    public static TrashAudioPlayer Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField][Range(0f, 1f)] private float defaultVolume = 1f;

    [Header("Pop / feedback de succión")]
    [SerializeField] private AudioClip popClip;
    [SerializeField][Range(0f, 1f)] private float popVolume = 0.8f;
    [SerializeField] private float popPitchMin = 0.9f;
    [SerializeField] private float popPitchMax = 1.1f;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, defaultVolume * volumeScale);
    }

    public void PlayPop()
    {
        if (popClip == null || audioSource == null) return;

       
        audioSource.pitch = Random.Range(popPitchMin, popPitchMax);
        audioSource.PlayOneShot(popClip, popVolume);
       
    }
}