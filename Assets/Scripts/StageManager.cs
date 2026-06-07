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

    [Header("Teleport Settings")]
    [Tooltip("Offset en Y para compensar el centro del CharacterController / OVR Rig. " +
             "Si caés desde arriba, aumentá este valor. Si quedás enterrado, bajalo.")]
    [SerializeField] private float spawnYOffset = 0f;

    private int currentStage = 0;
    private int trashRemaining = 0;
    private CharacterController characterController;

    private void Awake()
    {
        Instance = this;
        characterController = playerRig.GetComponent<CharacterController>();
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
        // 1. Fade a negro — el jugador todavía está en su posición anterior
        yield return StartCoroutine(fadeController.FadeOut());

        currentStage++;

        if (currentStage >= 3)
        {
            Debug.Log("¡Juego completado!");
            yield break;
        }

        // 2. Pantalla en negro: teleportar y cargar stage
        TeleportPlayer(currentStage);
        LoadStage(currentStage);

        yield return new WaitForSeconds(0.5f);

        // 3. Fade de vuelta — el jugador ya está en la nueva posición
        yield return StartCoroutine(fadeController.FadeIn());
    }

    /// <summary>
    /// Teletransporta el playerRig al spawn point del stage indicado,
    /// deshabilitando el CharacterController para que el warp no sea bloqueado.
    /// </summary>
    private void TeleportPlayer(int stage)
    {
        if (stageSpawnPoints.Length <= stage) return;

        Vector3 targetPosition = stageSpawnPoints[stage].position + Vector3.up * spawnYOffset;

        // Deshabilitar el CC antes de mover para evitar que corrija la posición
        if (characterController != null) characterController.enabled = false;

        playerRig.position = targetPosition;

        if (characterController != null) characterController.enabled = true;
    }

    /// <summary>
    /// Carga el contenido del stage: basura, armas y diálogos.
    /// Ya NO mueve al jugador — eso lo hace TeleportPlayer.
    /// </summary>
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
                if (trash != null) trash.SetActive(true);
            }
        }
    }

    private void DeactivateAllTrash()
    {
        foreach (var t in stage1Trash) if (t != null) t.SetActive(false);
        foreach (var t in stage2Trash) if (t != null) t.SetActive(false);
        foreach (var t in stage3Trash) if (t != null) t.SetActive(false);
    }
}