using UnityEngine;

public class TrashFloat : MonoBehaviour, IUpdatable
{
    [System.Serializable]
    public class FloatConfig
    {
        public float bobAmplitude = 0.1f;
        public float bobSpeed = 1f;
        public float driftSpeed = 0.1f;
        public float driftChangeInterval = 3f;
        public float rotationSpeed = 15f;
    }

    [SerializeField]
    private FloatConfig[] stageConfigs = new FloatConfig[]
    {
        new FloatConfig { bobAmplitude = 0.08f, bobSpeed = 0.6f, driftSpeed = 0.08f, driftChangeInterval = 4f, rotationSpeed = 8f },
        new FloatConfig { bobAmplitude = 0.15f, bobSpeed = 1f,   driftSpeed = 0.15f, driftChangeInterval = 3f, rotationSpeed = 15f },
        new FloatConfig { bobAmplitude = 0.25f, bobSpeed = 1.8f, driftSpeed = 0.25f, driftChangeInterval = 1.5f, rotationSpeed = 30f },
    };

    private FloatConfig config;
    private Vector3 startPosition;
    private Vector3 driftDirection;
    private float bobOffset;
    private float driftTimer;
    private Vector3 rotationAxis;

    private void Start()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentRound : 0;
        stage = Mathf.Clamp(stage, 0, stageConfigs.Length - 1);
        config = stageConfigs[stage];

        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        driftDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        driftTimer = config.driftChangeInterval;
        rotationAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    private void OnEnable()
    {
        if (CustomUpdateManager.Instance != null)
        {
            CustomUpdateManager.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning($"[TrashFloat] CustomUpdateManager.Instance es null al activar {gameObject.name}", this);
        }
    }

    private void OnDisable()
    {
        if (CustomUpdateManager.Instance != null) CustomUpdateManager.Instance.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        float bobY = Mathf.Sin(Time.time * config.bobSpeed + bobOffset) * config.bobAmplitude;

        driftTimer -= deltaTime;
        if (driftTimer <= 0f)
        {
            driftDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            driftTimer = config.driftChangeInterval;
        }

        startPosition += driftDirection * (config.driftSpeed * deltaTime);
        transform.position = new Vector3(startPosition.x, startPosition.y + bobY, startPosition.z);
        transform.Rotate(rotationAxis, config.rotationSpeed * deltaTime, Space.World);
    }
}