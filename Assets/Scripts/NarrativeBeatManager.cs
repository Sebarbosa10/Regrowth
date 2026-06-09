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

    private bool firstGrabDone = false;
    private bool halfwayFired = false;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[NarrativeBeat] Awake — Instance registrada");
    }

    private void Start()
    {
        // Validar audio source
        if (audioSource == null)
            Debug.LogError("[NarrativeBeat] ¡Falta el AudioSource! Asignalo en el Inspector.");

        // Validar beats
        for (int i = 0; i < beats.Length; i++)
        {
            if (beats[i] == null || beats[i].clip == null)
                Debug.LogWarning($"[NarrativeBeat] Beat [{i}] ({(BeatIndex)i}) no tiene clip asignado.");
        }

        StartCoroutine(PlayBeatDelayed(BeatIndex.GameStart, gameStartDelay));
    }

    // ─────────────────────────────────────────
    //  PUBLIC
    // ─────────────────────────────────────────

    public void OnFirstGrab()
    {
        Debug.Log("[NarrativeBeat] OnFirstGrab llamado");
        if (firstGrabDone) return;
        firstGrabDone = true;
        PlayBeat(BeatIndex.FirstGrab);
    }

    public void OnRoundStarted(int roundIndex)
    {
        Debug.Log($"[NarrativeBeat] OnRoundStarted ronda {roundIndex}");
        halfwayFired = false;
    }

    public void OnTrashDestroyed(int roundIndex, int remaining, int total)
    {
        if (!halfwayFired && (roundIndex == 1 || roundIndex == 2))
        {
            int half = Mathf.CeilToInt(total / 2f);
            int destroyed = total - remaining;

            Debug.Log($"[NarrativeBeat] OnTrashDestroyed — ronda {roundIndex}, destruidos {destroyed}/{total}, half={half}");

            if (destroyed >= half)
            {
                halfwayFired = true;
                BeatIndex beat = roundIndex == 1
                    ? BeatIndex.Round2HalfWay
                    : BeatIndex.Round3HalfWay;
                PlayBeat(beat);
            }
        }
    }

    public void OnRoundCompleted(int roundIndex)
    {
        Debug.Log($"[NarrativeBeat] OnRoundCompleted ronda {roundIndex}");
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
            Debug.LogError($"[NarrativeBeat] beats array null o index {i} fuera de rango (length={beats?.Length})");
            return;
        }

        Beat beat = beats[i];
        if (beat == null)
        {
            Debug.LogError($"[NarrativeBeat] Beat [{i}] es null");
            return;
        }

        Debug.Log($"[NarrativeBeat] ▶ Reproduciendo beat [{i}]: '{beat.name}'");

        if (audioSource == null)
        {
            Debug.LogError("[NarrativeBeat] AudioSource es null — no se puede reproducir");
            return;
        }

        if (beat.clip == null)
        {
            Debug.LogWarning($"[NarrativeBeat] Beat '{beat.name}' no tiene AudioClip asignado");
            return;
        }

        audioSource.PlayOneShot(beat.clip);
        StartCoroutine(MarkPlayingFor(beat.clip.length));
    }

    private IEnumerator MarkPlayingFor(float duration)
    {
        isPlaying = true;
        yield return new WaitForSeconds(duration);
        isPlaying = false;
    }

    private IEnumerator PlayBeatDelayed(BeatIndex index, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayBeat(index);
    }
}