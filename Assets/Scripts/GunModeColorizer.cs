using System.Collections;
using UnityEngine;

public class GunModeColorizer : MonoBehaviour
{
    [System.Serializable]
    public class ModeMaterial
    {
        public string modeName;       // Solo para el Inspector
        public Material material;     // Material a aplicar en este modo
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

    // ─────────────────────────────────────────
    //  PUBLIC — llamar desde TrashGun al cambiar modo
    // ─────────────────────────────────────────

    /// <summary>
    /// Swappea instantáneamente el material en el índice indicado.
    /// </summary>
    public void SetMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex >= modeMaterials.Length) return;

        Material target = modeMaterials[modeIndex].material;
        if (target == null)
        {
            Debug.LogWarning($"[GunModeColorizer] Modo {modeIndex} no tiene material asignado.");
            return;
        }

        ApplyMaterial(target);
    }

    // ─────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────

    private void ApplyMaterial(Material mat)
    {
        if (targetRenderers == null) return;

        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;

            // Copiamos el array para no mutar el sharedMaterials directamente
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