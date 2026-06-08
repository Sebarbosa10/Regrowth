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

    [Header("Renderers a colorear")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Index del material dentro de cada Renderer")]
    [SerializeField] private int materialIndex = 0;

    [Header("Materiales por modo (Single, Burst, Auto, Spread)")]
    [SerializeField]
    private ModeMaterial[] modeMaterials = new ModeMaterial[]
    {
        new ModeMaterial { modeName = "Single  - Plastic" },
        new ModeMaterial { modeName = "Burst   - Glass"   },
        new ModeMaterial { modeName = "Auto    - Organic" },
        new ModeMaterial { modeName = "Spread  - Metal"   },
    };

    private int currentModeIndex = 0;

    // Llamado al cambiar modo — restaura el material del modo
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

    // Llamado por GunEnergySystem — interpola color disparo a disparo
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

    // Llamado por GunEnergySystem cuando se queda sin energía
    public void SetDepletedMaterial(Material mat)
    {
        ApplyMaterial(mat);
    }

    // Devuelve el material del modo actual (para que GunEnergySystem lo guarde)
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