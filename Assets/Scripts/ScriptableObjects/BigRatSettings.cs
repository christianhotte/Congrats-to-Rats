using UnityEngine;

/// <summary>
/// Object containing preset data regarding big rat controls and behavior.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BigRatSettings", order = 1)]
public class BigRatSettings : ScriptableObject
{
    [Header("General:")]
    [Tooltip("Flips all sprite visualizations on big and small rats")] public bool flipAll;

    [Header("Movement:")]
    [Min(0), Tooltip("How fast the rat can move")]             public float speed;
    [Min(0), Tooltip("How quickly the rat reaches max speed")] public float accel;
    [Min(0), Tooltip("How quickly the rat comes to a stop")]   public float decel;

    [Header("Jumping & Falling:")]
    [Min(0), Tooltip("Forward and upward power of rat jump")]                                                                      public Vector2 jumpPower;
    [Min(1), Tooltip("Multiplier added to jump force when jumping while standing")]                                                public float stationaryJumpMultiplier;
    [Range(0, 1), Tooltip("Percentage of normal movement control rat has while in the air")]                                       public float airControl;
    [Min(0), Tooltip("Force used to bounce rat off cliffs to prevent jank")]                                                       public float cliffHop;
    [Min(0), Tooltip("Step height at which rat will fall instead of climbing down")]                                               public float fallHeight;
    [Min(0), Tooltip("Downward acceleration (in units per second squared) while falling")]                                         public float gravity;
    [Min(0), Tooltip("Resistance to motion while in air")]                                                                         public float airDrag;
    [Range(0, 1), Tooltip("Percentage of velocity which is retained each time rat bounces off a wall")]                            public float bounciness;
    [Min(0), Tooltip("Low velocity value used to prevent rat from hanging on walls after jumping")]                                public float wallRepulse;
    [Tooltip("Used to predict whether a jump is worth making jump tokens for. X is length in front of rat, Y is vertical offset")] public Vector2 jumpValidationOffset;

    [Header("Collisions:")]
    [Tooltip("Layers which will obstruct rat movement")]                                      public LayerMask blockingLayers;
    [Min(0), Tooltip("Physical thickness of rat when bumping into walls and touching floor")] public float collisionRadius;
    [Tooltip("Maximum steepness of terrain rat can move normally on")]                        public float maxWalkAngle;
    [Min(1), Tooltip("The maximum number of obstacles rat can collide with at once")]         public int maxObstacleCollisions;

    [Header("Spawning & Followers:")]
    [Tooltip("Prefab object for default rats in the swarm")]                                            public GameObject basicRatPrefab;
    [Min(0), Tooltip("Radius of circle around big rat within which free rats will be made followers")]  public float influenceRadius;
    [Tooltip("Coordinate offsets for center of spawn area")]                                            public Vector3 spawnOffset;
    [Tooltip("Width and depth of rectangular area within which new rats will spawn (and launch from)")] public Vector2 spawnArea;
    [MinMaxSlider(0, 90), Tooltip("Range of angles at which rat babies will be launched when spawned")] public Vector2 spawnAngle;
    [MinMaxSlider(0, 5), Tooltip("Force with which rat babies will be launched when spawned")]          public Vector2 spawnForce;
    [Tooltip("Offset from transform to which held food is moved to")]                                   public Vector2 heldFoodOffset;

    [Header("Death & Respawn Sequence:")]
    [Min(0), Tooltip("Time rat spends dead before respawning")]                                                             public float deadTime;
    [Min(0), Tooltip("Time given for camera to pan to respawn position")]                                                   public float respawnTransTime;
    [Min(0), Tooltip("Power with which rat is launched forward upon respawning (x is horizontal component, y is vertical")] public Vector2 respawnLaunchPower;

    [Header("Throwing:")]
    [Tooltip("Layers which rat can throw baby rats at")]                                                     public LayerMask throwTargetLayers;
    [Min(0), Tooltip("Force with which rat is able to throw other rats")]                                    public float throwForce;
    [Tooltip("Maximum distance from throw target which random spread can cause")]                            public float maxRandomSpread;
    [Min(0), Tooltip("Number of seconds to wait before throw force/rat count begins charging up")]           public float throwChargeWait;
    [Min(0), Tooltip("Percentage of full charge which will be gained per second while holding throw input")] public float throwChargeSpeed;
    [Min(0), Tooltip("Maximum percentage of total swarm which can be thrown at once")]                       public float maxRatPercentPerThrow;
    [Min(1), Tooltip("Hard cap on how many rats can be thrown at once")]                                     public int maxRatsPerThrow;
}