using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashBullet : MonoBehaviour
{
    private LayerMask trashLayer;
    private AudioClip impactSound;

    // --- MÉTODOS PARA COMPATIBILIDAD (Para que no fallen tus otros scripts) ---
    public void SetTrashLayer(LayerMask layer) => trashLayer = layer;
    public void SetImpactSound(AudioClip clip) => impactSound = clip;

    // --- MÉTODO RECOMENDADO (Para uso futuro más eficiente) ---
    public void Setup(LayerMask layer, AudioClip clip)
    {
        trashLayer = layer;
        impactSound = clip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificamos si la capa del objeto impactado está en nuestra LayerMask
        if ((trashLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            // Reproducir sonido de impacto
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position);
            }

            // Destruir la basura impactada
            Destroy(collision.gameObject);
        }

        // Destruir la bala tras el impacto
        Destroy(gameObject);
    }
}