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
        public GameObject[] trashPrefabs;
        public int spawnCount = 10;
    }

    [SerializeField] private RoundConfig[] rounds = new RoundConfig[3];
    [SerializeField] private Transform boxCenter;
    [SerializeField] public Vector3 boxSize = new Vector3(10f, 3f, 10f);
    [SerializeField] private float minPlayerDistance = 1.5f;
    [SerializeField] private Transform playerRig;
    [SerializeField] private FadeController fadeController;
    [SerializeField] private WeaponHolster[] holsters;
    [SerializeField] private GameObject vacuumGun;
    [SerializeField] private GameObject trashGun;
    [SerializeField] private WeaponHolster vacuumHolster;
    [SerializeField] private WeaponHolster trashGunHolster;
    [SerializeField] private float delayBetweenRounds = 0.5f;
    [SerializeField] private int maxSpawnAttempts = 30;
    [SerializeField] private float delayBeforeMainMenu = 1.5f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int currentRound = 0;
    private int trashRemaining = 0;
    private bool transitioning = false;
    private bool roundCleared = false;
    private bool roundStarted = false;

    public int CurrentRound => currentRound;

    private readonly List<GameObject> activeTrash = new List<GameObject>();

    private void Awake() { Instance = this; }

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
        yield return null;
        yield return null;
        ResetWeaponsToHolsters();
        SetWeaponsGrabbable(true);
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

    public void OnTrashDestroyed()
    {
        trashRemaining--;
        RoundHUDDisplay.Instance?.RegisterDestroyed();

        if (trashRemaining <= 0 && !transitioning && roundStarted)
            roundCleared = true;
    }

    private void OnAnyWeaponRemoved()
    {
        if (roundStarted || transitioning) return;

        bool anyRemoved = false;
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null && !holster.IsStored) { anyRemoved = true; break; }
        }
        if (!anyRemoved) return;

        if (currentRound == 0)
            NarrativeBeatManager.Instance?.OnFirstGrab();

        roundStarted = true;
        SpawnCurrentRound();
    }

    private void OnAnyWeaponStored()
    {
        if (!roundCleared || transitioning) return;
        foreach (WeaponHolster holster in holsters)
        {
            if (holster != null && !holster.IsStored) return;
        }
        StartCoroutine(TransitionToNextRound());
    }

    private void SetWeaponsGrabbable(bool state)
    {
        SetWeaponInteractable(vacuumGun, state);
        SetWeaponInteractable(trashGun, state);
    }

    private void SetWeaponInteractable(GameObject weapon, bool state)
    {
        if (weapon == null) return;

        foreach (var interactable in weapon.GetComponentsInChildren<Oculus.Interaction.HandGrab.HandGrabInteractable>(true))
            interactable.enabled = state;
        foreach (var interactable in weapon.GetComponentsInChildren<Oculus.Interaction.GrabInteractable>(true))
            interactable.enabled = state;
        foreach (var interactable in weapon.GetComponentsInChildren<Oculus.Interaction.Grabbable>(true))
            interactable.enabled = state;
    }

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

    private IEnumerator TransitionToNextRound()
    {
        transitioning = true;
        roundCleared = false;
        roundStarted = false;

        SetWeaponsGrabbable(false);
        vacuumHolster?.Lock();
        trashGunHolster?.Lock();

        NarrativeBeatManager.Instance?.OnRoundCompleted(currentRound);

        yield return new WaitUntil(() =>
            NarrativeBeatManager.Instance == null || !NarrativeBeatManager.Instance.IsPlaying);

        yield return StartCoroutine(fadeController.FadeOut());

        DestroyActiveTrash();
        yield return null;

        ResetWeaponsToHolsters();
        yield return null;

        currentRound++;

        if (currentRound >= rounds.Length)
        {
            yield return new WaitForSeconds(delayBeforeMainMenu);
            SceneManager.LoadScene(mainMenuSceneName);
            yield break;
        }

        LoadRound(currentRound);
        yield return new WaitForSeconds(delayBetweenRounds);
        yield return StartCoroutine(fadeController.FadeIn());

        vacuumHolster?.Unlock();
        trashGunHolster?.Unlock();
        SetWeaponsGrabbable(true);

        transitioning = false;
    }

    private void LoadRound(int roundIndex)
    {
        if (roundIndex >= rounds.Length) return;
        roundStarted = false;
        activeTrash.Clear();
        DialogueManager.Instance?.PlayDialoguesForStage(roundIndex);
        NarrativeBeatManager.Instance?.OnRoundStarted(roundIndex);
    }

    private void SpawnCurrentRound()
    {
        if (currentRound >= rounds.Length) return;

        RoundConfig config = rounds[currentRound];
        activeTrash.Clear();

        int spawned = SpawnTrash(config);
        trashRemaining = spawned;

        RoundHUDDisplay.Instance?.SetRoundTotal(spawned);
    }

    private int SpawnTrash(RoundConfig config)
    {
        if (config.trashPrefabs == null || config.trashPrefabs.Length == 0) return 0;

        Vector3 center = boxCenter != null ? boxCenter.position : transform.position;
        int spawned = 0;

        for (int i = 0; i < config.spawnCount; i++)
        {
            if (!TryGetSpawnPosition(center, out Vector3 spawnPos)) continue;

            GameObject prefab = config.trashPrefabs[Random.Range(0, config.trashPrefabs.Length)];
            if (prefab == null) continue;

            activeTrash.Add(Instantiate(prefab, spawnPos, Random.rotation));
            spawned++;
        }

        return spawned;
    }

    private bool TryGetSpawnPosition(Vector3 center, out Vector3 result)
    {
        Vector3 playerPos = playerRig != null ? playerRig.position : Vector3.zero;
        Vector3 half = boxSize * 0.5f;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 candidate = center + new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z)
            );

            if (Vector3.Distance(candidate, playerPos) >= minPlayerDistance)
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private void DestroyActiveTrash()
    {
        foreach (GameObject obj in activeTrash)
            if (obj != null) Destroy(obj);
        activeTrash.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = boxCenter != null ? boxCenter.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.1f);
        Gizmos.DrawCube(center, boxSize);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(center, boxSize);

        Vector3 playerPos = playerRig != null ? playerRig.position : center;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawSphere(playerPos, minPlayerDistance);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(playerPos, minPlayerDistance);
    }
}
