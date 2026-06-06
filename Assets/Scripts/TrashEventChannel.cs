using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/TrashEventChannel")]
public class TrashEventChannel : ScriptableObject
{
    public event Action OnTrashCollected;

    public void RaiseEvent() => OnTrashCollected?.Invoke();
}