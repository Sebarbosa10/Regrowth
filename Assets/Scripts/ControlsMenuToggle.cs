using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControlsMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private OVRInput.Button toggleButton = OVRInput.Button.Three; // Botón X

    private bool wasPressed = false;

    private void Update()
    {
        bool pressed = OVRInput.Get(toggleButton);

        if (pressed && !wasPressed)
        {
            controlsMenu.SetActive(!controlsMenu.activeSelf);
        }

        wasPressed = pressed;
    }
}