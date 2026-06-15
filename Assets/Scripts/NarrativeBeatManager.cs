using UnityEngine;
using System.Collections;

public class NarrativeBeatManager : MonoBehaviour
{
    public static NarrativeBeatManager Instance { get; private set; }

    [System.Serializable]
    public class Beat
    {
        public string name;
        public AudioClip clip;
        [TextArea(1, 3)] public string description;
    }

    public enum BeatIndex
    {
        GameStart = 0,
        Round1End = 1,
        Round2Start = 2,
        Round2End = 3,
        Round3Start = 4,
        Round3End = 5,
    }

    [SerializeField] private Beat[] beats = new Beat[6];
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float gameStartDelay = 0.5f;
    [SerializeField] private FirstGrabDissolveEvent firstGrabDissolveEvent;
    [SerializeField] private GameObject beatIndicatorObject;

    private int activeBeatCount = 0;
    public bool IsPlaying => activeBeatCount > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (beatIndicatorObject != null)
            beatIndicatorObject.SetActive(false);

        StartCoroutine(PlayBeatDelayed(BeatIndex.GameStart, gameStartDelay));
    }

    public void OnFirstGrab()
    {
        firstGrabDissolveEvent?.Trigger();
    }

    public void OnRoundStarted(int roundIndex)
    {
        switch (roundIndex)
        {
            case 1: StartCoroutine(PlayBeatThenDissolve(BeatIndex.Round2Start)); break;
            case 2: StartCoroutine(PlayBeatThenDissolve(BeatIndex.Round3Start)); break;
        }
    }

    public void OnRoundCompleted(int roundIndex)
    {
        firstGrabDissolveEvent?.Trigger();
        switch (roundIndex)
        {
            case 0: PlayBeat(BeatIndex.Round1End); break;
            case 1: PlayBeat(BeatIndex.Round2End); break;
            case 2: PlayBeat(BeatIndex.Round3End); break;
        }
    }

    private IEnumerator PlayBeatThenDissolve(BeatIndex index)
    {
        PlayBeat(index);
        yield return new WaitWhile(() => IsPlaying);
        firstGrabDissolveEvent?.Trigger();
    }

    private void PlayBeat(BeatIndex index)
    {
        int i = (int)index;
        if (beats == null || i >= beats.Length) return;

        Beat beat = beats[i];
        if (beat == null || beat.clip == null || audioSource == null) return;

        audioSource.PlayOneShot(beat.clip);
        StartCoroutine(TrackBeatDuration(beat.clip.length));
    }

    private IEnumerator TrackBeatDuration(float duration)
    {
        activeBeatCount++;
        if (beatIndicatorObject != null) beatIndicatorObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        activeBeatCount--;
        if (activeBeatCount <= 0 && beatIndicatorObject != null)
            beatIndicatorObject.SetActive(false);
    }

    private IEnumerator PlayBeatDelayed(BeatIndex index, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayBeat(index);
    }
}
