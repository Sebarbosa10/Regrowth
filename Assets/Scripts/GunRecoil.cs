using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    [System.Serializable]
    public class RecoilProfile
    {
        public string modeName;
        public float kickBack = 0.05f;      
        public float kickUp = 3f;           
        public float kickSide = 1f;         
        public float returnSpeed = 8f;      
        public float kickSpeed = 20f;       
    }

    
    [SerializeField]
    private RecoilProfile[] profiles = new RecoilProfile[]
    {
        new RecoilProfile { modeName = "Single",  kickBack = 0.04f, kickUp = 3f,  kickSide = 0.5f, returnSpeed = 10f, kickSpeed = 25f },
        new RecoilProfile { modeName = "Burst",   kickBack = 0.03f, kickUp = 2f,  kickSide = 0.8f, returnSpeed = 12f, kickSpeed = 30f },
        new RecoilProfile { modeName = "Auto",    kickBack = 0.02f, kickUp = 1.5f,kickSide = 1f,   returnSpeed = 15f, kickSpeed = 35f },
        new RecoilProfile { modeName = "Spread",  kickBack = 0.08f, kickUp = 6f,  kickSide = 2f,   returnSpeed = 7f,  kickSpeed = 20f },
    };

   
    [SerializeField] private Transform gunModel; 

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Start()
    {
        if (gunModel == null) gunModel = transform;
        originalPosition = gunModel.localPosition;
        originalRotation = gunModel.localRotation;
        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        
        gunModel.localPosition = Vector3.Lerp(
            gunModel.localPosition,
            targetPosition,
            Time.deltaTime * GetCurrentReturnSpeed()
        );

        gunModel.localRotation = Quaternion.Slerp(
            gunModel.localRotation,
            targetRotation,
            Time.deltaTime * GetCurrentReturnSpeed()
        );

        
        if (Vector3.Distance(gunModel.localPosition, targetPosition) < 0.001f)
        {
            targetPosition = originalPosition;
            targetRotation = originalRotation;
        }
    }

    public void ApplyRecoil(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex >= profiles.Length) return;

        RecoilProfile profile = profiles[modeIndex];

        
        float randomSide = Random.Range(-profile.kickSide, profile.kickSide);

        gunModel.localPosition -= new Vector3(0, 0, profile.kickBack);
        gunModel.localRotation *= Quaternion.Euler(-profile.kickUp, randomSide, 0);

        
        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }

    private float GetCurrentReturnSpeed()
    {
        
        return 10f;
    }
}