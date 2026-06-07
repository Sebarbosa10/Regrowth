using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Stage Zones (posiciones del jugador)")]
    [SerializeField] private Transform[] stageSpawnPoints; 

    [Header("Basura por Stage")]
    [SerializeField] private GameObject[] stage1Trash;
    [SerializeField] private GameObject[] stage2Trash;
    [SerializeField] private GameObject[] stage3Trash;

    [Header("Armas")]
    [SerializeField] private GameObject vacuumGun;
    [SerializeField] private GameObject trashGun;

    [Header("References")]
    [SerializeField] private Transform playerRig; 
    [SerializeField] private FadeController fadeController;

    private int currentStage = 0;
    private int trashRemaining = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadStage(0);
    }

    public void OnTrashDestroyed()
    {
        trashRemaining--;

        if (trashRemaining <= 0)
        {
            StartCoroutine(TransitionToNextStage());
        }
    }

    private IEnumerator TransitionToNextStage()
    {
        
        yield return StartCoroutine(fadeController.FadeOut());

        currentStage++;

        if (currentStage >= 3)
        {
            
            Debug.Log("¡Juego completado!");
            yield break;
        }

        LoadStage(currentStage);

        yield return new WaitForSeconds(0.5f);

        
        yield return StartCoroutine(fadeController.FadeIn());
    }

    private void LoadStage(int stage)
    {
        
        DeactivateAllTrash();

        GameObject[] currentTrash = null;

        switch (stage)
        {
            case 0:
                currentTrash = stage1Trash;
                vacuumGun.SetActive(true);
                trashGun.SetActive(false);
                break;

            case 1:
                currentTrash = stage2Trash;
                vacuumGun.SetActive(true);
                trashGun.SetActive(true);
                break;

            case 2:
                currentTrash = stage3Trash;
                vacuumGun.SetActive(true);
                trashGun.SetActive(true);
                break;
        }

        
        DialogueManager.Instance?.PlayDialoguesForStage(stage);

        
        if (currentTrash != null)
        {
            trashRemaining = currentTrash.Length;
            foreach (GameObject trash in currentTrash)
            {
                trash.SetActive(true);
            }
        }

        
        if (stageSpawnPoints.Length > stage)
        {
            playerRig.position = stageSpawnPoints[stage].position;
        }
    }
    private void DeactivateAllTrash()
    {
        foreach (var t in stage1Trash) if (t != null) t.SetActive(false);
        foreach (var t in stage2Trash) if (t != null) t.SetActive(false);
        foreach (var t in stage3Trash) if (t != null) t.SetActive(false);
    }
}