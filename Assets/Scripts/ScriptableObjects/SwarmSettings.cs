using UnityEngine;

/// <summary>
/// Object containing preset data regarding follower rat behavior.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SwarmSettings", order = 1)]
public class SwarmSettings : ScriptableObject
{
    [Header("General:")]
    [Min(0), Tooltip("How high off of the ground rats hover")]                                                                       public float baseRatHeight;
    [Min(0), Tooltip("Maximum speed rats may travel at")]                                                                            public float maxSpeed;
    [Min(0), Tooltip("Distance with rats will try to keep between themselves and other rats")]                                       public float separationRadius;
    [Min(0), Tooltip("Distance at which rats will influence each other and clump together (should be larger than separationRadius")] public float neighborRadius;
    [Header("Rule Weights:")]
    [Min(0), Tooltip("Tendency for rats to move toward other nearby rats")]              public float cohesionWeight = 1;
    [Min(0), Tooltip("Tendency for rats to maintain a small distance from nearby rats")] public float separationWeight = 1;
    [Min(0), Tooltip("Tendency for rats to match speed with other nearby rats")]         public float conformityWeight = 1;
    [Min(0), Tooltip("Tendency for rats to move toward desired position")]               public float targetWeight = 1;
    [Min(0), Tooltip("Tendency for rats to move along path when leader is moving")]      public float followWeight = 1;
}
