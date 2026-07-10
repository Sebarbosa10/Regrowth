using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomUpdateManager : MonoBehaviour
{
    public static CustomUpdateManager Instance { get; private set; }

    private readonly List<IUpdatable> updatables = new List<IUpdatable>();
    private readonly Dictionary<IUpdatable, int> indexMap = new Dictionary<IUpdatable, int>();

    private readonly List<IUpdatable> pendingAdds = new List<IUpdatable>();
    private readonly List<IUpdatable> pendingRemoves = new List<IUpdatable>();
    private bool isTicking;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        isTicking = true;
        for (int i = 0; i < updatables.Count; i++)
        {
            var u = updatables[i];
            try
            {
                u.Tick(dt);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
        isTicking = false;

        FlushPending();
    }

    public void Register(IUpdatable updatable)
    {
        if (updatable == null || indexMap.ContainsKey(updatable)) return;

        if (isTicking)
        {
            pendingRemoves.Add(updatable);
            return;
        }

        AddImmediate.Add(updatable);
    }

    public void Unregister(IUpdatable updatable)
    {
        if (updatable == null || indexMap.ContainsKey(updatable)) return;

        if (isTicking)
        {
            pendingRemoves.Add(updatable);
            return;
        }

        RemoveImmediate.Add(updatable);
    }

    private void AddImmediate(IUpdatable updatable)
    {
        indexMap[updatable] = updatables.Count;
        updatables.Add(updatable);
    }
}
