using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PlasmaBall : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float _maxTravelDistance = 30f;

    [Header("Impacto")]
    [SerializeField] private LayerMask _metalLayer;

    [Header("Dissolve (requiere shader con _DissolveAmount)")]
    [SerializeField] private bool _useDissolve = false;
    [SerializeField] private float _dissolveDuration = 0.6f;
    [SerializeField] private string _dissolveShaderProperty = "_DissolveAmount";

    [Header("VFX (opcional)")]
    [SerializeField] private ParticleSystem _impactVFX;

    [Header("Audio (opcional)")]
    [SerializeField] private AudioClip _impactClip;

    
    private Rigidbody _rb;
    private SphereCollider _col;
    private Vector3 _spawnPosition;
    private float _maxTravelDistanceSqr;    
    private bool _hasHit;
    private bool _initialized;

    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<SphereCollider>();

        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        
        _maxTravelDistanceSqr = _maxTravelDistance * _maxTravelDistance;
    }

    private void FixedUpdate()
    {
        if (!_initialized || _hasHit) return;

        
        if ((_spawnPosition - transform.position).sqrMagnitude >= _maxTravelDistanceSqr)
            DestroySelf();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;

        int hitLayer = collision.gameObject.layer;
        bool isMetal = (_metalLayer.value & (1 << hitLayer)) != 0;

        Debug.Log($"[PlasmaBall] Golpeó: '{collision.gameObject.name}' | Layer: '{LayerMask.LayerToName(hitLayer)}' | Es metal: {isMetal}");

        if (!isMetal)
        {
            DestroySelf();
            return;
        }

        _hasHit = true;
        _col.enabled = false;           
        _rb.velocity = Vector3.zero;
        _rb.isKinematic = true;

        if (collision.contacts.Length > 0)
        {
            TryPlayImpactVFX(collision.contacts[0].point, collision.contacts[0].normal);
            TryPlayImpactAudio();
        }

        if (_useDissolve)
            StartCoroutine(DissolveAndDestroy(collision.gameObject));
        else
            DestroyTargetInstant(collision.gameObject);

        DestroySelf();
    }

  
    public void Initialize(float speed, Collider[] collidersToIgnore)
    {
        _spawnPosition = transform.position;
        _rb.velocity = transform.forward * speed;
        _initialized = true;

        
        int count = collidersToIgnore.Length;
        for (int i = 0; i < count; i++)
        {
            if (collidersToIgnore[i] != null)
                Physics.IgnoreCollision(_col, collidersToIgnore[i], true);
        }
    }

   

    private void DestroyTargetInstant(GameObject target)
    {
        NotifyTrashItem(target);
        Destroy(target);
    }

    private IEnumerator DissolveAndDestroy(GameObject target)
    {
        NotifyTrashItem(target);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0 || !HasDissolveProperty(renderers))
        {
            Debug.LogWarning($"[PlasmaBall] Sin shader dissolve en '{target.name}'. Destrucción instantánea.");
            Destroy(target);
            yield break;
        }

        
        Material[][] instanceMats = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            instanceMats[i] = renderers[i].materials;

        float elapsed = 0f;
        while (elapsed < _dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dissolveDuration);

            for (int i = 0; i < instanceMats.Length; i++)
            {
                Material[] mats = instanceMats[i];
                for (int j = 0; j < mats.Length; j++)
                    if (mats[j].HasProperty(_dissolveShaderProperty))
                        mats[j].SetFloat(_dissolveShaderProperty, t);
            }

            yield return null;
        }

        Destroy(target);
    }

    private bool HasDissolveProperty(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
                if (mats[j] != null && mats[j].HasProperty(_dissolveShaderProperty))
                    return true;
        }
        return false;
    }

    private void NotifyTrashItem(GameObject target)
    {
        TrashItem trash = target.GetComponent<TrashItem>()
                       ?? target.GetComponentInParent<TrashItem>();
        trash?.OnVacuumed();
    }

    

    private void TryPlayImpactVFX(Vector3 position, Vector3 normal)
    {
        if (_impactVFX == null) return;
        _impactVFX.transform.SetParent(null);
        _impactVFX.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
        _impactVFX.Play();
        Destroy(_impactVFX.gameObject, _impactVFX.main.duration + _impactVFX.main.startLifetime.constantMax);
    }

    private void TryPlayImpactAudio()
    {
        if (_impactClip == null) return;
        AudioSource.PlayClipAtPoint(_impactClip, transform.position);
    }

    

    private void DestroySelf() => Destroy(gameObject);

    
}