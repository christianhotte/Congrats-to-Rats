using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;
using CustomEnums;

/// <summary>
/// Controls the big rat and governs behavior of all the little rats.
/// </summary>
public class MasterRatController : MonoBehaviour
{
    //Classes, Enums & Structs:
    /// <summary>
    /// A point on rat's follower trail.
    /// </summary>
    public class TrailPoint
    {
        /// <summary>
        /// Position of trail point.
        /// </summary>
        public Vector2 point;
        /// <summary>
        /// Distance between this trail point and the one behind it (if applicable).
        /// </summary>
        public float segLength = 0; //Segment length will be set externally
        /// <summary>
        /// If above zero, trail point is treated as a jump marker. Ticks down by one for each ratBoid which uses this to jump.
        /// </summary>
        public int jumpTokens = 0; 

        //OPERATION METHODS:
        public TrailPoint (Vector2 _point)
        {
            //Get data:
            point = _point; //Set point vector
        }
    }
    /// <summary>
    /// Contains information regarding a point on the follower trail.
    /// </summary>
    public struct TrailPointData
    {
        /// <summary>
        /// Position of trail point.
        /// </summary>
        public Vector2 point;
        /// <summary>
        /// Vector pointing foward along trail.
        /// </summary>
        public Vector2 forward;
        /// <summary>
        /// Value between 0 and 1 representing where in the line this point is.
        /// </summary>
        public float linePosition;
        /// <summary>
        /// The two TrailPoints which the given point is inbetween.
        /// </summary>
        public TrailPoint[] trailPoints;

        public TrailPointData(Vector2 position, TrailPoint[] points)
        {
            this.point = position;       //Set point vector
            this.trailPoints = points;   //Get references to pertinent trail points
            this.forward = Vector2.zero; //Indicate that point has no implied direction
            this.linePosition = 0;       //Set line value to zero (assume point is at head of line)
        }
        public TrailPointData(Vector2 position, TrailPoint[] points, Vector2 direction, float lineValue)
        {
            this.point = position;         //Set point vector
            this.trailPoints = points;     //Get references to pertinent trail points
            this.forward = direction;      //Set direction vector
            this.linePosition = lineValue; //Set line value
        }
    }

    //Static Stuff:
    /// <summary>
    /// Singleton instance of Big Rat in scene.
    /// </summary>
    public static MasterRatController main;

    //Objects & Components:
    private SpriteRenderer sprite;   //Sprite renderer component for big rat
    private Animator anim;           //Animator controller for big rat
    private Billboarder billboarder; //Component used to manage sprite orientation

    [Header("Settings:")]
    [Tooltip("Interchangeable data object describing settings of the main rat")]                                                    public BigRatSettings settings;
    [SerializeField, Tooltip("Place any number of swarm settings objects here (make sure they have different Target Rat Numbers)")] private List<SwarmSettings> swarmSettings = new List<SwarmSettings>();

    //Runtime Vars:
    private SwarmSettings currentSwarmSettings;                    //Instance of swarmSettings object used to interpolate between rat behaviors
    internal List<RatBoid> followerRats = new List<RatBoid>();     //List of all rats currently following this controller
    internal List<RatBoid> jumpingFollowers = new List<RatBoid>(); //List of follower rats which are currently jumping (and therefore still counted toward total)
    internal List<RatBoid> deployedRats = new List<RatBoid>();     //List of all rats currently deployed by player
    private List<TrailPoint> trail = new List<TrailPoint>();       //List of points in current trail (used to assemble ratswarm behind main rat)
    internal float totalTrailLength = 0;                           //Current length of trail (in units)
    internal int currentJumpMarkers = 0;                           //Current number of jump markers in trail

    internal Vector2 velocity;          //Current speed and direction of movement
    internal Vector3 airVelocity;       //3D velocity used when rat is falling
    internal Vector2 forward;           //Normalized vector representing which direction big rat was most recently moving
    internal float currentSpeed;        //Current speed at which rat is moving
    private Vector2 moveInput;          //Current input direction for movement
    private bool falling;               //Whether or not rat is currently falling
    internal bool commanding;           //Whether or not rat is currently deploying rats to a location
    private bool aiming;                //True when rat is preparing for a throw
    private Vector3 currentMouseTarget; //Current position in real space which mouse is pointing at

