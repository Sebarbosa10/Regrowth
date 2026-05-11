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

    [Header("Dissolve (opcional - requiere shader con _DissolveAmount)")]
    [SerializeField] private bool _useDissolve = false;       // Activar cuando tengas el shader
    [SerializeField] private float _dissolveDuration = 0.6f;
    [SerializeField] private string _dissolveShaderProperty = "_DissolveAmount";

    [Header("VFX (opcional)")]
    [SerializeField] private ParticleSystem _impactVFX;       // Asignar cuando tengas partículas

    [Header("Audio (opcional)")]
    [SerializeField] private AudioClip _impactClip;

    // Cache
    private Rigidbody _rb;
    private Vector3 _spawnPosition;
    private bool _hasHit;
    private bool _initialized;

    // ?????????????????????????????????????????????
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void FixedUpdate()
    {
        if (!_initialized || _hasHit) return;

        if (Vector3.Distance(_spawnPosition, transform.position) >= _maxTravelDistance)
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

        // Detenemos la bola
        GetComponent<Collider>().enabled = false;
        _rb.velocity = Vector3.zero;
        _rb.isKinematic = true;

        // VFX e audio (solo si están asignados)
        if (collision.contacts.Length > 0)
        {
            TryPlayImpactVFX(collision.contacts[0].point, collision.contacts[0].normal);
            TryPlayImpactAudio();
        }

        // Destrucción: con dissolve si está activado y hay shader, si no instantánea
        if (_useDissolve)
            StartCoroutine(DissolveAndDestroy(collision.gameObject));
        else
            DestroyTargetInstant(collision.gameObject);

        DestroySelf();
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Public API

    public void Initialize(float speed, Collider[] collidersToIgnore)
    {
        _spawnPosition = transform.position;
        _rb.velocity = transform.forward * speed;
        _initialized = true;

        SphereCollider ownCollider = GetComponent<SphereCollider>();
        foreach (Collider col in collidersToIgnore)
        {
            if (col != null)
                Physics.IgnoreCollision(ownCollider, col, true);
        }
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Destroy Modes

    /// <summary>
    /// Destrucción instantánea. Funciona siempre, sin assets extra.
    /// </summary>
    private void DestroyTargetInstant(GameObject target)
    {
        NotifyTrashItem(target);
        Debug.Log($"[PlasmaBall] Destruyendo instantáneamente: {target.name}");
        Destroy(target);
    }

    /// <summary>
    /// Destrucción con efecto dissolve. Requiere shader con propiedad _DissolveAmount.
    /// Activar _useDissolve = true en el Inspector cuando tengas el shader listo.
    /// </summary>
    private IEnumerator DissolveAndDestroy(GameObject target)
    {
        NotifyTrashItem(target);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        // Si no hay renderers o ninguno tiene el shader, destruimos directo
        if (renderers.Length == 0 || !HasDissolveProperty(renderers))
        {
            Debug.LogWarning($"[PlasmaBall] '{target.name}' no tiene shader con '{_dissolveShaderProperty}'. Destruyendo instantáneamente.");
            Destroy(target);
            yield break;
        }

        // Instanciamos materiales para no modificar el asset compartido
        Material[][] instanceMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            instanceMaterials[i] = renderers[i].materials;

        float elapsed = 0f;
        while (elapsed < _dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dissolveDuration);

            foreach (Material[] mats in instanceMaterials)
                foreach (Material mat in mats)
                    if (mat.HasProperty(_dissolveShaderProperty))
                        mat.SetFloat(_dissolveShaderProperty, t);

            yield return null;
        }

        Destroy(target);
    }

    private bool HasDissolveProperty(Renderer[] renderers)
    {
        foreach (Renderer r in renderers)
            foreach (Material mat in r.sharedMaterials)
                if (mat != null && mat.HasProperty(_dissolveShaderProperty))
                    return true;
        return false;
    }

    private void NotifyTrashItem(GameObject target)
    {
        TrashItem trash = target.GetComponent<TrashItem>()
                       ?? target.GetComponentInParent<TrashItem>();
        trash?.OnVacuumed();
    }

    #endregion

    // ?????????????????????????????????????????????
    #region VFX & Audio

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

    #endregion

    // ?????????????????????????????????????????????
    #region Cleanup

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    #endregion
}