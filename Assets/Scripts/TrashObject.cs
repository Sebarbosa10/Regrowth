using UnityEngine;

public class TrashObject : MonoBehaviour
{
    [SerializeField] private TrashEventChannel eventChannel;

    public void Collect()
    {
        eventChannel?.RaiseEvent();
        gameObject.SetActive(false);
    }
}