using UnityEngine;

/// <summary>
/// Object containing preset data regarding big rat controls and behavior.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BigRatSettings", order = 1)]
public class BigRatSettings : ScriptableObject
{
    [Header("Movement:")]
    [Min(0), Tooltip("How fast the rat can move")]                         public float speed;
    [Min(0), Tooltip("How quickly the rat reaches max speed")]             public float accel;
    [Min(0), Tooltip("How quickly the rat comes to a stop")]               public float decel;
    [Tooltip("Layers which will obstruct rat movement")]                   public LayerMask blockingLayers;
    [Min(0), Tooltip("Physical thickness of rat when bumping into walls")] public float collisionRadius;
    [Header("Spawning:")]
    [Tooltip("Prefab object for default rats in the swarm")]   public GameObject basicRatPrefab;
    [Min(0), Tooltip("How far away followers can be spawned")] public float spawnRadius;
}