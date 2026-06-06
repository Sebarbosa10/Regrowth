using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrashBullet : MonoBehaviour
{
    private Rigidbody _rb;
    private LayerMask _trashLayer;
    private AudioClip _impactSound;
    private ObjectPool<TrashBullet> _pool;
    private float _lifetime;
    private float _timer;
    private bool _returned; // ← guard contra doble ReturnToPool

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public void Initialize(Vector3 velocity, LayerMask layer, AudioClip impact,
                       float lifetime, ObjectPool<TrashBullet> pool)
    {
        _trashLayer = layer;
        _impactSound = impact;
        _lifetime = lifetime;
        _pool = pool;
        _timer = 0f;
        _returned = false;

      
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = false;

   
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _rb.velocity = velocity; 
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime) ReturnToPool();
    }

    private void OnCollisionEnter(Collision col)
    {
        Debug.Log($"Bullet pegó a: {col.gameObject.name}, layer: {LayerMask.LayerToName(col.gameObject.layer)}");

        if (((1 << col.gameObject.layer) & _trashLayer) != 0)
        {
            Debug.Log($"layer obj: {col.gameObject.layer} | mask value: {_trashLayer.value} | resultado: {(1 << col.gameObject.layer) & _trashLayer.value}");
            AudioSource.PlayClipAtPoint(_impactSound, transform.position);
            col.gameObject.GetComponentInParent<TrashObject>()?.Collect();
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_returned) return;
        _returned = true;

        _rb.collisionDetectionMode = CollisionDetectionMode.Discrete; 
        _rb.isKinematic = true; 
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _pool.Return(this);
    }
}