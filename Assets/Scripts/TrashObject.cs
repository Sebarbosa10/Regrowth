using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashObject : MonoBehaviour
{
    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnTrashDestroyed();
        }
    }
}