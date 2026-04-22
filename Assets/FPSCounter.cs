using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FPSCounter : MonoBehaviour
{
    float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int fps = (int)(1.0f / deltaTime);
        GUI.Label(new Rect(10, 10, 200, 40), "FPS: " + fps);
    }
}