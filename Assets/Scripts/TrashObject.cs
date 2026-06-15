using UnityEngine;

public class TrashObject : MonoBehaviour
{
    private void OnDestroy()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnTrashDestroyed();
    }
}
