using System;
using UnityEngine;

[Serializable]
public class WeaponManager
{
    public string ime = "Pistolj";
    [Min(1)] public int damage = 10;
    [Min(1f)] public float range = 100f;
    [Min(1)] public int headshotMultiplier = 3;

    [Header("Ammunition")]
    [Min(1)] public int magazineSize = 12;
    [Min(0.05f)] public float fireInterval = 0.25f;
    [Min(0.1f)] public float reloadTime = 1.5f;
}
