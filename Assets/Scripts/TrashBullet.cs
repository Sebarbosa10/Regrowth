using System.Collections;
using System.Collections.Generic;
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

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public void Initialize(Vector3 velocity, LayerMask layer, AudioClip impact,
                           float lifetime, ObjectPool<TrashBullet> pool)
    {
        _rb.velocity = velocity;
        _trashLayer = layer;
        _impactSound = impact;
        _lifetime = lifetime;
        _pool = pool;
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime) ReturnToPool();
    }

    private void OnCollisionEnter(Collision col)
    {
        if (((1 << col.gameObject.layer) & _trashLayer) != 0)
        {
            AudioSource.PlayClipAtPoint(_impactSound, transform.position);
            col.gameObject.GetComponent<TrashObject>()?.OnHit();
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _rb.velocity = Vector3.zero;
        _pool.Return(this);
    }
}