using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomUpdateManager : MonoBehaviour
{
    
    private List<IUpdatable> updatables = new List<IUpdatable>();

    
    void Update()
    {
        foreach (var u in updatables)
        {
            u.Tick(Time.deltaTime); 
        }
    }

    
    public void Register(IUpdatable updatable)
    {
        if (!updatables.Contains(updatable))
            updatables.Add(updatable);
    }

    
    public void Unregister(IUpdatable updatable)
    {
        updatables.Remove(updatable);
    }
}