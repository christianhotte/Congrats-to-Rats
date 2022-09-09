using UnityEngine;

/// <summary>
/// Object containing preset data regarding big rat controls and behavior.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BigRatSettings", order = 1)]
public class BigRatSettings : ScriptableObject
{
    [Header("Movement:")]
    [Min(0), Tooltip("How fast the rat can move")]             public float speed;
    [Min(0), Tooltip("How quickly the rat reaches max speed")] public float accel;
    [Min(0), Tooltip("How quickly the rat comes to a stop")]   public float decel;
    [Header("Trail:")]
    [Min(0), Tooltip("Amount of length which is added to trail for each rat follower")]                    public float trailLengthPerRat;
    [Min(0), Tooltip("Minimum distance between two trail points (higher values will make trail simpler)")] public float minTrailSegLength;
    [Min(1), Tooltip("Determines how much trail stretches when big rat is moving")]                        public float velTrailLengthMultiplier = 1;
}