using System.Collections.Generic;
using UnityEngine;


public class HolsterManager : MonoBehaviour
{
    // Singleton simple sin DontDestroyOnLoad (el juego tiene una sola escena submarina)
    public static HolsterManager Instance { get; private set; }

    // Lista de holsters registrados — se llena sola, sin asignación manual
    private readonly List<WeaponHolster> _holsters = new List<WeaponHolster>(4);

    // Diccionario snapPoint ? holster para búsqueda O(1)
    private readonly Dictionary<Transform, WeaponHolster> _snapPointMap
        = new Dictionary<Transform, WeaponHolster>(4);

    // ?????????????????????????????????????????????
    #region Singleton Setup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Registration

    /// <summary>
    /// Los WeaponHolster llaman esto en su OnEnable para registrarse.
    /// </summary>
    public void RegisterHolster(WeaponHolster holster, Transform snapPoint)
    {
        if (!_holsters.Contains(holster))
        {
            _holsters.Add(holster);
            if (snapPoint != null)
                _snapPointMap[snapPoint] = holster;

            Debug.Log($"[HolsterManager] Holster registrado: {holster.Slot} ({_holsters.Count} total)");
        }
    }

    /// <summary>
    /// Los WeaponHolster llaman esto en su OnDisable.
    /// </summary>
    public void UnregisterHolster(WeaponHolster holster, Transform snapPoint)
    {
        _holsters.Remove(holster);
        if (snapPoint != null)
            _snapPointMap.Remove(snapPoint);
    }

    #endregion

    // ?????????????????????????????????????????????
    #region Queries

    /// <summary>
    /// Encuentra el holster compatible más cercano al arma que está siendo soltada.
    /// Retorna null si ninguno está en rango.
    /// </summary>
    public WeaponHolster FindNearestCompatibleHolster(HolsterableWeapon weapon)
    {
        WeaponHolster nearest = null;
        float nearestDistSqr = float.MaxValue;

        int count = _holsters.Count;
        for (int i = 0; i < count; i++)
        {
            WeaponHolster holster = _holsters[i];

            // Descartamos: ocupado o incompatible
            if (holster.IsOccupied) continue;
            if (!holster.IsWeaponInRange(weapon)) continue;

            // Buscamos el más cercano si hay varios en rango
            float distSqr = (holster.transform.position - weapon.transform.position).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = holster;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Busca un holster dado su snapPoint. Usado por HolsterableWeapon.
    /// </summary>
    public WeaponHolster FindHolsterBySnapPoint(Transform snapPoint)
    {
        _snapPointMap.TryGetValue(snapPoint, out WeaponHolster holster);
        return holster;
    }

    /// <summary>
    /// Retorna el snapPoint de un holster. Usado por HolsterableWeapon para el Lerp.
    /// </summary>
    public Transform GetSnapPoint(WeaponHolster holster)
    {
        foreach (var kvp in _snapPointMap)
            if (kvp.Value == holster) return kvp.Key;
        return null;
    }

    #endregion
}