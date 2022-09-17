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
    [Header("Collisions:")]
    [Tooltip("Layers which will obstruct rat movement")]                                      public LayerMask blockingLayers;
    [Min(0), Tooltip("Physical thickness of rat when bumping into walls and touching floor")] public float collisionRadius;
    [Tooltip("Maximum steepness of terrain rat can move normally on")]                        public float maxWalkAngle;
    [Min(0), Tooltip("Step height at which rat will fall instead of climbing down")]          public float fallHeight;
    [Min(1), Tooltip("The maximum number of obstacles rat can collide with at once")]         public int maxObstacleCollisions;
    [Header("Spawning:")]
    [Tooltip("Prefab object for default rats in the swarm")]                                       public GameObject basicRatPrefab;
    [Min(0), Tooltip("How far away followers can be spawned")]                                     public float spawnRadius;
    [Min(0), Tooltip("Base height at which rats hover off the ground")]                            public float baseFollowerHeight;
    [Min(0), Tooltip("Maximum speed (in units per second) at which rats can change in elevation")] public float maxHeightChangeSpeed;
}