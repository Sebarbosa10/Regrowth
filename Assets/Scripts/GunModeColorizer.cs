using System.Collections;
using UnityEngine;


public class GunModeColorizer : MonoBehaviour
{
    [System.Serializable]
    public class ModeMaterial
    {
        public string modeName;
        public Material material;
    }

    
    [SerializeField] private Renderer[] targetRenderers;

    
    [SerializeField] private int materialIndex = 0;

    
    [SerializeField]
    private ModeMaterial[] modeMaterials = new ModeMaterial[]
    {
        new ModeMaterial { modeName = "Single  - Plastic" },
        new ModeMaterial { modeName = "Burst   - Glass"   },
        new ModeMaterial { modeName = "Auto    - Organic" },
        new ModeMaterial { modeName = "Spread  - Metal"   },
    };

    private int currentModeIndex = 0;

    
    public void SetMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex >= modeMaterials.Length) return;
        currentModeIndex = modeIndex;

        Material target = modeMaterials[modeIndex].material;
        if (target == null)
        {
            Debug.LogWarning($"[GunModeColorizer] Modo {modeIndex} no tiene material asignado.");
            return;
        }

        ApplyMaterial(target);
    }

    
    public void SetEnergyLerp(float t, Material depletedMaterial)
    {
        if (modeMaterials[currentModeIndex].material == null || depletedMaterial == null) return;

        Color chargedColor = modeMaterials[currentModeIndex].material.color;
        Color depletedColor = depletedMaterial.color;

        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;
            Material[] mats = r.materials;
            if (materialIndex >= mats.Length) continue;
            mats[materialIndex].color = Color.Lerp(chargedColor, depletedColor, t);
            r.materials = mats;
        }
    }

   
    public void SetDepletedMaterial(Material mat)
    {
        ApplyMaterial(mat);
    }


    public Material GetCurrentMaterial()
    {
        if (currentModeIndex < 0 || currentModeIndex >= modeMaterials.Length) return null;
        return modeMaterials[currentModeIndex].material;
    }

    private void ApplyMaterial(Material mat)
    {
        if (targetRenderers == null) return;

        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;
            Material[] mats = r.materials;
            if (materialIndex >= mats.Length)
            {
                Debug.LogWarning($"[GunModeColorizer] materialIndex {materialIndex} fuera de rango en {r.name}");
                continue;
            }
            mats[materialIndex] = mat;
            r.materials = mats;
        }
    }
}