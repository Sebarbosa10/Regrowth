using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrashBullet : MonoBehaviour
{
    private LayerMask trashLayer;

    public void SetTrashLayer(LayerMask layer)
    {
        trashLayer = layer;
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (((1 << collision.gameObject.layer) & trashLayer) != 0)
        {
            Destroy(collision.gameObject); 
        }

        Destroy(gameObject); 
    }
}