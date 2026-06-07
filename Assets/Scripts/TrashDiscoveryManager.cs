using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton que trackea qué combinaciones de Layer+Tag de basura
/// ya fueron descubiertas por primera vez. Al descubrir una nueva,
/// dispara un audio configurable.
/// </summary>
public class TrashDiscoveryManager : MonoBehaviour
{
    public static TrashDiscoveryManager Instance { get; private set; }

    [System.Serializable]
    public class TrashDiscoveryEntry
    {
        [Tooltip("Nombre del layer (ej: 'Trash', 'Glass', 'Organic')")]
        public string layerName;

        [Tooltip("Tag del objeto (ej: 'Plastic', 'Glass', 'Organic')")]
        public string tag;

        [Tooltip("Audio a reproducir la primera vez que se recolecta este tipo")]
        public AudioClip discoveryClip;

        [Tooltip("(Opcional) Descripción para identificarlo en el Inspector")]
        public string description;
    }

    [Header("Entradas de descubrimiento")]
    [SerializeField] private List<TrashDiscoveryEntry> discoveryEntries = new List<TrashDiscoveryEntry>();

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float volumeScale = 1f;

    // Clave interna: "layerName|tag"
    private readonly HashSet<string> discovered = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Opcional: DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Llamar este método cada vez que se destruye/recoge un objeto de basura.
    /// Pasa el GameObject antes de destruirlo para leer su layer y tag.
    /// </summary>
    public void OnTrashCollected(GameObject trashObject)
    {
        if (trashObject == null) return;

        string layerName = LayerMask.LayerToName(trashObject.layer);
        string tag = trashObject.tag;
        string key = $"{layerName}|{tag}";

        if (discovered.Contains(key))
            return; // Ya fue descubierto antes, no hacer nada

        // Primera vez que encontramos este tipo
        discovered.Add(key);

        Debug.Log($"[TrashDiscovery] ¡Primera vez! Layer: '{layerName}' | Tag: '{tag}'");

        // Buscar la entrada configurada para esta combinación
        TrashDiscoveryEntry entry = discoveryEntries.Find(e =>
            e.layerName == layerName && e.tag == tag);

        if (entry != null && entry.discoveryClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(entry.discoveryClip, volumeScale);
        }
        else if (entry == null)
        {
            Debug.LogWarning($"[TrashDiscovery] No hay entrada configurada para Layer='{layerName}' Tag='{tag}'. " +
                             $"Agregala en el Inspector del TrashDiscoveryManager.");
        }
    }

    /// <summary>
    /// Resetea todos los descubrimientos (útil al reiniciar el juego/stage).
    /// </summary>
    public void ResetDiscoveries()
    {
        discovered.Clear();
        Debug.Log("[TrashDiscovery] Descubrimientos reseteados.");
    }

    /// <summary>
    /// Consulta si un tipo ya fue descubierto sin disparar audio.
    /// </summary>
    public bool IsDiscovered(string layerName, string tag)
    {
        return discovered.Contains($"{layerName}|{tag}");
    }
}