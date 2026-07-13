using TMPro;
using UnityEngine;

public class FpsCounterDisplay : MonoBehaviour, IUpdatable
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private CustomUpdateManager updateManager;

    private float accumulatedTime = 0f;
    private int frameCount = 0;

    private void OnEnable()
    {
        if (updateManager != null) updateManager.Register(this);
    }

    private void OnDisable()
    {
        if (updateManager != null) updateManager.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        accumulatedTime += deltaTime;
        frameCount++;

        if (accumulatedTime >= updateInterval)
        {
            float fps = frameCount / accumulatedTime;
            if (fpsText != null) fpsText.text = $"{fps:F0} FPS";
            accumulatedTime = 0f;
            frameCount = 0;
        }
    }
}
