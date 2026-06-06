using System.Collections;
using UnityEngine;


[System.Serializable]
public class StageData
{
    public string stageName;
    public GameObject[] trashObjects;
    public GameObject[] weaponsToEnable;
    public GameObject[] weaponsToDisable;
}
public class StageManager : MonoBehaviour
{

    public static StageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Stages")]
    [SerializeField] private StageData[] stages;           

    [Header("Stage Zones")]
    [SerializeField] private Transform[] stageSpawnPoints;

    [Header("References")]
    [SerializeField] private Transform playerRig;
    [SerializeField] private FadeController fadeController;

    [Header("Event Channel")]
    [SerializeField] private TrashEventChannel trashChannel; 

    private int _currentStage = 0;
    private int _trashRemaining = 0;
    private void OnEnable()
    {
        if (trashChannel != null)
            trashChannel.OnTrashCollected += HandleTrashCollected;
    }

    private void OnDisable()
    {
        if (trashChannel != null)
            trashChannel.OnTrashCollected -= HandleTrashCollected;
    }

    private void Start()
    {
        LoadStage(0);
    }
    private void HandleTrashCollected()
    {
        _trashRemaining--;
        if (_trashRemaining <= 0)
            StartCoroutine(TransitionToNextStage());
    }

    private IEnumerator TransitionToNextStage()
    {
        yield return StartCoroutine(fadeController.FadeOut());

        _currentStage++;

        if (_currentStage >= stages.Length)
        {
            OnGameCompleted();
            yield break;
        }

        LoadStage(_currentStage);

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(fadeController.FadeIn());
    }

    private void OnGameCompleted()
    {
       
        Debug.Log("¡Juego completado!");
    }


    private void LoadStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length)
        {
            Debug.LogWarning($"StageManager: índice de stage inválido ({stageIndex})");
            return;
        }

        DeactivateAllTrash();

        StageData data = stages[stageIndex];

        ActivateTrash(data);
        ConfigureWeapons(data);
        TeleportPlayer(stageIndex);

        DialogueManager.Instance?.PlayDialoguesForStage(stageIndex);
    }

    private void ActivateTrash(StageData data)
    {
        _trashRemaining = 0;
        if (data.trashObjects == null) return;

        foreach (GameObject trash in data.trashObjects)
        {
            if (trash == null) continue;
            trash.SetActive(true);
            _trashRemaining++;
        }
    }

    private void ConfigureWeapons(StageData data)
    {
        if (data.weaponsToEnable != null)
            foreach (var w in data.weaponsToEnable)
                if (w != null) w.SetActive(true);

        if (data.weaponsToDisable != null)
            foreach (var w in data.weaponsToDisable)
                if (w != null) w.SetActive(false);
    }

    private void TeleportPlayer(int stageIndex)
    {
        if (stageSpawnPoints != null && stageIndex < stageSpawnPoints.Length)
            playerRig.position = stageSpawnPoints[stageIndex].position;
    }

    private void DeactivateAllTrash()
    {
    
        foreach (StageData data in stages)
        {
            if (data.trashObjects == null) continue;
            foreach (GameObject trash in data.trashObjects)
                if (trash != null) trash.SetActive(false);
        }
    }
}