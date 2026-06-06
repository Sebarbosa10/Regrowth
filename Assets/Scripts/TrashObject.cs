using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrashObject : MonoBehaviour
{
    [SerializeField] private TrashEventChannel eventChannel;

    public void OnHit() { }

    public void Collect()
    {
        eventChannel?.RaiseEvent(); 
        gameObject.SetActive(false); 
    }
}
