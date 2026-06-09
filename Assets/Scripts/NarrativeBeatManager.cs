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
        [TextArea(1, 3)]
        public string description;
    }

    public enum BeatIndex
    {
        GameStart = 0,
        FirstGrab = 1,
        Round1Complete = 2,
        Round2HalfWay = 3,
        Round2Complete = 4,
        Round3HalfWay = 5,
        Round3Complete = 6,
    }

    [Header("Beats (en orden)")]
    [SerializeField] private Beat[] beats = new Beat[7];

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float gameStartDelay = 0.5f;

    [Header("First Grab Event")]
    [SerializeField] private FirstGrabDissolveEvent firstGrabDissolveEvent;

    private bool firstGrabDone = false;
    private bool halfwayFired = false;

    // Contador de beats activos — IsPlaying es true mientras haya al menos uno sonando
    private int activeBeatCount = 0;
    public bool IsPlaying => activeBeatCount > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (audioSource == null)
            Debug.LogError("[NarrativeBeat] Falta el AudioSource.");

        for (int i = 0; i < beats.Length; i++)
            if (beats[i] == null || beats[i].clip == null)
                Debug.LogWarning($"[NarrativeBeat] Beat [{i}] ({(BeatIndex)i}) sin clip.");

        StartCoroutine(PlayBeatDelayed(BeatIndex.GameStart, gameStartDelay));
    }

    // ─────────────────────────────────────────
    //  PUBLIC
    // ─────────────────────────────────────────

    public void OnFirstGrab()
    {
        if (firstGrabDone) return;
        firstGrabDone = true;
        PlayBeat(BeatIndex.FirstGrab);
        firstGrabDissolveEvent?.Trigger();
    }

    public void OnRoundStarted(int roundIndex)
    {
        halfwayFired = false;
    }

    public void OnTrashDestroyed(int roundIndex, int remaining, int total)
    {
        if (halfwayFired) return;
        if (roundIndex != 1 && roundIndex != 2) return;

        int destroyed = total - remaining;
        int half = Mathf.CeilToInt(total / 2f);

        if (destroyed >= half)
        {
            halfwayFired = true;
            PlayBeat(roundIndex == 1 ? BeatIndex.Round2HalfWay : BeatIndex.Round3HalfWay);
        }
    }

    public void OnRoundCompleted(int roundIndex)
    {
        switch (roundIndex)
        {
            case 0: PlayBeat(BeatIndex.Round1Complete); break;
            case 1: PlayBeat(BeatIndex.Round2Complete); break;
            case 2: PlayBeat(BeatIndex.Round3Complete); break;
        }
    }

    // ─────────────────────────────────────────
    //  INTERNALS
    // ─────────────────────────────────────────

    private void PlayBeat(BeatIndex index)
    {
        int i = (int)index;

        if (beats == null || i >= beats.Length)
        {
            Debug.LogError($"[NarrativeBeat] Index {i} fuera de rango");
            return;
        }

        Beat beat = beats[i];

        if (beat == null || beat.clip == null)
        {
            Debug.LogWarning($"[NarrativeBeat] Beat [{i}] sin clip — saltando");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[NarrativeBeat] AudioSource null");
            return;
        }

        Debug.Log($"[NarrativeBeat] ▶ [{i}] '{beat.name}'");

        // Usar PlayOneShot para que los beats no se corten entre si
        audioSource.PlayOneShot(beat.clip);
        StartCoroutine(TrackBeatDuration(beat.clip.length));
    }

    
    private IEnumerator TrackBeatDuration(float duration)
    {
        activeBeatCount++;
        yield return new WaitForSeconds(duration);
        activeBeatCount--;
    }

    private IEnumerator PlayBeatDelayed(BeatIndex index, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayBeat(index);
    }
}