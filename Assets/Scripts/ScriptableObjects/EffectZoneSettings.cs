using UnityEngine;

/// <summary>
/// Settings object for EffectZone system.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EffectZoneSettings", order = 1)]
public class EffectZoneSettings : ScriptableObject
{
    //Classes, Structs & Enums:

    //DATA:
    [Tooltip("Base width, height, and depth of the zone")] public Vector3 baseDimensions;
}
