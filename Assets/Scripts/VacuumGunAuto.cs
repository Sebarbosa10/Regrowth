using UnityEngine;
using Oculus.Interaction;


public class VacuumGunAuto : MonoBehaviour
{
   
    [SerializeField] private Grabbable _grabbable;
    [SerializeField] private Transform _suctionPoint;

    
    [SerializeField] private float _suctionRadius = 3f;
    [SerializeField] private float _suctionForce = 20f;
    [SerializeField] private float _destroyDistance = 0.2f;
    [SerializeField] private LayerMask _vacuumLayer;

    
    [SerializeField] private float _triggerThreshold = 0.7f;

    
    [SerializeField] private AudioSource _suctionAudioSource;
    [SerializeField] private AudioClip _suctionLoopClip;

    
    private const int MaxOverlapResults = 32;
    private readonly Collider[] _overlapBuffer = new Collider[MaxOverlapResults];

    
    private float _destroyDistanceSqr;
    private float _suctionRadiusSqr;

    
    private bool _isTriggerPressed;
    private bool _isGrabbed;

    

    private void Reset()
    {
        _grabbable = GetComponent<Grabbable>();
        _suctionAudioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        
        _destroyDistanceSqr = _destroyDistance * _destroyDistance;
        _suctionRadiusSqr = _suctionRadius * _suctionRadius;

        InitAudio();
    }

    private void Update()
    {
        
        _isGrabbed = _grabbable != null && _grabbable.SelectingPointsCount > 0;
        _isTriggerPressed = _isGrabbed && IsIndexTriggerPressed();

        HandleSuctionAudio();
    }

    private void FixedUpdate()
    {
        if (!_isTriggerPressed || _suctionPoint == null) return;

        ApplySuction();
    }

    private void OnDrawGizmosSelected()
    {
        if (_suctionPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_suctionPoint.position, _suctionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_suctionPoint.position, _destroyDistance);
    }

    

    private void ApplySuction()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            _suctionPoint.position,
            _suctionRadius,
            _overlapBuffer,
            _vacuumLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
            ProcessCollider(_overlapBuffer[i]);
    }

    private void ProcessCollider(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        Vector3 toSuction = _suctionPoint.position - rb.position;

        
        float distanceSqr = toSuction.sqrMagnitude;

        if (distanceSqr <= _destroyDistanceSqr)
        {
            VacuumObject(rb);
            return;
        }

        
        float distance = Mathf.Sqrt(distanceSqr);
        rb.AddForce((toSuction / distance) * _suctionForce, ForceMode.Acceleration);
    }

    private void VacuumObject(Rigidbody rb)
    {
        
        TrashItem trash = rb.GetComponent<TrashItem>()
                       ?? rb.GetComponentInParent<TrashItem>();
        trash?.OnVacuumed();

        TrashAudioPlayer.Instance?.PlayPop();

        Destroy(rb.gameObject);
    }

    

    private void InitAudio()
    {
        if (_suctionAudioSource == null) return;
        _suctionAudioSource.loop = true;
        _suctionAudioSource.playOnAwake = false;
        if (_suctionLoopClip != null)
            _suctionAudioSource.clip = _suctionLoopClip;
    }

    private void HandleSuctionAudio()
    {
        if (_suctionAudioSource == null) return;

        if (_isTriggerPressed && !_suctionAudioSource.isPlaying)
            _suctionAudioSource.Play();
        else if (!_isTriggerPressed && _suctionAudioSource.isPlaying)
            _suctionAudioSource.Stop();
    }

   

    private bool IsIndexTriggerPressed()
    {
        float left = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float right = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        return left > _triggerThreshold || right > _triggerThreshold;
    }

   
}