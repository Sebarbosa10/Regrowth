using UnityEngine;
using System.Collections;

public class NarrativeBeatManager : MonoBehaviour
{
    public static NarrativeBeatManager Instance { get; private set; }

    [System.Serializable]
    public class SubtitleBlock
    {
        [TextArea(1, 3)] public string text;
        public float duration = 2f;
        public float delay = 0f;
    }

    [System.Serializable]
    public class Beat
    {
        public string name;
        public AudioClip clip;
        public SubtitleBlock[] subtitles;
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
    [Tooltip("Velocidad de los beats: acelera el audio y acorta los tiempos de los subtitulos con el mismo factor para que sigan sincronizados. Por encima de ~1.15 la voz se nota mas aguda")]
    [SerializeField, Range(0.5f, 2f)] private float narrationSpeed = 1.15f;

    [Header("Saltar narracion (joystick de cualquier mando)")]
    [Tooltip("Derecha = siguiente bloque, izquierda = bloque anterior, mantener derecha = saltar el beat")]
    [SerializeField] private bool allowSkipping = true;
    [Tooltip("Segundos manteniendo el joystick a la derecha para saltar el beat entero")]
    [SerializeField] private float holdToSkipBeatTime = 1f;
    [SerializeField, Range(0.2f, 0.95f)] private float stickThreshold = 0.6f;
    [Tooltip("Indicador del joystick bajo los subtitulos")]
    [SerializeField] private bool showSkipHint = true;
    [SerializeField] private Color hintAccentColor = new Color(0.1f, 0.9f, 1f, 1f);
    [Tooltip("Diametro del icono en tamanos de letra del subtitulo")]
    [SerializeField] private float hintSize = 2.2f;
    [Tooltip("Hueco entre el subtitulo mas alto y el icono, en tamanos de letra")]
    [SerializeField] private float hintGap = 0.35f;

    // Avisa en consola si los subtitulos de un beat no cuadran con su audio (mas de esto, en segundos)
    private const float TimingWarningThreshold = 1.5f;

    private Coroutine beatCoroutine;
    private bool isBeatActive = false;
    private BeatSkipHint skipHint;
    private int stickDirection = 0; // -1 izquierda, 0 centro, 1 derecha
    private float holdTimer = 0f;

    // El beat dura hasta que terminan el audio y el ultimo subtitulo, o hasta que se salta
    public bool IsPlaying => isBeatActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (beatIndicatorObject != null)
            beatIndicatorObject.SetActive(false);

        if (showSkipHint && allowSkipping && SubtitleDisplay.Instance != null && SubtitleDisplay.Instance.Text != null)
            skipHint = BeatSkipHint.Create(SubtitleDisplay.Instance.Text, hintAccentColor, hintSize, hintGap, AllSubtitleTexts());

        StartCoroutine(PlayBeatThenDissolve(BeatIndex.GameStart, gameStartDelay));
    }

