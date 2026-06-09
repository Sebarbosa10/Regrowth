using UnityEngine;

public class TrashBullet : MonoBehaviour
{
    private AudioClip impactSound;
    private string targetTag;

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
        
        if (string.IsNullOrEmpty(targetTag))
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        
        if (!collision.gameObject.CompareTag(targetTag))
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        
        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position);

        //if (TrashDiscoveryManager.Instance != null)
        //    TrashDiscoveryManager.Instance.OnTrashCollected(collision.gameObject);

        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
}