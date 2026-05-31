using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrashObject : MonoBehaviour
{
    private void OnDestroy()
    {
        // Avisa al StageManager cuando se destruye
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnTrashDestroyed();
        }
    }
}
