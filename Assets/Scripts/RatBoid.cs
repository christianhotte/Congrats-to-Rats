using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller for an individual rat follower.
/// </summary>
public class RatBoid : MonoBehaviour
{
    //Static Stuff:
    /// <summary>
    /// List of all rats currently spawned in scene.
    /// </summary>
    public static List<RatBoid> spawnedRats = new List<RatBoid>();
    public static readonly float timeBalancer = 100; //Value applied to all acceleration applications (so that settings aren't messed up by adding deltaTime)

    //Objects & Components:
    private SpriteRenderer r;    //Render component for this rat's sprite
    public RatSettings settings; //Settings object determining this rat's properties

    //Settings: NOTE: Put these in a scriptableObject
    [Header("Settings:")]
    [Min(0), Tooltip("Maximum allowed variance in scale (can go up or down)")]     public float scaleRandomness;
    [Tooltip("Alternate color rats can spawn with")]                               public Color altColor;
    [Tooltip("Increase this to prevent rat from flipping back and forth rapidly")] public float timeBetweenFlips;

    //Realtime Vars:
    /// <summary>
    /// Current speed and direction of boid.
    /// </summary>
    internal Vector2 velocity;
    /// <summary>
    /// Current position of this rat as a vector2.
    /// </summary>
    internal Vector2 flatPos;

    internal List<RatBoid> currentNeighbors = new List<RatBoid>();  //Other rats which are currently close to this rat
    internal List<RatBoid> currentSeparators = new List<RatBoid>(); //Other rats which are currently too close to this rat
    internal bool follower;                                         //If true, indicates that this rat is following the big main rat
    internal float lastTrailValue = -1;                             //Latest value of target on trail (should be reset whenever rat loses target

    private float timeUntilFlip; //Time before this rat is able to flip its sprite orientation (prevents jiggling)

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        r = GetComponentInChildren<SpriteRenderer>(); //Get renderer from child object

        //Initialization:
        spawnedRats.Add(this); //Add this rat to master list of spawned rats

