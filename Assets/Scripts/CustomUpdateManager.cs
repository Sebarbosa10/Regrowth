using System.Collections.Generic;
using UnityEngine;

public class CustomUpdateManager : MonoBehaviour
{
    public static CustomUpdateManager Instance { get; private set; }

    private readonly List<IUpdatable> updatables = new List<IUpdatable>();
    private readonly Dictionary<IUpdatable, int> updatablesSet = new Dictionary<IUpdatable, int>();

    private readonly List<IUpdatable> pendingAdds = new List<IUpdatable>();
    private readonly

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = updatables.Count - 1; i >= 0; i--)
            updatables[i].Tick(dt);
    }

    public void Register(IUpdatable updatable)
    {
        if (updatablesSet.Add(updatable))
            updatables.Add(updatable);
    }

    public void Unregister(IUpdatable updatable)
    {
        if (updatablesSet.Remove(updatable))
            updatables.Remove(updatable);
    }
}
