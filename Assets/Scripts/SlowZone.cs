using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowZone : EffectZone
{
    //Settings:
    [Header("Glue Settings:")]
    [Range(0, 1), Tooltip("Multiplier which this zone applies to rats within it")] public float slowFactor;
}
