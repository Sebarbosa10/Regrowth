using System.Collections.Generic;
using UnityEngine;


public class TrashDiscoveryManager : MonoBehaviour
{
    public static TrashDiscoveryManager Instance { get; private set; }

    
    public class TrashDiscoveryEntry
    {
        
        public string layerName;

       
        public string tag;

        
        public AudioClip discoveryClip;

        
        public string description;
    }

    [Header("Entradas de descubrimiento")]
    [SerializeField] private List<TrashDiscoveryEntry> discoveryEntries = new List<TrashDiscoveryEntry>();

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float volumeScale = 1f;

    
    private readonly HashSet<string> discovered = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
    }

   
    public void OnTrashCollected(GameObject trashObject)
    {
        if (trashObject == null) return;

        string layerName = LayerMask.LayerToName(trashObject.layer);
        string tag = trashObject.tag;
        string key = $"{layerName}|{tag}";

        if (discovered.Contains(key))
            return; 

        
        discovered.Add(key);

        Debug.Log($"[TrashDiscovery] ¡Primera vez! Layer: '{layerName}' | Tag: '{tag}'");

        
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

    
    public void ResetDiscoveries()
    {
        discovered.Clear();
        Debug.Log("[TrashDiscovery] Descubrimientos reseteados.");
    }

    
    public bool IsDiscovered(string layerName, string tag)
    {
        return discovered.Contains($"{layerName}|{tag}");
    }
}