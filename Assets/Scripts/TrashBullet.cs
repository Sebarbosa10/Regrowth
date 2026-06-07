using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashBullet : MonoBehaviour
{
    private AudioClip impactSound;
    private LayerMask shootableLayer;

    public void SetShootableLayer(LayerMask layer)
    {
        shootableLayer = layer;
    }

    public void SetImpactSound(AudioClip clip)
    {
        impactSound = clip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (((1 << collision.gameObject.layer) & shootableLayer) == 0)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position);

        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
}