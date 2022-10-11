using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomEnums;

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
    /// <summary>
    /// List of rats not currently targeting any particular object (such as leader).
    /// </summary>
    public static List<RatBoid> freeRats = new List<RatBoid>();
    public static readonly float timeBalancer = 100; //Value applied to all acceleration applications (so that settings aren't messed up by adding deltaTime)

    //Objects & Components:
    private SpriteRenderer r;         //Render component for this rat's sprite
    internal Billboarder billboarder; //Component which rotates this rat's sprite
    public RatSettings settings;      //Settings object determining this rat's properties

    //Realtime Vars:
    /// <summary>
    /// Current speed and direction of boid.
    /// </summary>
    internal Vector2 velocity;
    /// <summary>
    /// Current position of this rat as a vector2.
    /// </summary>
    internal Vector2 flatPos;
    /// <summary>
    /// Current targeting behavior of this rat.
    /// </summary>
    internal RatBehavior behavior = RatBehavior.Free;

    internal List<RatBoid> currentNeighbors = new List<RatBoid>();  //Other rats which are currently close to this rat
    internal List<RatBoid> currentSeparators = new List<RatBoid>(); //Other rats which are currently too close to this rat
    internal float lastTrailValue = -1;                             //Latest value of target on trail (should be reset whenever rat loses target)
    internal float sizeFactor = 1;                                  //Factor used to change certain properties of rat based on scale variance
    internal Vector3 airVelocity;                                   //3D velocity used when rat is falling

    private float timeUntilFlip;      //Time before this rat is able to flip its sprite orientation (prevents jiggling)
    internal float neighborCrush;     //Represents how many neighbors this rat has and how close they are
    internal float pileHeight = 0;    //Additional height added to rat due to piling
    internal float tempBounceMod = 0; //Modifier added to bounciness upon launch, erased upon landing

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        r = GetComponentInChildren<SpriteRenderer>();        //Get renderer from child object
        billboarder = GetComponentInChildren<Billboarder>(); //Get billboarder from child object

        //Initialization:
        spawnedRats.Add(this); //Add this rat to master list of spawned rats
        freeRats.Add(this);    //Every rat initializes as freeroaming

        //Randomize scale:
        float newScale = 1 + Random.Range(-settings.sizeVariance, settings.sizeVariance); //Get value of new scale
        transform.localScale = Vector3.one * newScale;                                    //Set new scale
        sizeFactor = newScale;                                                            //Record size factor

        //Set colors:
        r.material.SetColor("_FurColor", settings.GetFurColor()); //Get and apply random fur color from settings
        Color[] hatColors = settings.GetHatColors();              //Get random hat scheme from settings
        r.material.SetColor("_HatBaseColor", hatColors[0]);       //Apply hat base color
        r.material.SetColor("_HatStripeColor", hatColors[1]);     //Apply hat stripe color
    }
    private void OnDestroy()
    {
        //List cleanup:
        ReleaseTarget();          //Release rat from any lists it may be in
        freeRats.Remove(this);    //Remove rat from freerange list it has just been placed in
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
        //Update rat position:
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            //Initialize:
            Vector3 newPos = rat.transform.position;                         //Get current position of rat
            newPos.x += rat.velocity.x * deltaTime;                          //Add X velocity
            newPos.z += rat.velocity.y * deltaTime;                          //Add Y velocity
            float adjustedHeight = rat.settings.baseHeight * rat.sizeFactor; //Get effective height of rat based on scale
            adjustedHeight += rat.pileHeight;                                //Add pile effect to rat's adjusted height

            //Check new position:
            if (rat.behavior != RatBehavior.Projectile) //Rat is moving across the floor
            {
                //Get floor check value:
                float floorCheckHeight = adjustedHeight;                                                                         //Initialize height value used in floor checks
                if (rat.behavior == RatBehavior.TrailFollower) floorCheckHeight += MasterRatController.main.settings.fallHeight; //Use leader's fall height if applicable
                else floorCheckHeight += rat.settings.fallHeight;                                                                //Otherwise, use rat's natural fall height

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
                    newPos = Vector3.MoveTowards(newPos, hit.point + (Vector3.up * adjustedHeight), rat.settings.heightChangeRate * deltaTime);         //Move target position upward according to height of floor
                    rat.billboarder.targetZRot = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(hit.normal, Vector3.forward), Vector3.forward); //Twist billboard so rat is flat on surface
                    rat.GetLightingFromHit(hit, deltaTime);                                                                                             //Update rat lighting based on light properties of hit surface
                }
                else //No floor found
                {
                    if (Physics.Raycast(fallPoint, -UnFlattenVector(rat.velocity), out hit, rat.velocity.magnitude, rat.settings.obstructionLayers)) //Fall can be prevented
                    {
                        newPos = hit.point + (rat.velocity.magnitude * deltaTime * -hit.normal) + (Vector3.down * floorCheckHeight); //Trace back up over the edge and place rat there
                    }
                    else //Fall cannot be avoided for some reason (very rare)
                    {
                        rat.Launch(new Vector3(rat.velocity.x, 0, rat.velocity.y)); //Launch rat with zero vertical velocity, making it succeptible to gravity until it hits the floor
                    }
                }
            }
            else //Rat is moving through the air
            {
                newPos.y += rat.airVelocity.y * deltaTime; //Add vertical velocity to position calculation (because it was skipped in init because of flat velocity)
                if (Physics.Linecast(rat.transform.position, newPos, out RaycastHit hit, rat.settings.obstructionLayers)) //Rat's trajectory is obstructed
                {
                    float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up); //Get angle of surface relative to flat floor
                    if (surfaceAngle > MasterRatController.main.settings.maxWalkAngle) //Surface is too steep for rat to land on
                    {
                        //Apply force:
                        if (hit.collider.TryGetComponent(out Rigidbody hitBody)) //Hit object has an attached rigidbody
                        {
                            hitBody.AddForceAtPosition(rat.airVelocity * rat.settings.mass, hit.point, ForceMode.Impulse);
                        }

                        //Bounce rat:
                        rat.airVelocity = Vector3.Reflect(rat.airVelocity, hit.normal); //Reflect velocity of rat against surface
                        rat.airVelocity *= rat.settings.bounciness + rat.tempBounceMod; //Retain percentage of velocity depending on setting
                        newPos = hit.point + (-rat.airVelocity.normalized * 0.001f);    //Move rat to position close to wall but not inside it
                    }
                    else //Surface is flat enough for rat to land on
                    {
                        //Land rat:
                        newPos = hit.point + (hit.normal * adjustedHeight);                                     //Set landing position
                        rat.billboarder.SetZRot(-Vector3.SignedAngle(Vector3.up, hit.normal, Vector3.forward)); //Twist billboard so rat is flat on surface
                        rat.Land();                                                                             //Indicate that rat has landed
                    }
                }
                else //Rat is flying freely
                {
                    //Update billboard rotation:
                    float billboardRot = Vector3.SignedAngle(Camera.main.transform.right, rat.airVelocity, Camera.main.transform.forward); //Get angle representing rat velocity relative to camera
                    if (rat.airVelocity.x > 0) billboardRot += 180;                                                                        //Prevent rats traveling along positive X axis from being upside-down
                    rat.billboarder.SetZRot(billboardRot);                                                                                 //Set rotation of rat billboard
                }
            }

            //Cleanup:
            rat.flatPos.x = newPos.x; rat.flatPos.y = newPos.z; //Update flat position tracker
            rat.transform.position = newPos;                    //Move rat to match new position
            rat.currentNeighbors = new List<RatBoid>();         //Clear neighbors list
            rat.currentSeparators = new List<RatBoid>();        //Clear separators list
            rat.neighborCrush = 0;                              //Reset rat's neighbor crush value
        }

        //Establish proximity relationships:
        if (spawnedRats.Count > 1) //Only check for relationships if there is more than one rat
        {
            for (int r = 0; r < spawnedRats.Count; r++) //Iterate through every follower rat (with index variable)
            {
                //Initialize:
                RatBoid rat = spawnedRats[r];                         //Get current rat controller
                if (rat.behavior == RatBehavior.Projectile) continue; //Skip rat if it is a projectile

                //Look for neighbors:
                for (int o = r + 1; o < spawnedRats.Count; o++) //Iterate through list of rats, ignoring already-checked rats and current rat (all other undefined rats)
                {
                    //Initialize:
                    RatBoid otherRat = spawnedRats[o];                         //Get other rat controller
                    if (otherRat.behavior == RatBehavior.Projectile) continue; //Skip rat if it is a projectile
                    
                    //Check distance:
                    float dist = Vector2.Distance(rat.flatPos, otherRat.flatPos); //Get distance between rats
                    if (dist <= settings.neighborRadius) //Rats are close enough to be neighbors
                    {
                        //Update lists:
                        rat.currentNeighbors.Add(otherRat); //Add other rat to neighbors list
                        otherRat.currentNeighbors.Add(rat); //Add this rat to other rat's neighbors list
                        if (dist <= settings.separationRadius) //Rats are close enough to be separators
                        {
                            rat.currentSeparators.Add(otherRat); //Add other rat to separators list
                            otherRat.currentSeparators.Add(rat); //Add this rat to other rat's separators list
                        }

                        //Add crush value:
                        float addCrush = 1 - Mathf.InverseLerp(0, settings.neighborRadius, dist); //Get crush value added by neighbor based on proximity
                        rat.neighborCrush += addCrush;                                            //Add crush value to this rat
                        otherRat.neighborCrush += addCrush;                                       //Add crush value to other rat
                    }
                }
            }
        }

        //RAT RULES:
        float adjustedDT = deltaTime * timeBalancer;                                                                                    //Get adjusted deltaTime to uncouple acceleration changes from framerate
        Vector2 leaderFlatPos = MasterRatController.main.PosAsVector2();                                                                //Get flat position of mama rat
        float leaderSpeedValue = MasterRatController.main.currentSpeed / MasterRatController.main.settings.speed;                       //Get value between 0 and 1 representing main rat's current speed
        float leaderStandingHeight = MasterRatController.main.transform.position.y - MasterRatController.main.settings.collisionRadius; //Approximate height of bottom of leader's sprite
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            //PROJECTILE RULES:
            if (rat.behavior == RatBehavior.Projectile) //Rat is currently behaving as a projectile
            {
                //RULE - Gravity: (rats fall downward)
                Vector3 gravVel = Vector3.down * rat.settings.gravity; //Get acceleration due to gravity
                rat.airVelocity += gravVel * deltaTime;                //Apply velocity to rat

                //RULE - Drag: (the air resists motion)
                Vector3 dragVel = -rat.airVelocity.normalized * rat.settings.drag; //Get deceleration due to drag
                rat.airVelocity += dragVel * deltaTime;                            //Apply velocity to rat

                //Cleanup:
                rat.velocity = FlattenVector(rat.airVelocity); //Update flat velocity to match air velocity
                continue;                                      //DISREGARD ALL OTHER RULES
            }

            //GENERIC RULES:
            if (rat.currentNeighbors.Count != 0) //Rat must have neighbors for these rules to apply
            {
                //RULE - Cohesion: (nearby rats stick together)
                Vector2 localCenterMass = GetCenterMass(rat.currentNeighbors.ToArray());                 //Find center mass of this rat's current neighborhood
                Vector2 cohesionVel = localCenterMass - rat.flatPos;                                     //Get velocity vector representing direction and magnitude of rat separation from center mass
                cohesionVel = (cohesionVel / 100) * settings.cohesionWeight;                             //Apply weight and balancing values to cohesion velocity
                rat.velocity += Vector2.ClampMagnitude(cohesionVel, rat.settings.maxSpeed) * adjustedDT; //Apply clamped velocity to rat

                //RULE - Conformity: (rats tend to match the velocity of nearby rats)
                Vector2 conformVel = Vector2.zero; //Initialize container to store gross conformance-induced velocity
                foreach (RatBoid otherRat in rat.currentNeighbors) //Iterate through list of rats which are close enough to influence this rat
                {
                    conformVel += otherRat.velocity; //Add velocity of rat to local velocity total
                }
                conformVel /= rat.currentNeighbors.Count;                  //Get average velocity of neighbor rats
                conformVel = (conformVel / 8) * settings.conformityWeight; //Apply weight and balancing values to conformance velocity
                if (rat.behavior == RatBehavior.TrailFollower) //Special case for follower rats
                {
                    conformVel *= Mathf.Lerp(settings.stillConformMultiplier, 1, leaderSpeedValue); //Apply a multiplier to conformity value which scales inversely to leader speed
                }
                rat.velocity += Vector2.ClampMagnitude(conformVel, rat.settings.maxSpeed) * adjustedDT; //Apply clamped velocity to rat
            }

            //RULE - Velocity Clamping: (rats cannot go faster than their max speed)
            rat.velocity = Vector2.ClampMagnitude(rat.velocity, rat.settings.maxSpeed); //Clamp rat velocity to maximum speed

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

            if (rat.behavior == RatBehavior.Deployed) //Rat is set aside as deployed
            {
                //DEPLOYMENT RULES:

            }
            else //Rat is not deployed
            {
                //FOLLOWER RULES:
                MasterRatController.TrailPointData data = MasterRatController.main.GetClosestPointOnTrail(rat.transform.position, rat.lastTrailValue, settings.maxTrailSkip * deltaTime); //Get data for closest point to rat position
                float trailDistance = Vector2.Distance(rat.flatPos, data.point);                                                                                                          //Get separation between rat and trail
                if (trailDistance <= MasterRatController.main.settings.influenceRadius) //Rat is within influence radius of leader
                {
                    //Recruitment:
                    rat.lastTrailValue = data.linePosition;                   //Remember line position
                    if (rat.behavior == RatBehavior.Free) rat.MakeFollower(); //Add rat to the swarm if it has just come under the influence of the leader

                    //RULE - Leader Separation: (rats maintain a small distance from mama rat (while mama is standing still))
                    float leaderDistance = Vector2.Distance(rat.flatPos, leaderFlatPos); //Get distance between this rat and mama rat
                    if (leaderDistance <= settings.mamaRadius) //Rat is too close to leader
                    {
                        Vector2 leaderSepVel = (rat.flatPos - leaderFlatPos).normalized; //Get direction that will separate this rat from leader
                        leaderSepVel *= 1 - (leaderDistance / settings.mamaRadius);      //Scale force of separation velocity inversely to proximity between rat and leader
                        leaderSepVel *= 1 - leaderSpeedValue;                            //Scale effect inversely proportionately to how fast leader is moving
                        leaderSepVel *= settings.leaderSeparationWeight;                 //Apply weight setting
                        rat.velocity += leaderSepVel * adjustedDT;                       //Apply unclamped velocity to rat
                    }

                    if (rat.behavior == RatBehavior.TrailFollower) //Rat must be a follower for these rules to apply
                    {
                        //Jumping:
                        foreach (MasterRatController.TrailPoint trailPoint in data.trailPoints) //Iterate through each point in trail adjacent to data point
                        {
                            if (trailPoint.jumpTokens > 0) //Trailpoint is a jump marker
                            {
                                //Expend jump token:
                                trailPoint.jumpTokens--;                                                       //Expend a single jump token
                                if (trailPoint.jumpTokens == 0) MasterRatController.main.currentJumpMarkers--; //Indicate that a jump marker has been fully expended

                                //Determine launch velocity:
                                Vector3 launchVel = new Vector3();                                                                      //Initialize launch velocity at rat's current speed
                                launchVel.y = MasterRatController.main.settings.jumpPower.y;                                            //Give rat vertical jump power of leader
                                Vector2 horizontalLaunchVel = data.forward * MasterRatController.main.settings.jumpPower.x;             //Get isolated horizontal jump power from leader
                                horizontalLaunchVel *= 1 + Random.Range(-rat.settings.jumpRandomness.x, rat.settings.jumpRandomness.x); //Apply randomness to horizontal velocity
                                launchVel.y *= 1 + Random.Range(-rat.settings.jumpRandomness.y, rat.settings.jumpRandomness.y);         //Apply randomness to vertical velocity
                                launchVel += UnFlattenVector(horizontalLaunchVel);                                                      //Apply horizontal component to net launch velocity
                                launchVel *= 1 + settings.followerJumpBoost;                                                            //Apply jump boost based on settings

                                //Cleanup:
                                rat.Launch(launchVel, true); //Apply launch velocity (maintaining follower status)
                                continue;                    //Cancel all other checks
                            }
                        }

                        //RULE - Targeting: (rats tend to move toward target)
                        float targetingValue = Mathf.Min(1, trailDistance / settings.targetRadius); //Get value (capped at 1) representing how far rat is from target
                        Vector2 targetVel = (data.point - rat.flatPos).normalized;                  //Get direction toward target
                        targetVel *= 0.01f * settings.targetWeight * targetingValue;                //Apply weight and balancing values to target velocity
                        rat.velocity += targetVel * adjustedDT;                                     //Apply velocity to rat

                        //RULES - TRAIL GUIDANCE:
                        if (trailDistance <= settings.leadRadius) //Rat is within trail leadership radius
                        {
                            //Rule prep:
                            float multiplier = Mathf.Min(1, data.linePosition / settings.trailBuffer);                                    //Initialize general multiplier as value which goes down the farther ahead of the line this rat is
                            if (!(Vector2.Angle(MasterRatController.main.forward, data.forward) < 180 - settings.maxSegAngle)             //Leader is backtracking relative to this rat...
                                && data.linePosition < settings.backtrackBuffer) multiplier = 0;                                          //...and rat is fairly close to leader along trail, prevent following in this case
                            multiplier *= Mathf.InverseLerp(settings.compressionRange.y, settings.compressionRange.x, rat.neighborCrush); //Penalize overcrowding smoothly within density range

                            //RULE - Dispersion: (rats on a trail will tend to move along it toward the leader, stabilizing at target amount of compression)
                            Vector2 followVel = data.forward;              //Get follow velocity from forward direction of trail
                            followVel *= 0.1f * settings.dispersionWeight; //Apply weight and balancing values to follow velocity
                            followVel *= multiplier;                       //Apply general multiplier
                            rat.velocity += followVel * adjustedDT;        //Apply unclamped velocity to rat

                            //RULE - Leading: (rats on a trail will move along it when leader is moving)
                            Vector2 leadVel = data.forward * MasterRatController.main.currentSpeed; //Get lead velocity from forward direction of trail and speed of leader
                            leadVel *= 0.1f * settings.leadWeight;                                  //Apply weight value and balancing values to lead velocity
                            leadVel *= multiplier;                                                  //Apply general multiplier
                            rat.velocity += leadVel * adjustedDT;                                   //Apply unclamped velocity to rat
                        }

                        //RULES - END RETENTION:
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

                        //RULE - Max Follow Speed: (rats cannot move too fast relative to their leader)
                        rat.velocity = Vector2.ClampMagnitude(rat.velocity, MasterRatController.main.currentSpeed + settings.maxOvertakeSpeed); //Clamp rat velocity to effective max speed
                    }
                }
                else //Rat is outside influence radius of leader
                {
                    //Dischargement:
                    if (rat.behavior == RatBehavior.TrailFollower) rat.ReleaseTarget(); //Release from leader if rat has strayed too far away
                }
            }

            //SPATIAL RULES:
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
                float heightCheckDepth = rat.settings.baseHeight * rat.sizeFactor;                                               //Get value used to check for ledges
                if (rat.behavior == RatBehavior.TrailFollower) heightCheckDepth += MasterRatController.main.settings.fallHeight; //Apply leader's fall height if being lead
                else heightCheckDepth += rat.settings.fallHeight;                                                                //Otherwise, use rat's natural fall height
                if (!Physics.Raycast(checkPoint, Vector3.down, heightCheckDepth, rat.settings.obstructionLayers)) //Rat is moving toward an edge
                {
                    Vector3 depthPoint = checkPoint + (Vector3.down * heightCheckDepth); //Get point at end of line used to check height depth
                    if (Physics.Raycast(depthPoint, -UnFlattenVector(rat.velocity), out hit, rat.settings.obstacleSeparation, rat.settings.obstructionLayers)) //Outside of ledge can be found
                    {
                        if (rat.transform.position.y - (rat.pileHeight + (rat.settings.baseHeight * rat.sizeFactor)) >= leaderStandingHeight + rat.settings.fallHeight) //Rat is above leader and is being pushed against a ledge
                        {
                            //Get jump velocity:
                            Vector3 launchVel = new Vector3();                                                    //Initialize launch velocity at zero
                            launchVel.y = rat.settings.autoJumpPower.y;                                           //Use rat's default vertical jump power
                            launchVel += UnFlattenVector(rat.velocity.normalized * rat.settings.autoJumpPower.x); //Align rat's horizontal jump power with normal of cliff (making sure to use flat normal of cliff)

                            //Cleanup:
                            rat.Launch(launchVel); //Apply launch velocity
                            continue;              //Cancel all other checks
                        }
                        float distanceValue = 1 - (Vector2.Distance(FlattenVector(hit.point), rat.flatPos) / rat.settings.obstacleSeparation); //Get value between 0 and 1 which is inversely proportional to distance between rat and ledge
                        Vector2 avoidanceVel = -FlattenVector(hit.normal) * distanceValue;                                                     //Get velocity which pushes rat away from ledge
                        avoidanceVel *= rat.settings.obstacleAvoidanceWeight;                                                                  //Apply weight value to added velocity
                        rat.velocity += avoidanceVel * adjustedDT;                                                                             //Apply unclamped velocity to rat
                        //rat.neighborCrush *= 1 - distanceValue;                                                                                //Prevent rats from piling up against a ledge
                    }
                }
            }

            //Crush effects:
            if (rat.neighborCrush >= rat.settings.crushRange.x) //Rat currently has enough neighbors to be occluded
            {
                //Initialization:
                float crushValue = Mathf.Clamp01(Mathf.InverseLerp(rat.settings.crushRange.x, rat.settings.crushRange.y, rat.neighborCrush)); //Get interpolant value representing intensity of crush

                //Occlusion:
                float occlusionValue = Mathf.Lerp(0, rat.settings.occlusionIntensity, crushValue);                                                                                      //Get occlusion value based on crush condition and curve settings
                Color occlusionColor = Color.Lerp(rat.settings.occlusionColors.colorA, rat.settings.occlusionColors.colorB, rat.settings.occlusionColorCurve.Evaluate(occlusionValue)); //Get color used for rat occlusion based on crush amount and color curve
                occlusionValue = rat.settings.occlusionCurve.Evaluate(occlusionValue);
                rat.r.material.SetFloat("_OcclusionIntensity", occlusionValue);                                                                                                         //Apply occlusion intensity to shader
                rat.r.material.SetColor("_OcclusionColor", occlusionColor);                                                                                                             //Apply occlusion color to shader

                //Piling:
                rat.pileHeight = Mathf.Lerp(0, rat.settings.maxPileHeight, rat.settings.pileCurve.Evaluate(crushValue)); //Determine how much extra height this rat gets from piling (smoothly move toward value)
            }
            else //Rat does not have enough neighbors to be occluded
            {
                rat.r.material.SetFloat("_OcclusionIntensity", 0); //Ensure rat is not occluded
                rat.pileHeight = 0;                                //Set pile height to zero
            }
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Releases rat from target it is currently following, initiating freeroam behavior.
    /// </summary>
    public void ReleaseTarget()
    {
        switch (behavior) //Determine procedure based on current targeting status
        {
            case RatBehavior.Free: return; //Ignore if rat is not currently targeting anything
            case RatBehavior.Projectile: //Rat is currently a projectile
                break;
            case RatBehavior.TrailFollower: //Rat is currently a follower of the mama rat
                if (MasterRatController.main.followerRats.Contains(this)) //Leader has this rat as a follower
                {
                    MasterRatController.main.followerRats.Remove(this); //Remove rat from master follower list
                    MasterRatController.main.OnFollowerCountChanged();  //Update rat swarm settings
                }
                break;
            case RatBehavior.Deployed:
                if (MasterRatController.main.deployedRats.Contains(this)) //This rat is currently deployed
                {
                    MasterRatController.main.deployedRats.Remove(this); //Remove rat from deployed rats list
                }
                break;
            case RatBehavior.Distracted: //Rat is currently targeting a generic object

                break;
        }

        //Cleanup:
        if (!freeRats.Contains(this)) freeRats.Add(this); //Add rat to list of freeroaming rats
        lastTrailValue = -1;                              //Clear rat's memory of target trail
        behavior = RatBehavior.Free;                      //Indicate that rat is now freeroaming
    }
    /// <summary>
    /// Makes rat a follower, adding it to main rat swarm.
    /// </summary>
    public void MakeFollower()
    {
        if (!MasterRatController.main.followerRats.Contains(this)) //Rat is not already a follower
        {
            MasterRatController.main.followerRats.Add(this); //Add rat to master list of followers
            MasterRatController.main.OnFollowerCountChanged();  //Adjust swarm settings in accordance with new follower quantity
        }
        if (MasterRatController.main.deployedRats.Contains(this)) MasterRatController.main.deployedRats.Remove(this); //Remove rat from list of deployed rats if applicable
        if (freeRats.Contains(this)) freeRats.Remove(this);                                                           //Remove rat from list of free rats

        //Cleanup:
        behavior = RatBehavior.TrailFollower; //Indicate that rat is a follower
    }
    /// <summary>
    /// Makes rat go to deployed position (according to leader pointing location)
    /// </summary>
    public void MakeDeployed()
    {
        if (!MasterRatController.main.deployedRats.Contains(this)) //Rat is not already deployed
        {
            MasterRatController.main.deployedRats.Add(this); //Add rat to master list of deployed rats
            MasterRatController.main.OnFollowerCountChanged();  //Adjust swarm settings in accordance with new follower quantity
        }
        if (MasterRatController.main.followerRats.Contains(this)) MasterRatController.main.followerRats.Remove(this); //Remove rat from list of follower rats if applicable
        if (freeRats.Contains(this)) freeRats.Remove(this);                                                           //Remove rat from list of free rats
        behavior = RatBehavior.Deployed;                                                                          //Indicate that rat is deployed to point
    }
    /// <summary>
    /// Launches rat into the air.
    /// </summary>
    /// <param name="force">Direction and power with which rat will be launched.</param>
    /// <param name="keepFollower">If true, follower will be kept in a separate list until landing and counted towards follower total.</param>
    public void Launch(Vector3 force, bool keepFollower = false)
    {
        //Behavior maintenance:
        if (behavior == RatBehavior.TrailFollower && keepFollower) //Rat is going to be kept on a separate list of aerial followers
        {
            if (!MasterRatController.main.jumpingFollowers.Contains(this)) MasterRatController.main.jumpingFollowers.Add(this); //Add this rat to a special list of airborne followers
        }
        ReleaseTarget();                   //Release this rat from its current target (taking it off of other lists)
        behavior = RatBehavior.Projectile; //Indicate that this rat is now a proper projectile

        //Modify velocity:
        velocity = Vector2.zero; //Zero out normal velocity
        airVelocity = force;     //Apply launch force to velocity

        //Cleanup:
        lastTrailValue = -1;                           //Reset trail value
        r.material.SetFloat("_OcclusionIntensity", 0); //Ensure rat is not darkened
        r.material.SetFloat("_ShadowIntensity", 0);    //Clear rat shadow value
        pileHeight = 0;                                //Reset rat's pile value
    }
    /// <summary>
    /// Called when a launched rat hits a surface and does not bounce off of it.
    /// </summary>
    public void Land()
    {
        //Check for ownership:
        behavior = RatBehavior.Free;                                                                                          //Indicate that rat is now free (should already be released from target lists)
        if (MasterRatController.main.jumpingFollowers.Contains(this)) MasterRatController.main.jumpingFollowers.Remove(this); //Remove rat from jumping follower list if applicable

        //Cleanup:
        tempBounceMod = 0;          //Reset bounce modifier
        lastTrailValue = -1;        //Reset trail value (just to be sure)
        airVelocity = Vector3.zero; //Clear air velocity (but keep momentum by retaining flat velocity)
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
    /// <summary>
    /// Dynamically updates rat's coloration based on baked shadows of given surface hit.
    /// </summary>
    public void GetLightingFromHit(RaycastHit hit, float deltaTime)
    {
        //Validity checks:
        if (!hit.collider.gameObject.isStatic) return;                                             //Ignore if object is static
        if (!hit.collider.TryGetComponent(out MeshRenderer mr)) return;                            //Ignore if object does not have a meshRenderer
        if (mr.lightmapIndex < 0 || mr.lightmapIndex >= LightmapSettings.lightmaps.Length) return; //Ignore if object does not have an associated lightmap

        //Initialize:
        LightmapData lightMap = LightmapSettings.lightmaps[mr.lightmapIndex]; //Get lightmap data of object rat is touching
        Texture2D colorMap = lightMap.lightmapColor;                          //Get colored lightmap texture
        Texture2D dirMap = lightMap.lightmapDir;                              //Get directional lightmap texture

        //Update shadow intensity:
        if (colorMap.isReadable) //Only scan pixels if lightmap is read/write enabled
        {
            Vector2Int pixelCoords = new Vector2Int(Mathf.RoundToInt(hit.lightmapCoord.x * colorMap.width), Mathf.RoundToInt(hit.lightmapCoord.y * colorMap.height)); //Pixel coordinates of point on lightmap rat is touching
            float darkness = 1 - colorMap.GetPixel(pixelCoords.x, pixelCoords.y).grayscale;                                       //Get relative darkness of point rat is standing on
            darkness = settings.shadowSensitivityCurve.Evaluate(darkness);                                                        //Make shadow intensity relative to settings
            darkness = Mathf.MoveTowards(darkness, r.material.GetFloat("_ShadowIntensity"), settings.maxShadowDelta * deltaTime); //Smoothly move from current darkness to target
            r.material.SetFloat("_ShadowIntensity", darkness);                                                                    //Apply new darkness value
        }
        else //Lightmap is not read/write enabled
        {
            Debug.LogWarning("Failed to reference lightmap at index" + mr.lightmapIndex + ", make sure baked lightmaps are manually set to Read/Write"); //Post warning indicating problem
        }

        //Update lighting direction:
        /*if (dirMap.isReadable) //Only scan pixels if directional lightmap is read/write enabled
        {
            Vector2Int pixelCoords = new Vector2Int(Mathf.RoundToInt(hit.lightmapCoord.x * dirMap.width), Mathf.RoundToInt(hit.lightmapCoord.y * dirMap.height)); //Pixel coordinates of point on directionMap rat is touching
            Color dirColor = dirMap.GetPixel(pixelCoords.x, pixelCoords.y).linear;                                                                                //Get color representing direction of lights at point
            //NOTE: Figure out how to allow for negatives
            Vector3 lightDirection = new Vector3(dirColor.b - 0.5f, dirColor.g - 0.5f, dirColor.r - 0.5f).normalized;
            print(lightDirection);
            Debug.DrawRay(hit.point, lightDirection, dirColor);
        }
        else //Directional lightmap is not read/write enabled
        {
            Debug.LogWarning("Failed to reference directional lightmap at index" + mr.lightmapIndex + ", make sure baked lightmaps are manually set to Read/Write"); //Post warning indicating problem
        }*/
    }
}
