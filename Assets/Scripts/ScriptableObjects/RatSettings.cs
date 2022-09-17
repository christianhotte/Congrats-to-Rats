using UnityEngine;

/// <summary>
/// Object containing data regarding the nature and settings of a type of rat (unlike SwarmSettings, this does not scale with volume).
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RatSettings", order = 1)]
public class RatSettings : ScriptableObject
{
    [Header("General:")]
    [Tooltip("Height above ground at which rat will rest")]                                       public float baseHeight;
    [Tooltip("Height at and above which rat will fall off ledges instead of climbing down them")] public float fallHeight;
    [Tooltip("Maximum speed at which rat can move up or down (in units per second)")]             public float heightChangeRate;
    [Tooltip("Determines which objects will be treated as walls and floors by this rat")]         public LayerMask obstructionLayers;
    [Header("Visuals:")]
    [Tooltip("Maximum amount of random scale increase or decrease when spawning a new rat")]                    public float sizeVariance;
    [Tooltip("Darkest alternative color, rat color is randomized within range between this and default color")] public Color altColor;
    [Tooltip("Increase this to prevent rat from flipping back and forth rapidly")]                              public float timeBetweenFlips;
    [Header("Behavior:")]
    [Tooltip("Desired distance this rat keeps between itself and walls/cliffs")] public float obstacleSeparation;
    [Min(0), Tooltip("Tendency for rats to avoid walls and cliffs")]             public float obstacleAvoidanceWeight;
}
