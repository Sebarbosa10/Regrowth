using UnityEngine;

public class TrashObject : MonoBehaviour
{
    [SerializeField] private TrashEventChannel eventChannel;

    public void Collect()
    {
        gameObject.SetActive(false);
        eventChannel?.RaiseEvent();
    }
}