using UnityEngine;
using System.Collections;

/// <summary>
/// Maneja los beats narrativos del juego en orden estricto.
/// Se conecta al StageManager para recibir eventos de hitos.
/// </summary>
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
        GameStart = 0,  // Al entrar a la escena
        FirstGrab = 1,  // Al agarrar la pistola por primera vez
        Round1Complete = 2,  // Al completar stage 1
        Round2HalfWay = 3,  // Al destruir mitad de basuras en stage 2
        Round2Complete = 4,  // Al completar stage 2
        Round3HalfWay = 5,  // Al destruir mitad de basuras en stage 3
        Round3Complete = 6,  // Al completar stage 3 → va al menu
    }

    [Header("Beats (en orden)")]
    [SerializeField] private Beat[] beats = new Beat[7];

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float gameStartDelay = 0.5f;

    // Estado
    private bool firstGrabDone = false;
    private bool halfwayFired = false; // se resetea por ronda
    private bool isPlaying = false;

    /// <summary>True mientras se está reproduciendo un beat narrativo.</summary>
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(PlayBeatDelayed(BeatIndex.GameStart, gameStartDelay));
    }

    // ─────────────────────────────────────────
    //  PUBLIC — llamados desde StageManager / TrashGun
    // ─────────────────────────────────────────

    /// <summary>Llamar cuando el jugador agarra el arma por primera vez.</summary>
    public void OnFirstGrab()
    {
        if (firstGrabDone) return;
        firstGrabDone = true;
        PlayBeat(BeatIndex.FirstGrab);
    }

    /// <summary>Llamar desde StageManager cuando comienza una ronda nueva.</summary>
    public void OnRoundStarted(int roundIndex)
    {
        halfwayFired = false;
    }

    /// <summary>Llamar desde StageManager cada vez que se destruye basura.</summary>
    public void OnTrashDestroyed(int roundIndex, int remaining, int total)
    {
        // Halfway solo aplica a ronda 2 y 3 (index 1 y 2)
        if (!halfwayFired && (roundIndex == 1 || roundIndex == 2))
        {
            int half = Mathf.CeilToInt(total / 2f);
            int destroyed = total - remaining;

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

    /// <summary>Llamar desde StageManager al completar una ronda.</summary>
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
        if (beats == null || i >= beats.Length) return;

        Beat beat = beats[i];
        if (beat == null) return;

        Debug.Log($"[NarrativeBeat] ▶ {beat.name}");

        if (audioSource != null && beat.clip != null)
        {
            audioSource.PlayOneShot(beat.clip);
            StartCoroutine(MarkPlayingFor(beat.clip.length));
        }
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