    //Utility Vars:
    /// <summary>
    /// Quantity of rats currently treated as followers.
    /// </summary>
    public int totalFollowerCount { get { return followerRats.Count + jumpingFollowers.Count; } }
    /// <summary>
    /// Current position of this rat projected onto world Y axis.
    /// </summary>
    public Vector2 flatPos { get { return new Vector2(transform.position.x, transform.position.z); } }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Validity checks:
        if (settings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); }                   //Make sure big rat has ratSettings
        if (swarmSettings.Count == 0) { Debug.LogError("Big rat needs at least one swarmSettings object"); Destroy(this); } //Make sure rat swarm has settings
        if (main == null) main = this; else Destroy(this);                                                                  //Singleton-ize this script instance

        //Global initialization:
        Application.targetFrameRate = 120; //Set target framerate

        //Local initialization:
        trail.Insert(0, new TrailPoint(flatPos)); //Add starting position as first point in trail
        OnFollowerCountChanged();                 //Set up swarm settings and do initial update
        MoveRat(0);                               //Snap rat to floor
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>();   //Get spriteRenderer component
        anim = GetComponentInChildren<Animator>();           //Get animator controller component
        billboarder = GetComponentInChildren<Billboarder>(); //Get billboarder component
    }
    private void Update()
    {
        MoveRat(Time.deltaTime);                                  //Move the big rat
        RatBoid.UpdateRats(Time.deltaTime, currentSwarmSettings); //Move all the little rats

        OnFollowerCountChanged(); //TEMP: Keep swarm settings regularly up-to-date for debugging purposes

        //Visualize trail:
        if (trail.Count > 1)
        {
            for (int i = 1; i < trail.Count; i++)
            {
                Vector3 p1 = new Vector3(trail[i].point.x, 0.1f, trail[i].point.y);
                Vector3 p2 = new Vector3(trail[i - 1].point.x, 0.1f, trail[i - 1].point.y);
                Debug.DrawLine(p1, p2, trail[i].jumpTokens > 0 || trail[i - 1].jumpTokens > 0 ? Color.yellow : Color.blue);
            }
        }
    }

    //UPDATE METHODS:
    /// <summary>
    /// Moves the big rat according to current velocity.
    /// </summary>
    private void MoveRat(float deltaTime)
    {
        //Initialize:
        Vector3 newPos = transform.position; //Get current position of rat

        //Modify velocity:
        if (falling) //Rat is currently falling through the air
        {
            //Modify velocity:
            Vector3 addVel = new Vector3();                                                      //Initialize vector to store acceleration
            addVel += settings.accel * settings.airControl * RatBoid.UnFlattenVector(moveInput); //Get acceleration due to input
            addVel += Vector3.down * settings.gravity;                                           //Get acceleration due to gravity
            addVel += -airVelocity.normalized * settings.airDrag;                                //Get deceleration due to drag
            airVelocity += addVel * deltaTime;                                                   //Apply change in velocity
            velocity = RatBoid.FlattenVector(airVelocity);                                       //Update flat velocity to match air velocity

            //Get new position:
            newPos += airVelocity * deltaTime; //Get target position based on velocity over time
            float airSpeed = Vector3.Distance(transform.position, newPos); //Get current airspeed of rat
            if (Physics.SphereCast(transform.position, settings.collisionRadius, (newPos - transform.position).normalized, out RaycastHit hit, airSpeed, settings.blockingLayers)) //Fall is obstructed
            {
                float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up); //Get angle of surface relative to flat floor
                if (surfaceAngle > settings.maxWalkAngle) //Surface is too steep for rat to land on
                {
                    //Bounce:
                    airVelocity = Vector3.Reflect(airVelocity, hit.normal);                                            //Reflect velocity of rat against surface
                    airVelocity *= settings.bounciness;                                                                //Retain percentage of velocity depending on setting
                    if (airVelocity.magnitude < settings.wallRepulse) airVelocity = hit.normal * settings.wallRepulse; //Make sure bounce has at least a little velocity so rat doesn't get stuck
                    newPos = transform.position;                                                                       //Do not allow rat to move into wall
                }
                else //Surface is flat enough for rat to land on
                {
                    //Land rat:
                    newPos = hit.point + (hit.normal * (settings.collisionRadius + 0.001f));            //Set landing position
                    billboarder.SetZRot(-Vector3.SignedAngle(Vector3.up, hit.normal, Vector3.forward)); //Twist billboard so rat is flat on surface

                    //Landing cleanup:
                    falling = false;            //Indicate that rat is no longer falling
                    airVelocity = Vector3.zero; //Cancel all air velocity
                }
            }
        }
        else //Rat is moving normally along a surface
        {
            //Modify velocity:
            if (moveInput != Vector2.zero && !aiming) //Player is moving rat in a direction (and not aiming)
            {
                //Add velocity:
                Vector2 addVel = moveInput * settings.accel; //Get added velocity based on input this frame
                velocity += addVel * deltaTime;              //Add velocity as acceleration over time

                //Flip sprite:
                if (moveInput.x != 0) sprite.flipX = settings.flipAll ? moveInput.x < 0 : moveInput.x > 0; //Flip sprite to direction of horizontal move input
            }
            else if (velocity != Vector2.zero) //No input is given but rat is still moving
            {
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, settings.decel * deltaTime); //Slow rat down based on deceleration over time
            }

            //Cap velocity:
            currentSpeed = velocity.magnitude; //Get current speed of main rat
            if (currentSpeed > settings.speed) //Check if current speed is faster than target speed
            {
                velocity = Vector2.ClampMagnitude(velocity, settings.speed); //Clamp velocity to target speed
                currentSpeed = settings.speed;                               //Update current speed value
            }
            if (anim != null) anim.SetFloat("Speed", currentSpeed / settings.speed); //Send speed value to animator

            //Get new position:
            if (velocity != Vector2.zero) //Only update position if player has velocity
            {
                //Initialize:
                newPos.x += velocity.x * deltaTime; //Add X velocity over time to position target
                newPos.z += velocity.y * deltaTime; //Add Y velocity over time to position target

                //Solve obstructions:
                Vector3 castDir = RatBoid.UnFlattenVector(velocity); //Create container to store direction of spherecasts, initialized based on velocity
                float castDist = currentSpeed * Time.deltaTime;      //Create container to store distance of spherecasts, initialized based on speed
                bool touchingFloor = false;                          //Initialize bool to confirm whether or not floor has been found
                for (int i = 0; i < settings.maxObstacleCollisions; i++) //Iterate collision check for, at most, maximum allowed number of obstacle collisions
                {
                    if (Physics.SphereCast(transform.position, settings.collisionRadius, castDir, out RaycastHit hit, castDist, settings.blockingLayers)) //Rat is currently moving into an obstacle
                    {
                        //Get obstructed position:
                        Vector3 idealPos = newPos;                                               //Get position rat would be at if not obstructed
                        newPos = hit.point + ((settings.collisionRadius + 0.001f) * hit.normal); //Get position at center of sphere colliding with obstruction
                        newPos += Vector3.ProjectOnPlane(idealPos - newPos, hit.normal);         //Add remainder of velocity by projecting change in position onto plane defined by hit normal
                    
                        //Check for floor:
                        if (Vector3.Angle(Vector3.up, hit.normal) <= settings.maxWalkAngle) //Obstruction is recognized as a FLOOR
                        {
                            GetComponentInChildren<Billboarder>().targetZRot = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(hit.normal, Vector3.forward), Vector3.forward); //Twist billboard so rat is flat on surface
                            touchingFloor = true;                                                                                                                                     //Indicate that floor has been found prematurely
                        }
                        else //Obstruction is recognized as a WALL
                        {
                            newPos.y = idealPos.y; //Prevent walls from lifting the player (by carrying over Y value from floors or original transform position)
                        }

                        //Prep for next cast:
                        castDir = newPos - transform.position; //Update direction of cast
                        castDist = castDir.magnitude;          //Update distance of cast
                    }
                    else break; //End collision checking once rat is no longer obstructed
                }
                if (Physics.CheckSphere(newPos, settings.collisionRadius, settings.blockingLayers)) //Rat is still colliding with something (collision avoidance has failed)
                {
                    newPos = transform.position; //Cancel entire movement
                }

                //Stick to floor:
                if (!touchingFloor) //Floor was not registered as an obstruction
                {
                    if (Physics.SphereCast(newPos, settings.collisionRadius, Vector3.down, out RaycastHit hit, settings.fallHeight, settings.blockingLayers)) //Rat has a floor under it
                    {
                        newPos = hit.point + ((settings.collisionRadius + 0.001f) * hit.normal);                                                                                  //Modify position to stick to found floor
                        GetComponentInChildren<Billboarder>().targetZRot = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(hit.normal, Vector3.forward), Vector3.forward); //Twist billboard so rat is flat on surface
                        touchingFloor = true;                                                                                                                                     //Indicate that rat is now touching floor
                    }
                    else //Rat is hovering above empty space
                    {
                        Launch(RatBoid.UnFlattenVector(velocity), false); //Allow rat to fall
                    }
                    if (!Physics.Raycast(transform.position, Vector3.down, settings.fallHeight + settings.collisionRadius, settings.blockingLayers)) //Check for floor directly below rat
                    {
                        Launch(new Vector3(velocity.x, settings.cliffHop, velocity.y), false); //Bump rat off cliff
                    }
                }
            }
        }

        //Movement cleanup:
        currentSpeed = Vector2.Distance(flatPos, RatBoid.FlattenVector(newPos)) / deltaTime; //Get actual current speed (after obstructions)
        transform.position = newPos;                                                         //Apply new position
        forward = velocity.normalized;                                                       //Update forward direction tracker

        //Update trail characteristics:
        trail.Insert(0, new TrailPoint(flatPos));                                //Add new trailPoint for current position
        float firstSegLength = Vector2.Distance(trail[0].point, trail[1].point); //Get length of new segment being created
        totalTrailLength += firstSegLength;                                      //Add length of new segment to total length of trail
        if (trail.Count > 1) trail[0].segLength = firstSegLength;                //Store length of new segment if applicable
        if (trail.Count > 2) //Only perform culling operations if trail is long enough
        {
            //Grow first segment to minimum length:
            float secondSegLength = Vector2.Distance(trail[1].point, trail[2].point); //Get length of second segment in trail
            if (secondSegLength < currentSwarmSettings.minTrailSegLength) //Check if second segment in trail is too short (first segment can be any length)
            {
                totalTrailLength -= firstSegLength + secondSegLength;              //Subtract lengths of both removed segments from total
                trail[0].jumpTokens += trail[1].jumpTokens;                        //Pass jump tokens to the new point when merging segments
                trail.RemoveAt(1);                                                 //Remove second segment from trail
                firstSegLength = Vector2.Distance(trail[0].point, trail[1].point); //Get new length of first segment
                totalTrailLength += firstSegLength;                                //Add length of new segment back to total
            }
            else //A new trail point has been permanently created
            {
                trail[1].segLength = secondSegLength; //Store length of second segment now that it is official
            }
            trail[0].segLength = firstSegLength; //Keep first segment in list constantly updated

            //Limit trail length:
            float targetTrailLength = (1 / currentSwarmSettings.trailDensity) * (totalFollowerCount + 0.01f);                 //Get target trail length based off of follower count and settings
            targetTrailLength *= Mathf.Lerp(1, currentSwarmSettings.velTrailLengthMultiplier, currentSpeed / settings.speed); //Apply velocity-based length multiplier to target trail length
            while (totalTrailLength > targetTrailLength) //Current trail is longer than target length (and is non-zero)
            {
                float extraLength = totalTrailLength - targetTrailLength;                 //Get amount of extra length left in trail
                float lastSegLength = Vector2.Distance(trail[^1].point, trail[^2].point); //Get distance between last two segments in trail NOTE: this distance check may not be needed
                if (extraLength >= lastSegLength) //Last segment is shorter than length which needs to be removed
                {
                    if (trail[^1].jumpTokens > 0) currentJumpMarkers = Mathf.Max(0, currentJumpMarkers - 1); //Check for a removed jump marker
                    trail.RemoveAt(trail.Count - 1);                                                         //Remove last segment from trail
                    totalTrailLength -= lastSegLength;                                                       //Subtract length of removed segment from total
                    trail[^1].segLength = 0;                                                                 //Delete now-unnecessary segment length of final point in trail
                }
                else //Last segment is longer than length which needs to be removed
                {
                    trail[^1].point = Vector2.MoveTowards(trail[^1].point, trail[^2].point, extraLength); //Shorten last segment by extra length
                    trail[^2].segLength -= extraLength;                                                   //Subtract extra length from last segment length tracker
                    totalTrailLength -= extraLength;                                                      //Subtract remaining extra length from total
                }
            }

            //Check for kinks in line:
            while (trail.Count > 2 && Vector2.Angle(trail[1].point - trail[0].point, trail[1].point - trail[2].point) < 180 - currentSwarmSettings.maxSegAngle) //Trail is kinked (and contains more than one segment)
            {
                //Fuse first two segments:
                totalTrailLength -= trail[0].segLength + trail[1].segLength;                            //Remove deleted segment lengths from total trail length
                if (trail[1].jumpTokens > 0) currentJumpMarkers = Mathf.Max(0, currentJumpMarkers - 1); //Check for a removed jump marker
                trail.RemoveAt(1);                                                                      //Remove second point from trail (combining first and second segments)
                trail[0].segLength = Vector2.Distance(trail[0].point, trail[1].point);                  //Update new length of first segment
                totalTrailLength += trail[0].segLength;                                                 //Add new segment length to total
            }
        }
    }

    //INPUT METHODS:
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); //Store input value
    }
    public void OnScrollSpawn(InputAction.CallbackContext context)
    {
        if (context.started) //Scroll wheel has just been moved one tick
        {
            if (context.ReadValue<float>() > 0) SpawnRat(settings.basicRatPrefab); //Spawn rats when wheel is scrolled up
            else if (followerRats.Count > 0) Destroy(followerRats[^1].gameObject); //Despawn rats when wheel is scrolled down
        }
    }
    public void OnCommandInput(InputAction.CallbackContext context)
    {
        if (context.performed) //Command button has been pressed
        {
            commanding = true;              //Indicate that rat is now in command mode
            anim.SetBool("Pointing", true); //Execute pointing animation
        }
        else //Command button has been released
        {
            commanding = false;              //Indicate that rat is no longer commanding
            anim.SetBool("Pointing", false); //End pointing animation
        }
            
    }
    public void OnThrowInput(InputAction.CallbackContext context)
    {
        if (context.performed && followerRats.Count > 0) //Throw button has just been pressed (and there is at least one rat to throw)
        {
            aiming = true;                //Indicate that rat is now aiming
            anim.SetBool("Aiming", true); //Play aim animation
        }
        else if (aiming) //Throw button has just been released while aiming
        {
            aiming = false;                //Indicate that rat is no longer aiming
            anim.SetBool("Aiming", false); //Play throw animation
            if (followerRats.Count > 0)
            {
                RatBoid throwRat = followerRats[0];
                throwRat.transform.position = transform.position;
                throwRat.tempBounceMod = -0.7f;
                throwRat.Launch((currentMouseTarget - transform.position).normalized * settings.throwForce);
            }
        }
    }
    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed) //Jump button has just been pressed
        {
            if (!falling) //Player can only jump while they are not in the air
            {
                if (currentJumpMarkers < currentSwarmSettings.maxJumpMarkers) //Only allow a jump to be performed if max number is not already exceeded
                {
                    Vector3 jumpforce = RatBoid.UnFlattenVector(moveInput).normalized * settings.jumpPower.x; //Get horizontal jump power
                    jumpforce.y = settings.jumpPower.y;                                                       //Get vertical jump power
                    if (moveInput == Vector2.zero) jumpforce.y *= settings.stationaryJumpMultiplier;          //Apply multiplier to vertical jump if rat is stationary
                    Launch(jumpforce);                                                                        //Launch rat using jump force
                }
            }
        }
    }
    public void OnMousePositionMove(InputAction.CallbackContext context)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(context.ReadValue<Vector2>());
        if (Physics.Raycast(mouseRay, out RaycastHit hit))
        {
            currentMouseTarget = hit.point;
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Spawns a new rat and adds it to the swarm.
    /// </summary>
    public void SpawnRat(GameObject prefab)
    {
        //Initialize:
        Transform newRat = Instantiate(prefab).transform;       //Spawn new rat
        RatBoid ratController = newRat.GetComponent<RatBoid>(); //Get controller from spawned rat

        //Get spawn position:
        Vector3 spawnDirection = new Vector3(Random.Range(-settings.spawnArea.x / 2, settings.spawnArea.x / 2), 0, //Get vector which moves spawnpoint away from center by a random amount
                                             Random.Range(-settings.spawnArea.y / 2, settings.spawnArea.y / 2));   //Add random amount to depth separately
        Vector3 spawnPoint = transform.position + settings.spawnOffset + spawnDirection;                           //Use position of mama rat plus offset and random bias as basis for spawnpoint

        //Get launch characteristics:
        Vector3 launchVel = Vector3.up * Random.Range(settings.spawnForce.x, settings.spawnForce.y); //Initialize force vector for launching rat (with random force value)
        float launchAngle = Random.Range(settings.spawnAngle.x, settings.spawnAngle.y);              //Randomize angle at which rat is launched
        float spawnAngle = Vector2.SignedAngle(Vector2.down, RatBoid.FlattenVector(spawnDirection)); //Get angle between spawnpoint and mother rat position
        launchVel = Quaternion.AngleAxis(launchAngle, Vector3.right) * launchVel;                    //Rotate launch vector forward by launch angle
        launchVel = Quaternion.AngleAxis(spawnAngle, Vector3.up) * launchVel;                        //Rotate launch vector around mother rat by spawn angle
        launchVel += RatBoid.UnFlattenVector(velocity);                                              //Add current velocity of mother rat to launch velocity of child

        //Cleanup:
        newRat.position = spawnPoint;                              //Set rat position to spawnpoint
        ratController.flatPos = RatBoid.FlattenVector(spawnPoint); //Update flat position tracker of spawned rat
        ratController.Launch(launchVel);                           //Launch spawned rat
    }
    /// <summary>
    /// Launches rat into the air.
    /// </summary>
    /// <param name="force">Direction and power with which rat will be launched.</param>
    public void Launch(Vector3 force, bool placeMarker = true)
    {
        //Modify velocity:
        velocity = Vector2.zero; //Erase conventional velocity
        airVelocity = force;     //Apply force to airborne velocity

        //Place jump marker:
        if (force.normalized == Vector3.up) //Vertical jump
        {

        }
        if (placeMarker &&            //Jump marker is requested
            totalFollowerCount > 0 && //Rat has at least one follower
            trail.Count > 1)          //There is a trail to place the jump marker on
        {
            trail[0].jumpTokens += totalFollowerCount; //Place a jump marker on the current trail with enough tokens for each follower to jump once
            currentJumpMarkers++;                      //Indicate that a jump marker has been placed
        }

        //Cleanup:
        falling = true; //Indicate that rat is now falling
    }
    /// <summary>
    /// Updates stuff which depends on current number of follower rats. Should be called whenever follower count changes.
    /// </summary>
    public void OnFollowerCountChanged()
    {
        //Update UI:
        InterfaceMaster.SetCounter(totalFollowerCount); //Set rat counter

        //Update swarm settings:
        if (currentSwarmSettings == null) //Swarm settings have not been set up yet
        {
            if (swarmSettings.Count == 0) { Debug.LogError("Big Rat needs at least one swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
            else if (swarmSettings.Count == 1) { currentSwarmSettings = swarmSettings[0]; }                                     //Just make currentSwarmSettings a reference if only one settings object is given
            else currentSwarmSettings = ScriptableObject.CreateInstance<SwarmSettings>();                                       //Create a temporary object instance for swarm settings as part of normal setup
        }
        if (swarmSettings.Count == 1) return; //Ignore if swarm settings are just a copy of single given settings object
        int ratNumber = totalFollowerCount; //Get current number of follower rats
        SwarmSettings lerpSettingsA = null; //Initialize container to store lower bound settings
        SwarmSettings lerpSettingsB = null; //Initialize container to store upper bound settings
        foreach (SwarmSettings item in swarmSettings) //Iterate through each given settings item
        {
            if (ratNumber == item.targetRatNumber) //Item is designed for this exact number of rats
            {
                currentSwarmSettings.Lerp(item, item, ratNumber); //Just set current settings to item
                return;                                           //Skip everything else
            }
            if (ratNumber > item.targetRatNumber) //Item is designed for fewer rats than are currently spawned
            {
                if (lerpSettingsA == null) lerpSettingsA = item;                                     //Use these settings as lower bound if no others have qualified yet
                else if (lerpSettingsA.targetRatNumber < item.targetRatNumber) lerpSettingsA = item; //If two settings are competing for lower bound, use the one which is closer to current number of rats
            }
            else //Item is designed for more rats than are currently spawned
            {
                if (lerpSettingsB == null) lerpSettingsB = item;                                     //Use these settings as upper bound if no others have qualified yet
                else if (lerpSettingsB.targetRatNumber > item.targetRatNumber) lerpSettingsB = item; //If two settings are competing for upper bound, use the one which is closer to current number of rats
            }
        }
        if (lerpSettingsA == null) lerpSettingsA = lerpSettingsB;           //Just use closest upper bound setting if no settings are lower than given amount
        else if (lerpSettingsB == null) lerpSettingsB = lerpSettingsA;      //Just use closest lower bound setting if no settings are higher than given amount
        currentSwarmSettings.Lerp(lerpSettingsA, lerpSettingsB, ratNumber); //Lerp settings between two identified bounds
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns the point on trail which is closest to given reference point, but within given distance behind given trail value.
    /// </summary>
    /// <param name="origin">Reference point which returned position will be as close as possible to.</param>
    /// <param name="prevValue">Previous trail value (0 - 1) used to restrict section of checked trail. Pass negative for a clean check.</param>
    /// <param name="maxBackup">Maximum distance along trail by which returned point can be behind point at prevValue.</param>
    /// <param name="queryJump">If true, system will check whether or not point is a jump marker, and will expend a jump token if it is.</param>
    public TrailPointData GetClosestPointOnTrail(Vector3 origin, float prevValue = -1, float maxBackup = 0)
    {
        //Trim down trail:
        if (trail.Count == 1) return new TrailPointData(trail[0].point, new TrailPoint[] { trail[0] }); //Simply return only point in trail if applicable
        int lastValidIndex = trail.Count - 1;                                                           //Initialize place to store index of last valid point as last actual point on trail
        Vector2 origLastPoint = trail[lastValidIndex].point;                                            //Initialize place to store real value of last point
        float origLastSeg = trail[lastValidIndex - 1].segLength;                                        //Initialize place to store real length of last segment
        if (prevValue >= 0) //Only trim trail if a valid trail value is supplied
        {
            float valueRemaining = (prevValue * totalTrailLength) + maxBackup; //Get distance value of cut point in trail
            valueRemaining = Mathf.Clamp(valueRemaining, 0, totalTrailLength); //Clamp value to range of totalTrailLength
            for (int i = 0; i < trail.Count - 1; i++) //Iterate through trail points with segment lengths
            {
                if (trail[i].segLength > valueRemaining) //PrevValue is within this segment
                {
                    //Save original values:
                    lastValidIndex = i + 1;             //Store index of last trail point this method will be checking
                    origLastPoint = trail[i + 1].point; //Store original position of point in trail
                    origLastSeg = trail[i].segLength;   //Store original last segment length

                    //Temporarily modify trail:
                    trail[i + 1].point = Vector2.Lerp(trail[i].point, trail[i + 1].point, valueRemaining / trail[i].segLength); //Shorten final segment in trail based on remaining value
                    trail[i].segLength = valueRemaining;                                                                        //Set final segment length of temp trail point
                    break;                                                                                                      //Break loop
                }
                else valueRemaining -= trail[i].segLength; //Otherwise, subtract length of segment from remaining value and pass to next segment
            }
        }

        //Find closest point:
        Vector2 flatOrigin = RatBoid.FlattenVector(origin);  //Get origin as flat vector (for efficiency)
        Vector2 pointA = trail[0].point;                     //Initialize container for first point (will be closest point to start of trail)
        Vector2 pointB = trail[1].point;                     //Initialize second point at second item in trail
        int closestIndex = 1;                                //Initialize container to store index of closest point (later switches use to index of earliest point in trail)
        List<TrailPoint> pointRefs = new List<TrailPoint>(); //Storage for references to the two found trail points
        if (trail.Count > 2) //Only search harder if there are more than two points to check
        {
            //Get closest point:
            float closestDistance = Vector2.Distance(flatOrigin, pointB); //Initialize closest point tracker at distance between origin and second item in trail
            for (int i = 2; i < lastValidIndex; i++) //Iterate through all valid points in trail which have two valid neighbors (neither of which are already point A)
            {
                float distance = Vector2.Distance(flatOrigin, trail[i].point); //Check distance between origin and point
                if (distance < closestDistance) //Current point is closer than previous closest point
                {
                    //Store point values:
                    closestDistance = distance;     //Store closest distance
                    closestIndex = i;               //Store closest index
                    pointA = trail[i].point;        //Update point A
                }
            }
            pointRefs.Add(trail[closestIndex]); //Add point A to reference list

            //Get closest adjacent point:
            if (Vector2.Distance(flatOrigin, trail[closestIndex - 1].point) <= Vector2.Distance(flatOrigin, trail[closestIndex + 1].point)) //Former point is closer to origin
            {
                //Rearrange points:
                pointB = pointA;                        //Make point B the latter point
                pointA = trail[closestIndex - 1].point; //Make point A the former point
                closestIndex -= 1;                      //Make closestIndex the index of point A
                pointRefs.Add(trail[closestIndex]);     //Add point A (formerly point B) to reference list
            }
            else //Latter point is closer to origin
            {
                //Get point:
                pointB = trail[closestIndex + 1].point; //Make point B the latter point
                pointRefs.Add(trail[closestIndex + 1]); //Add point B to reference list
            }
        }
        Vector2 closestPoint = GetClosestPointOnLine(pointA, pointB, flatOrigin); //Get closest point to target between two found points in trail

        //Get position of point in line:
        float trailValue = 0; //Initialize container to store total line distance
        for (int i = 0; i < closestIndex; i++) //Iterate through each segment before segment containing target
        {
            trailValue += trail[i].segLength; //Add up segment lengths
        }
        trailValue += Vector2.Distance(closestPoint, pointB); //Add partial distance of current segment to total distance
        trailValue = trailValue / totalTrailLength;           //Get value as percentage of total length of trail

        //Repair trail:
        trail[lastValidIndex].point = origLastPoint;       //Restore original position of last point in temp trail
        trail[lastValidIndex - 1].segLength = origLastSeg; //Restore original segment length of last segment in temp trail

        //Return point data:
        if (closestPoint == trail[0].point) return new TrailPointData(closestPoint, pointRefs.ToArray(), (trail[0].point - trail[1].point).normalized, 0); //If the closest point is the very beginning of the trail, give it a direction which points toward the leader
        return new TrailPointData(closestPoint, pointRefs.ToArray(), -(pointB - pointA).normalized, trailValue);                                           //Otherwise, return closest point with known direction
    }
    /// <summary>
    /// Returns distance (in units) between points at given values on trail.
    /// </summary>
    /// <param name="pointValueA">Value between 0 and 1 representing first point in trail.</param>
    /// <param name="pointValueB">Value between 0 and 1 representing second point in trail.</param>
    public float GetDistanceBetweenTrailPoints(float pointValueA, float pointValueB)
    {
        if (pointValueA == pointValueB) return 0; //Return zero if points are the same
        if (pointValueB > pointValueA) //First value is smaller than second value
        {
            float p = pointValueB;     //Store point value B
            pointValueB = pointValueA; //Switch values with A
            pointValueA = p;           //Set value of A
        }
        return (totalTrailLength * pointValueB) - (totalTrailLength * pointValueA); //Use total trail length to determine exact distance between two points
    }
    /// <summary>
    /// Returns point between pointA and pointB which is closest to target.
    /// </summary>
    private Vector2 GetClosestPointOnLine(Vector2 pointA, Vector2 pointB, Vector2 target)
    {
        Vector2 dir = pointB - pointA;                 //Get direction of line between two points
        float lineLength = dir.magnitude;              //Get distance between points
        dir.Normalize();                               //Normalize directional vector
        Vector2 lhs = target - pointA;                 //Get the left hand side vector
        float product = Vector2.Dot(lhs, dir);         //Get dot product of direction and side
        product = Mathf.Clamp(product, 0, lineLength); //Clamp length to make sure projection is on line
        return pointA + (dir * product);               //Do projection to get actual closest point on trail
    }
}

//BONEYARD:
/*
    /// <summary>
    /// Returns point in trail which corresponds to given value.
    /// </summary>
    /// <param name="trailValue">Number between 0 and 1 representing position on trail (0 is the leader, 1 is the end).</param>
    public TrailPointData GetTrailPointFromValue(float trailValue)
    {
        //Edge cases:
        if (trail.Count < 2) return new TrailPointData(PosAsVector2());                                                                                                      //Return position of leader if trail has no segments
        if (trail.Count == 2) return new TrailPointData(Vector2.Lerp(trail[0].point, trail[1].point, trailValue), (trail[0].point - trail[1].point).normalized, trailValue); //Simply interpolate between only two points if possible

        //Find point:
        float distanceRemaining = trailValue * totalTrailLength; //Get amount of distance to scrub through
        for (int i = 0; i < trail.Count - 1; i++) //Iterate through each trail point with a segment length
        {
            if (trail[i].segLength >= distanceRemaining) //Point is inside this segment
            {
                Vector2 point = Vector2.Lerp(trail[i].point, trail[i + 1].point, distanceRemaining / trail[i].segLength); //Lerp to find point according to distance remaining within segment
                Vector2 direction = (trail[i + 1].point - trail[i].point).normalized;                                     //Get direction of trail at this point
                return new TrailPointData(point, direction, trailValue);                                                  //Return point data
            }
            else distanceRemaining -= trail[i].segLength; //Otherwise, subtract segment length and move further down trail
        }

        //Point could not be found:
        Debug.LogError("GetTrailPointFromValue failed"); //Post error
        return new TrailPointData();                     //Return empty data container
    }*/
/*
    public TrailPointData GetClosestPointOnTrail(Vector2 origin)
    {
        //Validity checks:
        if (trail.Count == 1) return new TrailPointData(trail[0]); //Simply return only point in trail if applicable

        //Find closest point:
        Vector2 pointA = trail[0]; //Initialize container for first point (will be closest point to start of trail)
        Vector2 pointB = trail[1]; //Initialize second point at second item in trail
        int closestIndex = 1;      //Initialize container to store index of closest point (later switches use to index of earliest point in trail)
        if (trail.Count > 2) //Only search harder if there are more than two points to check
        {
            //Get closest point:
            float closestDistance = Vector2.Distance(origin, pointB); //Initialize closest point tracker at distance between origin and second item in trail
            for (int i = 2; i < trail.Count - 1; i++) //Iterate through points in trail which have two neighbors (and are not already point A)
            {
                float distance = Vector2.Distance(origin, trail[i]); //Check distance between origin and point
                if (distance < closestDistance) //Current point is closer than previous closest point
                {
                    closestDistance = distance; //Store closest distance
                    closestIndex = i;           //Store closest index
                    pointA = trail[i];          //Update point A
                }
            }

            //Get closest adjacent point:
            if (Vector2.Distance(origin, trail[closestIndex - 1]) <= Vector2.Distance(origin, trail[closestIndex + 1])) //Former point is closer to origin
            {
                pointB = pointA;                  //Make point B the latter point
                pointA = trail[closestIndex - 1]; //Make point A the former point
                closestIndex -= 1;                //Make closestIndex the index of point A
            }
            else //Latter point is closer to origin
            {
                pointB = trail[closestIndex + 1]; //Make point B the latter point
            }
        }
        Vector2 closestPoint = GetClosestPointOnLine(pointA, pointB, origin); //Get closest point to target between two found points in trail

        //Get position of point in line:
        float trailValue = 0; //Initialize container to store total line distance
        for (int i = 0; i < closestIndex; i++) //Iterate through each segment before segment containing target
        {
            trailValue += segLengths[i]; //Add up segment lengths
        }
        trailValue += Vector2.Distance(closestPoint, pointB); //Add partial distance of current segment to total distance
        trailValue = trailValue / totalTrailLength;           //Get value as percentage of total length of trail

        //Return point data:
        if (closestPoint == trail[0]) return new TrailPointData(closestPoint, (trail[0] - trail[1]).normalized, 0); //If the closest point is the very beginning of the trail, give it a direction which points toward the leader
        return new TrailPointData(closestPoint, -(pointB - pointA).normalized, trailValue);                         //Otherwise, return closest point with known direction
    }*/