    public void OnFirstGrab()
    {
        // La sala de simulacion ya se disuelve al terminar el texto de GameStart
        // (PlayBeatThenDissolve). Si se disparara aqui otra vez, volveria a aparecer.
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

    private IEnumerator PlayBeatThenDissolve(BeatIndex index, float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        PlayBeat(index);
        yield return new WaitWhile(() => IsPlaying);
        firstGrabDissolveEvent?.Trigger();

        if (index == BeatIndex.GameStart)
            TutorialManager.Instance?.OnIntroFinished();
    }

    private void PlayBeat(BeatIndex index)
    {
        int i = (int)index;
        if (beats == null || i >= beats.Length) return;

        Beat beat = beats[i];
        if (beat == null || beat.clip == null || audioSource == null) return;

        if (beatCoroutine != null)
        {
            StopCoroutine(beatCoroutine);
            EndBeat();
        }
        beatCoroutine = StartCoroutine(RunBeat(beat));
    }

    // ─────────────────────────────────────────
    //  REPRODUCCION DE UN BEAT
    // ─────────────────────────────────────────

    // Todo el beat se mide en segundos del clip (velocidad normal): los tiempos del Inspector
    // estan escritos asi, y el pitch hace que pasen narrationSpeed veces mas rapido.
    private IEnumerator RunBeat(Beat beat)
    {
        isBeatActive = true;
        if (beatIndicatorObject != null) beatIndicatorObject.SetActive(true);

        SubtitleBlock[] blocks = beat.subtitles ?? System.Array.Empty<SubtitleBlock>();
        int count = blocks.Length;

        // Momento del clip en el que empieza cada bloque (retraso + duraciones anteriores)
        var starts = new float[count];
        float cursor = 0f;
        for (int b = 0; b < count; b++)
        {
            SubtitleBlock block = blocks[b];
            starts[b] = cursor + (block != null ? Mathf.Max(0f, block.delay) : 0f);
            cursor = starts[b] + (block != null ? Mathf.Max(0f, block.duration) : 0f);
        }

        if (count > 0 && Mathf.Abs(cursor - beat.clip.length) > TimingWarningThreshold)
        {
            Debug.LogWarning($"[NarrativeBeatManager] Beat '{beat.name}': los subtitulos suman {cursor:F1}s y el audio dura " +
                             $"{beat.clip.length:F1}s. Al saltar con el joystick la voz no caera justo al inicio de cada frase.");
        }

        float total = Mathf.Max(cursor, beat.clip.length);

        audioSource.pitch = narrationSpeed;
        audioSource.clip = beat.clip;
        audioSource.time = 0f;
        audioSource.Play();

        float position = 0f;
        int current = -1;
        // Si el joystick ya estaba empujado al empezar, no cuenta como pulsacion
        stickDirection = ReadStickDirection();
        holdTimer = 0f;
        if (skipHint != null) skipHint.Show(true);

        while (position < total)
        {
            float deltaTime = Time.deltaTime; // 0 durante la pausa del tutorial

            // ─── Joystick ───
            int target = -1;
            bool skipBeat = false;
            bool paused = TutorialManager.Instance != null && TutorialManager.Instance.IsPaused;

            if (allowSkipping && !paused)
            {
                int direction = ReadStickDirection();
                if (direction != stickDirection)
                {
                    stickDirection = direction;
                    holdTimer = 0f;
                    if (direction == 1) target = current + 1;
                    else if (direction == -1 && current >= 0) target = Mathf.Max(current - 1, 0);
                }
                else if (direction == 1)
                {
                    holdTimer += deltaTime;
                    skipBeat = holdTimer >= holdToSkipBeatTime;
                }

                if (skipHint != null)
                    skipHint.SetHoldProgress(stickDirection == 1 && holdToSkipBeatTime > 0f ? holdTimer / holdToSkipBeatTime : 0f);
            }

            if (skipBeat) break;

            if (target >= 0)
            {
                // Pasar del ultimo bloque (o un beat sin subtitulos) termina el beat
                if (target >= count) break;

                position = starts[target];
                SeekAudio(position, beat.clip);
                ShowBlock(blocks[target]);
                current = target;
            }

            // ─── Avance normal ───
            position = audioSource.isPlaying ? audioSource.time : position + deltaTime * narrationSpeed;

            while (current + 1 < count && position >= starts[current + 1])
            {
                current++;
                ShowBlock(blocks[current]);
            }

            if (skipHint != null) skipHint.Animate(Time.time);
            yield return null;
        }

        audioSource.Stop();
        EndBeat();
    }

    private void ShowBlock(SubtitleBlock block)
    {
        if (block == null || string.IsNullOrEmpty(block.text)) return;

        SubtitleDisplay.Instance?.Show(block.text, block.duration / narrationSpeed, narrationSpeed);
    }

    private void SeekAudio(float clipTime, AudioClip clip)
    {
        // Si el bloque empieza despues de que acabe la voz (subtitulos mas largos que el audio) no se oye nada
        if (clipTime >= clip.length - 0.05f)
        {
            audioSource.Stop();
            return;
        }

        if (!audioSource.isPlaying) audioSource.Play();
        audioSource.time = clipTime;
    }

    private void EndBeat()
    {
        isBeatActive = false;
        beatCoroutine = null;
        holdTimer = 0f;
        SubtitleDisplay.Instance?.Hide();
        if (skipHint != null) skipHint.Show(false);
        if (beatIndicatorObject != null) beatIndicatorObject.SetActive(false);
    }

    private System.Collections.Generic.IEnumerable<string> AllSubtitleTexts()
    {
        if (beats == null) yield break;
        foreach (Beat beat in beats)
        {
            if (beat?.subtitles == null) continue;
            foreach (SubtitleBlock block in beat.subtitles)
                if (block != null) yield return block.text;
        }
    }

    // Joystick horizontal de cualquiera de los dos mandos (el que este mas empujado).
    // Para soltar hay que volver por debajo de la mitad del umbral: evita pulsaciones dobles.
    private int ReadStickDirection()
    {
        float left = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).x;
        float right = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
        float x = Mathf.Abs(left) > Mathf.Abs(right) ? left : right;

        float threshold = stickDirection == 0 ? stickThreshold : stickThreshold * 0.5f;
        if (x > threshold) return 1;
        if (x < -threshold) return -1;
        return 0;
    }
}
