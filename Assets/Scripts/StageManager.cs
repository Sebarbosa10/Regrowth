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

    [Header("Holsters")]
    [SerializeField] private WeaponHolster[] holsters;

    [Header("Weapons")]
    [SerializeField] private GameObject vacuumGun;
    [SerializeField] private GameObject trashGun;
    [SerializeField] private WeaponHolster vacuumHolster;
    [SerializeField] private WeaponHolster trashGunHolster;

    [Header("Settings")]
    [SerializeField] private float delayBetweenRounds = 0.5f;
    [SerializeField] private int maxSpawnAttempts = 30;
    [SerializeField] private float delayBeforeMainMenu = 1.5f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int currentRound = 0;
    private int trashRemaining = 0;
    private bool transitioning = false;
    private bool roundCleared = false;
    private bool roundStarted = false;

    public int CurrentRound => currentRound;

    private readonly List<GameObject> activeTrash = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null)
            {
                holster.OnWeaponStored += OnAnyWeaponStored;
                holster.OnWeaponRemoved += OnAnyWeaponRemoved;
            }
        }

        StartCoroutine(InitWithDelay());
    }

    private IEnumerator InitWithDelay()
    {
        // Esperar dos frames para que todo esté inicializado
        yield return null;
        yield return null;

        ResetWeaponsToHolsters();
        LoadRound(0);
    }

    private void OnDestroy()
    {
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null)
            {
                holster.OnWeaponStored -= OnAnyWeaponStored;
                holster.OnWeaponRemoved -= OnAnyWeaponRemoved;
            }
        }
    }

    // ─────────────────────────────────────────
    //  PUBLIC — llamado por TrashObject
    // ─────────────────────────────────────────

    public void OnTrashDestroyed()
    {
        trashRemaining--;

        RoundHUDDisplay.Instance?.RegisterDestroyed();

        if (trashRemaining <= 0 && !transitioning && roundStarted)
        {
            roundCleared = true;
            Debug.Log("[StageManager] ¡Basura limpia! Guardá el arma en el holster para continuar.");
        }
    }

    // ─────────────────────────────────────────
    //  HOLSTER EVENTS
    // ─────────────────────────────────────────

    private void OnAnyWeaponRemoved()
    {
        if (roundStarted || transitioning) return;

        bool anyRemoved = false;
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null && !holster.IsStored)
            {
                anyRemoved = true;
                break;
            }
        }

        if (!anyRemoved) return;

        roundStarted = true;
        SpawnCurrentRound();
        Debug.Log("[StageManager] Arma sacada — spawneando basura!");
    }

    private void OnAnyWeaponStored()
    {
        if (!roundCleared || transitioning) return;

        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null && !holster.IsStored)
                return;
        }

        StartCoroutine(TransitionToNextRound());
    }

    // ─────────────────────────────────────────
    //  WEAPONS
    // ─────────────────────────────────────────

    private void ResetWeaponsToHolsters()
    {
        if (vacuumGun != null && vacuumHolster != null)
        {
            vacuumGun.transform.SetParent(null);
            vacuumHolster.ForceStore(vacuumGun);
        }

        if (trashGun != null && trashGunHolster != null)
        {
            trashGun.transform.SetParent(null);
            trashGunHolster.ForceStore(trashGun);
        }
    }

    // ─────────────────────────────────────────
    //  TRANSITION
    // ─────────────────────────────────────────

    private IEnumerator TransitionToNextRound()
    {
        transitioning = true;
        roundCleared = false;
        roundStarted = false;

        yield return StartCoroutine(fadeController.FadeOut());

        DestroyActiveTrash();

        // Esperar un frame para que todo se limpie
        yield return null;

        ResetWeaponsToHolsters();

        // Esperar otro frame para que el holster procese
        yield return null;

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

        roundStarted = false;
        activeTrash.Clear();

        DialogueManager.Instance?.PlayDialoguesForStage(roundIndex);

        Debug.Log($"[StageManager] Ronda {roundIndex + 1} cargada — sacá el arma para empezar");
    }

    // ─────────────────────────────────────────
    //  SPAWN
    // ─────────────────────────────────────────

    private void SpawnCurrentRound()
    {
        if (currentRound >= rounds.Length) return;

        RoundConfig config = rounds[currentRound];
        activeTrash.Clear();

        int spawned = SpawnTrash(config);
        trashRemaining = spawned;

        RoundHUDDisplay.Instance?.SetRoundTotal(spawned);

        Debug.Log($"[StageManager] Ronda {currentRound + 1} — {spawned} objetos spawneados");
    }

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