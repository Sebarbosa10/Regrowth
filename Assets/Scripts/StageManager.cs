using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [System.Serializable]
    public class RoundConfig
    {
        public string roundName;
        [Tooltip("Prefabs de basura a spawnear en esta ronda")]
        public GameObject[] trashPrefabs;
        [Tooltip("Cuantos objetos spawnear en total")]
        public int spawnCount = 10;
    }

    [Header("Rounds")]
    [SerializeField] private RoundConfig[] rounds = new RoundConfig[3];

    [Header("Spawn Sphere")]
    [SerializeField] private Transform sphereCenter;
    [SerializeField] private float sphereRadius = 5f;
    [SerializeField] private float minPlayerDistance = 1.5f;

    [Header("References")]
    [SerializeField] private Transform playerRig;
    [SerializeField] private FadeController fadeController;

    [Header("Settings")]
    [SerializeField] private float delayBetweenRounds = 0.5f;
    [SerializeField] private int maxSpawnAttempts = 30;
    [SerializeField] private float delayBeforeMainMenu = 1.5f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int currentRound = 0;
    private int trashRemaining = 0;
    private bool transitioning = false;

    private readonly List<GameObject> activeTrash = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadRound(0);
    }

    // ─────────────────────────────────────────
    //  PUBLIC — llamado por TrashObject
    // ─────────────────────────────────────────

    public void OnTrashDestroyed()
    {
        trashRemaining--;

        // Notificar al HUD
        RoundHUDDisplay.Instance?.RegisterDestroyed();

        if (trashRemaining <= 0 && !transitioning)
            StartCoroutine(TransitionToNextRound());
    }

    // ─────────────────────────────────────────
    //  TRANSITION
    // ─────────────────────────────────────────

    private IEnumerator TransitionToNextRound()
    {
        transitioning = true;

        yield return StartCoroutine(fadeController.FadeOut());

        DestroyActiveTrash();

        currentRound++;

        if (currentRound >= rounds.Length)
        {
            Debug.Log("[StageManager] ¡Juego completado! Volviendo al main menu...");
            yield return new WaitForSeconds(delayBeforeMainMenu);
            SceneManager.LoadScene(mainMenuSceneName);
            yield break;
        }

        LoadRound(currentRound);

        yield return new WaitForSeconds(delayBetweenRounds);

        yield return StartCoroutine(fadeController.FadeIn());

        transitioning = false;
    }

    // ─────────────────────────────────────────
    //  LOAD ROUND
    // ─────────────────────────────────────────

    private void LoadRound(int roundIndex)
    {
        if (roundIndex >= rounds.Length) return;

        RoundConfig config = rounds[roundIndex];

        activeTrash.Clear();

        // Dialogos
        DialogueManager.Instance?.PlayDialoguesForStage(roundIndex);

        // Spawn
        int spawned = SpawnTrash(config);
        trashRemaining = spawned;

        // Notificar al HUD con el total de esta ronda
        RoundHUDDisplay.Instance?.SetRoundTotal(spawned);

        Debug.Log($"[StageManager] Ronda {roundIndex + 1} — {spawned} objetos spawneados");
    }

    // ─────────────────────────────────────────
    //  SPAWN LOGIC
    // ─────────────────────────────────────────

    private int SpawnTrash(RoundConfig config)
    {
        if (config.trashPrefabs == null || config.trashPrefabs.Length == 0)
        {
            Debug.LogWarning("[StageManager] La ronda no tiene prefabs asignados.");
            return 0;
        }

        Vector3 center = sphereCenter != null ? sphereCenter.position : transform.position;
        int spawned = 0;

        for (int i = 0; i < config.spawnCount; i++)
        {
            Vector3 spawnPos;
            bool found = TryGetSpawnPosition(center, out spawnPos);

            if (!found)
            {
                Debug.LogWarning($"[StageManager] No se encontro posicion valida para objeto {i}");
                continue;
            }

            GameObject prefab = config.trashPrefabs[Random.Range(0, config.trashPrefabs.Length)];
            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, spawnPos, Random.rotation);
            activeTrash.Add(obj);
            spawned++;
        }

        return spawned;
    }

    private bool TryGetSpawnPosition(Vector3 center, out Vector3 result)
    {
        Vector3 playerPos = playerRig != null ? playerRig.position : Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 candidate = center + Random.insideUnitSphere * sphereRadius;

            float distToPlayer = Vector3.Distance(candidate, playerPos);
            if (distToPlayer < minPlayerDistance)
                continue;

            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // ─────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────

    private void DestroyActiveTrash()
    {
        foreach (GameObject obj in activeTrash)
        {
            if (obj != null) Destroy(obj);
        }
        activeTrash.Clear();
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 center = sphereCenter != null ? sphereCenter.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawSphere(center, sphereRadius);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(center, sphereRadius);

        Vector3 playerPos = playerRig != null ? playerRig.position : center;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawSphere(playerPos, minPlayerDistance);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(playerPos, minPlayerDistance);
    }
}