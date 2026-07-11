using UnityEngine;
using Oculus.Interaction;

public class GunEnergySystem : MonoBehaviour
{
    [SerializeField] private int maxShots = 10;
    [SerializeField] private GunModeColorizer colorizer;
    [SerializeField] private Material depletedMaterial;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip depletedSound;
    [SerializeField] private AudioClip rechargedSound;

    private int shotsRemaining;
    private bool isDepleted = false;
    private int currentModeIndex = 0;
    private Material lastChargedMaterial;

    public bool IsDepleted => isDepleted;
    public bool IsFull => shotsRemaining >= maxShots;

    private void Start()
    {
        shotsRemaining = maxShots;
    }

    public bool TryShoot()
    {
        if (isDepleted) return false;

        shotsRemaining--;
        UpdateEnergyVisual();

        if (shotsRemaining <= 0)
        {
            Deplete();
            return false;
        }

        return true;
    }

    public void SetCurrentMode(int modeIndex, Material chargedMaterial)
    {
        currentModeIndex = modeIndex;
        lastChargedMaterial = chargedMaterial;
    }

    public void ForceRecharge()
    {
        Recharge();
    }

    private void Deplete()
    {
        isDepleted = true;
        colorizer?.SetDepletedMaterial(depletedMaterial);
        if (audioSource != null && depletedSound != null)
            audioSource.PlayOneShot(depletedSound);
    }

    private void Recharge()
    {
        isDepleted = false;
        shotsRemaining = maxShots;
        colorizer?.SetMode(currentModeIndex);
        if (audioSource != null && rechargedSound != null)
            audioSource.PlayOneShot(rechargedSound);
    }

    private void UpdateEnergyVisual()
    {
        if (colorizer == null || depletedMaterial == null) return;
        float t = 1f - ((float)shotsRemaining / maxShots);
        colorizer.SetEnergyLerp(t, depletedMaterial);
    }
}