using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrashBullet : MonoBehaviour
{
    private LayerMask trashLayer;
    private AudioClip impactSound;

    public void SetTrashLayer(LayerMask layer)
    {
        trashLayer = layer;
    }

    public void SetImpactSound(AudioClip clip)
    {
        impactSound = clip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & trashLayer) != 0)
        {
            // Reproducir sonido de impacto en la posición de la basura
            if (impactSound != null)
                AudioSource.PlayClipAtPoint(impactSound, transform.position);

            Destroy(collision.gameObject);
        }

        Destroy(gameObject);
    }
}