        //Randomize properties:
        float newScale = 1 + Random.Range(-settings.sizeVariance, settings.sizeVariance); //Get value of new scale
        transform.localScale = Vector3.one * newScale;                                    //Set new scale
        r.color = Color.Lerp(r.color, settings.altColor, Random.value);                   //Randomize color
    }
    private void OnDestroy()
    {
        //Final cleanup:
        spawnedRats.Remove(this); //Remove this rat from master list of spawned rats
    }
    private void Update()
    {
        //Update flip timer (TEMP):
        if (timeUntilFlip > 0)
        {
            timeUntilFlip = Mathf.Max(0, timeUntilFlip - Time.deltaTime); //Update flip timer if necessary
            if (timeUntilFlip == 0)
            {
                bool prevFlip = r.flipX;
                if (velocity.x < 0) r.flipX = MasterRatController.main.settings.flipAll;
                else if (velocity.x > 0) r.flipX = !MasterRatController.main.settings.flipAll;
                if (prevFlip != r.flipX) timeUntilFlip = settings.timeBetweenFlips;
            }
        }
        else if (velocity.x < 0 && r.flipX != MasterRatController.main.settings.flipAll || velocity.x > 0 && r.flipX == MasterRatController.main.settings.flipAll)
        {
            r.flipX = !r.flipX;
            timeUntilFlip = settings.timeBetweenFlips;
        }
    }

    //STATIC METHODS:
    /// <summary>
    /// Updates status and positions of all spawned rats.
    /// </summary>
    /// <param name="deltaTime"></param>
    public static void UpdateRats(float deltaTime, SwarmSettings settings)
    {
        //Update rat position & data:
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            //Initialize:
            Vector3 newPos = rat.transform.position;                                     //Get current position of rat
            newPos.x += rat.velocity.x * deltaTime;                                      //Add X velocity
            newPos.z += rat.velocity.y * deltaTime;                                      //Add Y velocity
            float adjustedHeight = rat.settings.baseHeight * rat.transform.localScale.x; //Get effective height of rat based on scale
            float floorCheckHeight = adjustedHeight + rat.settings.fallHeight;           //Get height value used in floor checks

            //Check obstacles & floor:
            if (Physics.Linecast(rat.transform.position, newPos, out RaycastHit hit, rat.settings.obstructionLayers)) //Rat is obstructed
            {
                Vector3 idealPos = newPos;                                       //Get position rat would move to if not obstructed
                newPos = hit.point + (0.001f * hit.normal);                      //Move rat to hit location (and scooch away slightly so that it is able to re-collide)
                newPos += Vector3.ProjectOnPlane(idealPos - newPos, hit.normal); //Add remainder of velocity by projecting change in position onto plane defined by hit normal
            }
            Vector3 fallPoint = newPos + (Vector3.down * floorCheckHeight); //Get point used to check whether or not rat is falling
            if (Physics.Linecast(newPos, fallPoint, out hit, rat.settings.obstructionLayers)) //Floor under rat can be found
            {
                newPos = Vector3.MoveTowards(newPos, hit.point + (Vector3.up * adjustedHeight), rat.settings.heightChangeRate * deltaTime);                                   //Move target position upward according to height of floor
                rat.GetComponentInChildren<Billboarder>().targetZRot = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(hit.normal, Vector3.forward), Vector3.forward); //Twist billboard so rat is flat on surface
            }
            else //No floor found
            {
                if (Physics.Raycast(fallPoint, -UnFlattenVector(rat.velocity), out hit, rat.velocity.magnitude, rat.settings.obstructionLayers)) //Fall can be prevented
                {
                    newPos = hit.point + (rat.velocity.magnitude * deltaTime * -hit.normal) + (Vector3.up * floorCheckHeight); //Trace back up over the edge and place rat there
                }
                else //Fall cannot be avoided for some reason (very rare)
                {
                    
                }
            }

            //Cleanup:
            rat.flatPos.x = newPos.x; rat.flatPos.y = newPos.z; //Update flat position tracker
            rat.transform.position = newPos;                    //Move rat to match new position
            rat.currentNeighbors = new List<RatBoid>();         //Clear neighbors list
            rat.currentSeparators = new List<RatBoid>();        //Clear separators list
        }

        //Establish proximity relationships:
        if (spawnedRats.Count > 1) //Only check for relationships if there is more than one rat
        {
            for (int r = 0; r < spawnedRats.Count; r++) //Iterate through every follower rat (with index variable)
            {
                RatBoid rat = spawnedRats[r]; //Get current rat controller
                for (int o = r + 1; o < spawnedRats.Count; o++) //Iterate through list of rats, ignoring already-checked rats and current rat (all other undefined rats)
                {
                    RatBoid otherRat = spawnedRats[o];                            //Get other rat controller
                    float dist = Vector2.Distance(rat.flatPos, otherRat.flatPos); //Get distance between rats
                    if (dist <= settings.neighborRadius) //Rats are close enough to be neighbors
                    {
                        rat.currentNeighbors.Add(otherRat); //Add other rat to neighbors list
                        otherRat.currentNeighbors.Add(rat); //Add this rat to other rat's neighbors list
                        if (dist <= settings.separationRadius) //Rats are close enough to be separators
                        {
                            rat.currentSeparators.Add(otherRat);    //Add other rat to separators list
                            otherRat.currentSeparators.Add(rat);    //Add this rat to other rat's separators list
                        }
                    }
                }
            }
        }

        //RAT RULES:
        float adjustedDT = deltaTime * timeBalancer; //Get adjusted deltaTime to uncouple acceleration changes from framerate
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            if (rat.currentNeighbors.Count != 0) //Rat must have neighbors for these rules to apply
            {
                //RULE - Cohesion: (nearby rats stick together)
                Vector2 localCenterMass = GetCenterMass(rat.currentNeighbors.ToArray());             //Find center mass of this rat's current neighborhood
                Vector2 cohesionVel = localCenterMass - rat.flatPos;                                 //Get velocity vector representing direction and magnitude of rat separation from center mass
                cohesionVel = (cohesionVel / 100) * settings.cohesionWeight;                         //Apply weight and balancing values to cohesion velocity
                rat.velocity += Vector2.ClampMagnitude(cohesionVel, settings.maxSpeed) * adjustedDT; //Apply clamped velocity to rat

                //RULE - Conformity: (rats tend to match the velocity of nearby rats)
                Vector2 conformVel = Vector2.zero; //Initialize container to store gross conformance-induced velocity
                foreach (RatBoid otherRat in rat.currentNeighbors) //Iterate through list of rats which are close enough to influence this rat
                {
                    conformVel += otherRat.velocity; //Add velocity of rat to local velocity total
                }
                conformVel /= rat.currentNeighbors.Count;                                           //Get average velocity of neighbor rats
                conformVel = (conformVel / 8) * settings.conformityWeight;                          //Apply weight and balancing values to conformance velocity
                rat.velocity += Vector2.ClampMagnitude(conformVel, settings.maxSpeed) * adjustedDT; //Apply clamped velocity to rat
            }

            //RULE - Velocity Clamping: (rats cannot go faster than their max speed)
            rat.velocity = Vector2.ClampMagnitude(rat.velocity, settings.maxSpeed); //Clamp rat velocity to maximum speed

            //RULE - Separation: (rats maintain a small distance from each other)
            if (rat.currentSeparators.Count != 0) //Only do separation if rat has separators
            {
                Vector2 separationVel = Vector2.zero; //Initialize container to store gross separation-induced velocity
                foreach (RatBoid otherRat in rat.currentSeparators) //Iterate through list of rats which are currently too close to this rat
                {
                    Vector2 sepDir = rat.flatPos - otherRat.flatPos;                     //Get direction that will separate this rat from the one near it
                    float sepPower = 1 - (sepDir.magnitude / settings.separationRadius); //Get power such that the closer the rats are, the stronger the impulse
                    separationVel += sepDir.normalized * sepPower;                       //Add power and corresponding direction to separation velocity
                }
                separationVel *= settings.separationWeight; //Apply weight setting to separation rule
                rat.velocity += separationVel * adjustedDT; //Apply unclamped velocity to rat
            }

            if (rat.follower) //Rat must be a follower for these rules to apply
            {
                //Get current target:
                MasterRatController.TrailPointData data = MasterRatController.main.GetClosestPointOnTrail(rat.flatPos, rat.lastTrailValue, settings.maxTrailSkip * deltaTime); //Get data for closest point to rat position
                rat.lastTrailValue = data.linePosition;                                                                                                                        //Remember line position

                Vector2 target = data.point;                                                                 //Get position of target from data
                float targetDistance = Vector2.Distance(rat.flatPos, target);                                //Get separation between rat and target
                float targetRadius = settings.EvaluateTargetSize(data.linePosition) * settings.targetRadius; //Get target size depending on position in line and base target radius
                if (targetDistance > targetRadius) //Rat must be outside target distance for these rules to apply
                {
                    //RULE - Targeting: (rats tend to move toward target)
                    Vector2 targetVel = (target - rat.flatPos).normalized;                             //Get direction toward target
                    targetVel *= 0.01f * settings.targetWeight;                                        //Apply weight and balancing values to target velocity
                    rat.velocity += Vector2.ClampMagnitude(targetVel, settings.maxSpeed) * adjustedDT; //Apply clamped velocity to rat
                }
                else if (data.linePosition > settings.trailBuffer                                                      //Rat is within target distance and not too far ahead in line
                            && Vector2.Angle(MasterRatController.main.forward, data.forward) < 180 - settings.maxSegAngle //Prevent rats from being lead when main rat is backtracking
                            || data.linePosition > settings.backtrackBuffer)                                              //Lead all rats which are at least partially down the trail
                {
                    //RULE - Following: (rats on a trail will move along it toward the leader)
                    Vector2 followVel = data.forward;          //Get follow velocity from forward direction of trail
                    followVel *= 0.1f * settings.followWeight; //Apply weight and balancing values to follow velocity
                    rat.velocity += followVel * adjustedDT;    //Apply unclamped velocity to rat
                    
                    //RULE - Leading: (rats on a trail will move along it when leader is moving)
                    Vector2 leadVel = data.forward * MasterRatController.main.currentSpeed;    //Get lead velocity from forward direction of trail and speed of leader
                    float followStrength = settings.EvaluateFollowStrength(data.linePosition); //Get follow strength depending on position of rat in line
                    leadVel *= 0.1f * followStrength * settings.leadWeight;                    //Apply weight value and balancing values to lead velocity
                    rat.velocity += leadVel * adjustedDT;                                      //Apply unclamped velocity to rat
                }

                if (MasterRatController.main.totalTrailLength >= settings.minTrailLength) //Trail must be long enough for these rules to apply
                {
                    if (data.linePosition <= settings.trailBuffer) //Rat must be ahead of line for these rules to apply
                    {
                        //RULE - Staying Behind: (rats in front of leader tend to want to get behind it)
                        Vector2 stayBackVel = -data.forward;           //Make stay back velocity keep rat on trail
                        stayBackVel *= 0.1f * settings.stayBackWeight; //Apply weight and balancing values to stay back velocity
                        rat.velocity += stayBackVel * adjustedDT;      //Apply unclamped velocity to rat
                    }
                    else if (data.linePosition >= 1 - settings.trailBuffer) //Rat must be behind line for these rules to apply
                    {
                        //RULE - Straggler Prevention: (rats behind the line will speed up)
                        Vector2 stragglerVel = data.forward;       //Make velocity move rats toward end of trail
                        stragglerVel *= settings.stragglerWeight;  //Apply weight and balancing values to velocity
                        rat.velocity += stragglerVel * adjustedDT; //Apply unclamped velocity to rat
                    }
                }
            }

            Vector3 checkPoint = rat.transform.position + (UnFlattenVector(rat.velocity).normalized * rat.settings.obstacleSeparation); //Get point (projected using velocity) which will be used to check for incoming obstacles
            if (Physics.Linecast(rat.transform.position, checkPoint, out RaycastHit hit, rat.settings.obstructionLayers)) //Rat is heading toward an obstacle it is too close to
            {
                //RULE - Wall Avoidance: (rats will tend to avoid getting too close to walls)
                if (Vector3.Angle(Vector3.up, hit.normal) > MasterRatController.main.settings.maxWalkAngle) //Only avoid true walls (steep floors are fine)
                {
                    float distanceValue = 1 - (Vector2.Distance(FlattenVector(hit.point), rat.flatPos) / rat.settings.obstacleSeparation); //Get value between 0 and 1 which is inversely proportional to distance between rat and wall
                    Vector2 avoidanceVel = FlattenVector(hit.normal) * distanceValue;                                                      //Get velocity which pushes rat away from wall
                    avoidanceVel *= rat.settings.obstacleAvoidanceWeight;                                                                  //Apply weight value to added velocity
                    rat.velocity += avoidanceVel * adjustedDT;                                                                             //Apply unclamped velocity to rat
                }
            }
            else //Rat has no obstacles directly in front of it
            {
                //RULE - Ledge Avoidance: (rats will tend to avoid getting too close to steep ledges)
                float heightCheckDepth = rat.settings.fallHeight + (rat.settings.baseHeight * rat.transform.localScale.x); //Get value used to check for ledges
                if (!Physics.Raycast(checkPoint, Vector3.down, heightCheckDepth, rat.settings.obstructionLayers)) //Rat is moving toward an edge
                {
                    Vector3 depthPoint = checkPoint + (Vector3.down * heightCheckDepth); //Get point at end of line used to check height depth
                    if (Physics.Raycast(depthPoint, -UnFlattenVector(rat.velocity), out hit, rat.settings.obstacleSeparation, rat.settings.obstructionLayers)) //Outside of ledge can be found
                    {
                        float distanceValue = 1 - (Vector2.Distance(FlattenVector(hit.point), rat.flatPos) / rat.settings.obstacleSeparation); //Get value between 0 and 1 which is inversely proportional to distance between rat and ledge
                        Vector2 avoidanceVel = -FlattenVector(hit.normal) * distanceValue;                                                     //Get velocity which pushes rat away from ledge
                        avoidanceVel *= rat.settings.obstacleAvoidanceWeight;                                                                  //Apply weight value to added velocity
                        rat.velocity += avoidanceVel * adjustedDT;                                                                             //Apply unclamped velocity to rat
                    }
                }
            }
        }
    }

    //UTILITY METHODS:
    /// <summary>
    /// Converts given Vector3 to Vector2 (destroying Y value of original vector).
    /// </summary>
    public static Vector2 FlattenVector(Vector3 vector) { return new Vector2(vector.x, vector.z); }
    /// <summary>
    /// Converts given Vector2 to Vector3, assuming Y is zero.
    /// </summary>
    public static Vector3 UnFlattenVector(Vector2 vector) { return new Vector3(vector.x, 0, vector.y); }
    /// <summary>
    /// Returns center of given group of rats.
    /// </summary>
    private static Vector2 GetCenterMass(RatBoid[] ratGroup)
    {
        //Validity checks:
        if (ratGroup.Length == 0) return Vector2.zero; //Return zero if given group is empty

        //Get center of given transforms:
        Vector2 totalPos = Vector2.zero;                           //Initialize container to store total position of all group members
        foreach (RatBoid rat in ratGroup) totalPos += rat.flatPos; //Get total planar position of all rats in group
        return totalPos / ratGroup.Length;                         //Return average position of all given rats
    }
}
