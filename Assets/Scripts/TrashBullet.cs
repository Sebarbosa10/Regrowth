using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class TrashBullet : MonoBehaviour
{
    private AudioClip impactSound;
    private string targetTag = "MetalGarbage";

    public void SetTargetTag(string tag)
    {
        targetTag = tag;
    }

    public void SetImpactSound(AudioClip clip)
    {
        impactSound = clip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignorar basura de aspiradora
        if (collision.gameObject.CompareTag("VacuumGarbage"))
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        if (collision.gameObject.CompareTag(targetTag))
        {
            if (impactSound != null)
                AudioSource.PlayClipAtPoint(impactSound, transform.position);

            Destroy(collision.gameObject);
        }

        Destroy(gameObject);
    }
}