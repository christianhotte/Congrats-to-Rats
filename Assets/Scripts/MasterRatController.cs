using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;
using CustomEnums;
using UnityEngine.Events;

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
        //Data:
        /// <summary>
        /// Distance between this trail point and the one behind it (if applicable).
        /// </summary>
        public float segLength = 0; //Segment length will be set externally
        /// <summary>
        /// If above zero, trail point is treated as a jump marker. Ticks down by one for each ratBoid which uses this to jump.
        /// </summary>
        public int jumpTokens = 0; //TrailPoints do not initialize with jump tokens.
        /// <summary>
        /// Velocity of leader when this point was last updated.
        /// </summary>
        public Vector3 leaderVel;

        //Utility Variables:
        /// <summary>
        /// Position of trail point.
        /// </summary>
        public Vector2 point;
        /// <summary>
        /// Whether or not this point is a jump marker.
        /// </summary>
        public bool IsJumpMarker { get { return jumpTokens > 0; } }
        /// <summary>
        /// The current position of this point in the trail (between 0 and 1).
        /// </summary>
        public float TrailValue {
            get
            {
                //Initial checks:
                if (main.trail.Count == 0) { Debug.LogError("Tried to get value of trail point which is not in trail."); return 0; } //Post error and return nothing if point is not in trail
                if (main.trail.IndexOf(this) == 0) return 0;                                                                         //Return zero if this is the first point in the trail
                if (main.trail.IndexOf(this) == main.trail.Count - 1) return 1;                                                      //Return one if this is the last point in the trail

                //Find value:
                float value = 0; //Initialize value at zero
                foreach (TrailPoint otherPoint in main.trail) //Iterate through points in trail
                {
                    value += otherPoint.segLength; //Add up segment lengths of points up to and including this one
                    if (otherPoint == this) break; //Stop iterating after this point has been reached
                }
                return Mathf.Clamp01(value / main.totalTrailLength); //Return value as percentage of total trail length at position of this point
            }
        }

        //OPERATION METHODS:
        /// <summary>
        /// Create a new trail point.
        /// </summary>
        /// <param name="_point">Position of this point in 2D space.</param>
        public TrailPoint (Vector2 _point)
        {
            //Get data:
            point = _point;                                     //Set point vector
            leaderVel = RatBoid.UnFlattenVector(main.velocity); //Store velocity of leader at time of point creation
        }
        /// <summary>
        /// Make this trail point a jump marker.
        /// </summary>
        /// <param name="jumpVel">Initial velocity of jump</param>
        public void MakeJumpMarker(Vector3 jumpVel)
        {
            if (main.TotalFollowerCount > 0) //Leader must have some followers for a jump marker to be made
            {
                jumpTokens = main.TotalFollowerCount; //Add one jump token for each follower
                leaderVel = jumpVel;                  //Record jump velocity of leader
                main.currentJumpMarkers++;            //Indicate that a new jump marker has been added
            }
        }
        /// <summary>
        /// Returns true if this point has any remaining jump tokens, and expends on if this is the case.
        /// </summary>
        public bool TryExpendToken()
        {
            //Not a jump marker:
            if (!IsJumpMarker) return false; //Indicate false if point is not a jump marker

            //Is a jump marker:
            jumpTokens--; //Decrement jump token tracker
            if (jumpTokens == 0) //Check if marker is out of tokens
            {
                main.currentJumpMarkers--; //Update total number of jump markers
            }
            return true; //Confirm that token was expended
        }
        /// <summary>
        /// Remove this point's jump marker status.
        /// </summary>
        public void ClearJumpMarker()
        {
            if (IsJumpMarker) //Point is currently a jump marker
            {
                leaderVel.y = 0;           //Remove vertical velocity from memory
                jumpTokens = 0;            //Expend all jump tokens immediately
                main.currentJumpMarkers--; //Indicate that a marker has been removed
            }
        }
        /// <summary>
        /// Transfers all jump tokens to target trail point.
        /// </summary>
        public void TransferJumpTokens(TrailPoint target)
        {
            if (!IsJumpMarker) return;                           //Ignore if point is not a jump marker
            if (!target.IsJumpMarker) main.currentJumpMarkers++; //Indicate that a new marker is being created if target is not already a jump marker
            target.leaderVel = leaderVel;    //Pass leader velocity at jump point on to new jump marker
            target.jumpTokens += jumpTokens; //Add jump tokens from this point to target
            ClearJumpMarker();               //Clear this point's jump marker status
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
    internal SpriteRenderer sprite;   //Sprite renderer component for big rat
    internal Animator anim;           //Animator controller for big rat
    internal Billboarder billboarder; //Component used to manage sprite orientation
    internal AudioSource audioSource; //Audiosource component for mama rat sfx

    [Header("Settings Objects:")]
    [Tooltip("Interchangeable data object describing settings of the main rat")]                                                    public BigRatSettings settings;
    [Tooltip("Interchangeable data object describing sound settings for this rat")]                                                 public MamaSoundSettings soundSettings;
    [SerializeField, Tooltip("Place any number of swarm settings objects here (make sure they have different Target Rat Numbers)")] private List<SwarmSettings> swarmSettings = new List<SwarmSettings>();
    [Header("Debug Options:")]
    [SerializeField, Tooltip("Kills big rat")]        private bool debugKill;
    [SerializeField, Tooltip("Advances music phase")] private bool debugAdvanceMusic;

    //Runtime Vars:
    private SwarmSettings currentSwarmSettings;                      //Instance of swarmSettings object used to interpolate between rat behaviors
    internal List<RatBoid> followerRats = new List<RatBoid>();       //List of all rats currently following this controller
    internal List<RatBoid> jumpingFollowers = new List<RatBoid>();   //List of follower rats which are currently jumping (and therefore still counted toward total)
    internal List<RatBoid> deployedRats = new List<RatBoid>();       //List of all rats currently deployed by player
    internal List<TrailPoint> trail = new List<TrailPoint>();        //List of points in current trail (used to assemble ratswarm behind main rat)
    internal List<EffectZone> currentZones = new List<EffectZone>(); //List of zones rat is currently in
    internal float totalTrailLength = 0;                             //Current length of trail (in units)
    internal int currentJumpMarkers = 0;                             //Current number of jump markers in trail
    private int prevFollowerCount = 0;                               //Total follower count last time follower count-contingent settings were updated
    private SlowZone currentGlue = null;                             //Glue zone rat is currently in (if any)

    internal Vector2 velocity;         //Current speed and direction of movement
    internal Vector3 airVelocity;      //3D velocity used when rat is falling
    internal Vector2 forward;          //Normalized vector representing which direction big rat was most recently moving
    internal float currentSpeed;       //Current speed at which rat is moving
    private Vector2 rawMoveInput;      //Current input direction for movement (without modifiers)
    internal bool falling;             //Whether or not rat is currently falling
    internal bool commanding;          //Whether or not rat is currently deploying rats to a location
    private float aimTime = -1;        //The number of seconds rat has been aiming a throw for. Negative if rat is not aiming a throw
    private RaycastHit latestMouseHit; //Data from last point hit by mouse raycast
    private bool jumpButtonPressed;    //Indicates whether or not jump button is currently pressed

    internal bool stasis;           //When true, this rat will not update or move
    internal bool noControl;        //When true, inputs for this rat will not read as motion and will activate nothing
    internal bool unControlledFall; //When true, inputs for this rat will be ignored until rat lands

    //Utility Vars:
    /// <summary>
    /// Quantity of rats currently treated as followers.
    /// </summary>
    public int TotalFollowerCount { get { return followerRats.Count + jumpingFollowers.Count; } }
    /// <summary>
    /// Current position of this rat projected onto world Y axis.
    /// </summary>
    public Vector2 FlatPos { get { return new Vector2(transform.position.x, transform.position.z); } }
    /// <summary>
    /// Maximum amount of time rat needs to spend aiming to have fully-charged throw.
    /// </summary>
    private float MaxThrowChargeTime { get { return (1 / settings.throwChargeSpeed) + settings.throwChargeWait; } }
    /// <summary>
    /// Move input vector in the context of the current camera (and current camera blend state, if applicable).
    /// </summary>
    private Vector3 RotatedMoveInput
    {
        get
        {
            Vector3 moveInput = RatBoid.UnFlattenVector(rawMoveInput);                       //Put move input into Vector3 for rotating purposes
            float camAngle = Vector2.SignedAngle(Vector2.up, CameraTrigger.GetDirectionRef); //Get angle relative to current camera direction
            return Quaternion.AngleAxis(-camAngle, Vector3.up) * moveInput;                  //Rotate move input vector around Y axis based on camera angle
        }
    }

    //Events & Delegates:
    /// <summary>
    /// Event which is triggered after follower count has been changed and other effects have taken place.
    /// </summary>
    public UnityAction followerCountChanged;

    //Coroutines:
    /// <summary>
    /// Sequence which follows after main rat is killed.
    /// </summary>
    IEnumerator DeathSequence()
    {
        //Initial grace period:
        yield return new WaitForSeconds(settings.deadTime); //Wait a brief period

        //Reposition to spawnpoint:
        transform.position = Respawner.currentSpawnPoint.spawnPosition.position; //Move rat to position of spawnpoint
        ResetTrail();                                                            //Reset trail state
        yield return new WaitForSeconds(settings.respawnTransTime);              //Give camera time to move to new position

        //Pass to respawn system:
        Respawner.Respawn(); //Trigger respawn sequence in respawner system
    }
    /// <summary>
    /// Spawns given number of rats over given amount of time.
    /// </summary>
    public IEnumerator SpawnRatsOverTime(int rats, float time)
    {
        float secsPerRat = time / rats; //Get number of seconds to wait between rat spawns
        for (int i = 0; i < rats; i++) //Iterate for given number of rats
        {
            SpawnRat();                                  //Spawn a rat
            yield return new WaitForSeconds(secsPerRat); //Wait for designated time before spawning next rat
        }
    }

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
        trail.Insert(0, new TrailPoint(FlatPos)); //Add starting position as first point in trail
        OnFollowerCountChanged();                 //Set up swarm settings and do initial update
        MoveRat(0);                               //Snap rat to floor

        //Event subscriptions:
        followerCountChanged += OnFollowerCountChanged; //Add official follower count changed method to event
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>();   //Get spriteRenderer component
        anim = GetComponentInChildren<Animator>();           //Get animator controller component
        billboarder = GetComponentInChildren<Billboarder>(); //Get billboarder component
        audioSource = GetComponent<AudioSource>();           //Get audioSource component
    }
    private void Update()
    {
        //Update counters:
        if (!stasis && aimTime > -1 && aimTime != MaxThrowChargeTime) aimTime = Mathf.Min(aimTime + Time.deltaTime, MaxThrowChargeTime); //Increment aim time if rat is aiming (cap out at max aim time)

        //Perform rat movements:
        if (!stasis) MoveRat(Time.deltaTime);                     //Move the big rat
        RatBoid.UpdateRats(Time.deltaTime, currentSwarmSettings); //Move all the little rats

        //OnFollowerCountChanged(); //TEMP: Keep swarm settings regularly up-to-date for debugging purposes

        //Visualize trail:
        if (trail.Count > 1)
        {
            //float x = 1;
            for (int i = 1; i < trail.Count; i++)
            {
                Vector3 p1 = new Vector3(trail[i].point.x, 0.1f, trail[i].point.y);
                Vector3 p2 = new Vector3(trail[i - 1].point.x, 0.1f, trail[i - 1].point.y);
                Debug.DrawLine(p1, p2, trail[i].IsJumpMarker || trail[i - 1].IsJumpMarker ? Color.yellow : Color.blue);
                //if (trail[i].IsJumpMarker) { print("Marker " + x + "'s trail value is " + trail[i].trailValue); x++; }
            }
        }

        //Debug functions:
        if (debugKill) { Kill(); debugKill = false; }                                           //Trigger death on debug kill command
        if (debugAdvanceMusic) { MusicManager.main.AdvancePhase(); debugAdvanceMusic = false; } //Trigger music phase advancement

        //Footstep sounds:
        if (!stasis && !falling && !audioSource.isPlaying && currentSpeed != 0) //Rat is moving (not falling) but audioSource is silent
        {
            audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.footsteps)); //Play a random footstep noise
        }
    }
    private void OnDestroy()
    {
        //followerCountChanged -= OnFollowerCountChanged; //Unsubscribe from follower count changed event
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
            Vector3 addVel = new Vector3();                                                                   //Initialize vector to store acceleration
            if (!noControl && !commanding) addVel += settings.accel * settings.airControl * RotatedMoveInput; //Get acceleration due to input
            addVel += Vector3.down * settings.gravity;                                                        //Get acceleration due to gravity
            addVel += -airVelocity.normalized * settings.airDrag;                                             //Get deceleration due to drag
            airVelocity += addVel * deltaTime;                                                                //Apply change in velocity
            velocity = RatBoid.FlattenVector(airVelocity);                                                    //Update flat velocity to match air velocity

            //Get new position:
            newPos += airVelocity * deltaTime; //Get target position based on velocity over time
            float airSpeed = Vector3.Distance(transform.position, newPos); //Get current airspeed of rat
            if (Physics.SphereCast(transform.position, settings.collisionRadius, (newPos - transform.position).normalized, out RaycastHit hit, airSpeed, settings.blockingLayers)) //Fall is obstructed
            {
                float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up); //Get angle of surface relative to flat floor
                if (hit.collider.TryGetComponent(out RatBouncer bouncer)) //Rat has collided with a bouncy object
                {
                    //Bounce (with object settings):
                    airVelocity = bouncer.GetBounceVelocity(airVelocity, hit.normal);                                  //Get new velocity from bouncer object
                    if (airVelocity.magnitude < settings.wallRepulse) airVelocity = hit.normal * settings.wallRepulse; //Make sure bounce has at least a little velocity so rat doesn't get stuck
                    newPos = transform.position;                                                                       //Do not allow rat to move into wall
                    audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.bounceSounds));                     //Play random bounce sound
                }
                else if (hit.collider.TryGetComponent(out ToasterController toaster)) //Rat has collided with a toaster
                {
                    toaster.LoadRat(); //Load mama rat into toaster
                    return;            //SKip everything else
                }
                else if (surfaceAngle > settings.maxWalkAngle) //Surface is too steep for rat to land on
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
                    falling = false;                                                           //Indicate that rat is no longer falling
                    if (unControlledFall) { unControlledFall = false; noControl = false; }     //End uncontrolled fall if applicable
                    airVelocity = Vector3.zero;                                                //Cancel all air velocity
                    if (!noControl) anim.SetTrigger("Land");                                   //Play landing animation
                    audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.landings)); //Play a random landing sound
                }
            }
        }
        else //Rat is moving normally along a surface
        {
            //Modify velocity:
            if (!noControl && !commanding && rawMoveInput != Vector2.zero && aimTime < 0) //Player is moving rat in a direction (and not aiming) (and is in control)
            {
                //Add velocity:
                Vector3 input = RotatedMoveInput;                               //Store contextual move input vector
                Vector2 addVel = RatBoid.FlattenVector(input) * settings.accel; //Get added velocity based on input this frame
                velocity += addVel * deltaTime;                                 //Add velocity as acceleration over time

                //Flip sprite:
                if (input.x != 0) sprite.flipX = settings.flipAll ? input.x < 0 : input.x > 0; //Flip sprite to direction of horizontal move input
            }
            else if (velocity != Vector2.zero) //No input is given but rat is still moving
            {
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, settings.decel * deltaTime); //Slow rat down based on deceleration over time
            }

            //Cap velocity:
            currentSpeed = velocity.magnitude;                                    //Get current speed of main rat
            float effectiveMaxSpeed = settings.speed;                             //Initialize value for max speed
            if (currentGlue != null) effectiveMaxSpeed *= currentGlue.slowFactor; //Apply glue factor if applicable
            if (currentSpeed > effectiveMaxSpeed) //Check if current speed is faster than target speed
            {
                velocity = Vector2.ClampMagnitude(velocity, effectiveMaxSpeed); //Clamp velocity to target speed
                currentSpeed = effectiveMaxSpeed;                               //Update current speed value
            }
            if (anim != null) anim.SetFloat("Speed", currentSpeed / effectiveMaxSpeed); //Send speed value to animator

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
                        Launch(new Vector3(velocity.x, settings.cliffHop, velocity.y), false);       //Bump rat off cliff
                        audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.fallSounds)); //Play a random fall sound
                    }
                }
            }
        }

        //Check zones:
        if (newPos != transform.position) //Only do zone check while rat is moving
        {
            //Look for new zones:
            Vector3 moveDir = newPos - transform.position;                                                                                          //Get non-normalized vector representing rat's direction and distance of travel
            float moveDist = moveDir.magnitude; moveDir = moveDir.normalized;                                                                       //Get distance rat has moved as separate variable, then normalize move direction vector
            RaycastHit[] zoneHits = Physics.SphereCastAll(transform.position, settings.collisionRadius, moveDir, moveDist, RatBoid.effectZoneMask); //Check to see if rat has entered any zones
            
            //Update zone lists:
            List<EffectZone> newZones = new List<EffectZone>();                  //Create a temporary list to store new effect zones
            currentGlue = null;                                                  //Reset sticky zone memory
            foreach (RaycastHit hit in zoneHits) //Iterate through each detected zone
            {
                if (hit.collider.TryGetComponent(out EffectZone zone)) //Make sure object has EffectZone component
                {
                    //Checks:
                    if (zone.deactivated) continue; //Skip zone if it is deactivated

                    //Mark zone as current:
                    newZones.Add(zone); //Add zone to new list of occupied zones
                    if (!currentZones.Contains(zone)) //Zone did not previously contain mama rat
                    {
                        zone.bigRatInZone = true; //Indicate that rat is now in zone
                        zone.OnBigRatEnter();     //Call big rat entry event
                    }
                    else currentZones.Remove(zone); //Use currentZones list to get zones which rat has left

                    //Other checks:
                    if (hit.collider.TryGetComponent(out SlowZone slowZone)) currentGlue = slowZone; //Get slowZone component if applicable
                }
            }
            foreach (EffectZone zone in currentZones) //Iterate through zones which rat is no longer in
            {
                if (zone == null || stasis || noControl) continue; //Skip destroyed zones
                zone.bigRatInZone = false;                         //Indicate that rat is no longer in zone
                if (!zone.deactivated && zone.checkForRatLeave) zone.OnBigRatLeave(); //Indicate that big rat has left zone
            }
            currentZones = newZones; //Save new list of zones
        }

        //Movement cleanup:
        currentSpeed = Vector2.Distance(FlatPos, RatBoid.FlattenVector(newPos)) / deltaTime; //Get actual current speed (after obstructions)
        transform.position = newPos;                                                         //Apply new position
        forward = velocity.normalized;                                                       //Update forward direction tracker

        //Update trail characteristics:
        trail.Insert(0, new TrailPoint(FlatPos));                                //Add new trailPoint for current position
        float firstSegLength = Vector2.Distance(trail[0].point, trail[1].point); //Get length of new segment being created
        totalTrailLength += firstSegLength;                                      //Add length of new segment to total length of trail
        if (trail.Count > 1) trail[0].segLength = firstSegLength;                //Store length of new segment if applicable
        if (trail.Count > 2) //Only perform culling operations if trail is long enough
        {
            //Grow first segment to minimum length:
            float secondSegLength = Vector2.Distance(trail[1].point, trail[2].point); //Get length of second segment in trail
            if (secondSegLength < currentSwarmSettings.minTrailSegLength) //Check if second segment in trail is too short (first segment can be any length)
            {
                //Fuse segments:
                totalTrailLength -= firstSegLength + secondSegLength;              //Subtract lengths of both removed segments from total
                trail[1].TransferJumpTokens(trail[0]);                             //Pass jump tokens to the new point
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
            float targetTrailLength = (1 / currentSwarmSettings.trailDensity) * (TotalFollowerCount + 0.01f);                 //Get target trail length based off of follower count and settings
            targetTrailLength *= Mathf.Lerp(1, currentSwarmSettings.velTrailLengthMultiplier, currentSpeed / settings.speed); //Apply velocity-based length multiplier to target trail length
            while (totalTrailLength > targetTrailLength) //Current trail is longer than target length (and is non-zero)
            {
                //State check:
                if (trail[^1].IsJumpMarker && totalTrailLength < targetTrailLength * 1.5f) break; //Wait for jump markers to resolve in order to trim trail (but give up once trail gets too long)
                float extraLength = totalTrailLength - targetTrailLength;                         //Get amount of extra length left in trail
                float lastSegLength = Vector2.Distance(trail[^1].point, trail[^2].point);         //Get distance between last two segments in trail NOTE: this distance check may not be needed
                
                //Determine how to shorten trail:
                if (extraLength >= lastSegLength) //Last segment is shorter than length which needs to be removed
                {
                    trail[^1].ClearJumpMarker();       //Clean up jump marker status (if applicable)
                    trail.RemoveAt(trail.Count - 1);   //Remove last segment from trail
                    totalTrailLength -= lastSegLength; //Subtract length of removed segment from total
                    trail[^1].segLength = 0;           //Delete now-unnecessary segment length of final point in trail
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
                //State check:
                if (trail[1].IsJumpMarker) break;

                //Fuse first two segments:
                totalTrailLength -= trail[0].segLength + trail[1].segLength;           //Remove deleted segment lengths from total trail length
                trail[1].ClearJumpMarker();                                            //Clean up jump marker status (if applicable)
                trail.RemoveAt(1);                                                     //Remove second point from trail (combining first and second segments)
                trail[0].segLength = Vector2.Distance(trail[0].point, trail[1].point); //Update new length of first segment
                totalTrailLength += trail[0].segLength;                                //Add new segment length to total
            }
        }
    }

    //INPUT METHODS:
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        rawMoveInput = context.ReadValue<Vector2>(); //Store input value

        //Animator effects:
        if (rawMoveInput == Vector2.zero) anim.SetBool("MoveInput", false); //Indicate to animator when no move input is being given
        else anim.SetBool("MoveInput", true);                               //Indicate to animator when move input is being given
    }
    public void OnScrollSpawn(InputAction.CallbackContext context)
    {
        if (context.started & !noControl) //Scroll wheel has just been moved one tick and player has control over rat
        {
            if (context.ReadValue<float>() > 0) SpawnRat();                        //Spawn rats when wheel is scrolled up
            else if (followerRats.Count > 0) Destroy(followerRats[^1].gameObject); //Despawn rats when wheel is scrolled down
        }
    }
    public void OnCommandInput(InputAction.CallbackContext context)
    {
        if (context.performed && !noControl && !falling) //Command button has been pressed and player has control over rat
        {
            commanding = true;                                  //Indicate that rat is now in command mode
            anim.SetBool("Pointing", true);                     //Execute pointing animation
            audioSource.PlayOneShot(soundSettings.deploySound); //Play deploy sound
        }
        else //Command button has been released
        {
            commanding = false;              //Indicate that rat is no longer commanding
            anim.SetBool("Pointing", false); //End pointing animation
        }
            
    }
    public void OnThrowInput(InputAction.CallbackContext context)
    {
        if (noControl || commanding) return;             //Ignore if player has no control over rat
        if (context.performed && followerRats.Count > 0) //Throw button has just been pressed (and there is at least one rat to throw)
        {
            //Cleanup:
            aimTime = 0;                                                                   //Indicate that rat is now aiming
            anim.SetBool("Aiming", true);                                                  //Play aim animation
            audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.throwWindups)); //Play a random windup sound
        }
        else if (aimTime > -1) //Throw button has just been released while aiming
        {
            if (followerRats.Count > 0) //There is at least one rat to throw
            {
                //Initialize:
                List<RatBoid> throwRats = new List<RatBoid>();                                                      //Initialize list to store rats which will be thrown
                float aimValue = GetAimValue();                                                                     //Get aim value (so we don't have to keep re-calculating it)
                int ratsToThrow = Mathf.RoundToInt(TotalFollowerCount * settings.maxRatPercentPerThrow * aimValue); //Get raw number of rats to throw based on aim value and follower count

                //Find rats to throw:
                ratsToThrow = Mathf.Clamp(ratsToThrow, 1, settings.maxRatsPerThrow);                            //Clamp throw number to make sure it is at least one and does not exceed hard cap
                ratsToThrow = Mathf.Min(ratsToThrow, followerRats.Count);                                       //Make sure system is not trying to throw rats which are currently airborne
                for (int i = 0; i < ratsToThrow && i < followerRats.Count; i++) throwRats.Add(followerRats[i]); //Designate rats from follower list to be thrown

                //Launch rats:
                float maxPointRand = settings.maxRandomSpread * aimValue; //Use aim value to scale random spread based on how many rats are being thrown
                foreach (RatBoid rat in throwRats) //Iterate through list of rats being thrown
                {
                    //Get throw target:
                    Vector3 currentTarget = latestMouseHit.point;                                                           //Get exact target from last point hit by mouse raycast
                    currentTarget += Vector3.ProjectOnPlane(Random.insideUnitSphere * maxPointRand, latestMouseHit.normal); //Apply randomness to target within set radius, and only along normal plane of target point

                    //Throw:
                    rat.transform.position = transform.position;                                       //Move rat to position of leader
                    rat.thrown = true;                                                                 //Indicate that rat has been thrown
                    rat.Launch((currentTarget - transform.position).normalized * settings.throwForce); //Apply throwForce to rat relative to given target
                }

                //Cleanup:
                audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.throwReleases)); //Play a random release sound
            }

            //Cleanup:
            aimTime = -1;                  //Indicate that rat is no longer aiming
            anim.SetBool("Aiming", false); //Play throw animation
        }
    }
    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed && !noControl && !commanding) //Jump button has just been pressed (and player has control over rat)
        {
            if (stasis && ToasterController.main.bigRatContained) //Use jump to launch from toaster
            {
                ToasterController.main.LaunchRats(); //Launch all contained rats from toaster
            }
            else if (!falling && currentGlue == null) //Player can only jump while they are not in the air (and not stuck to glue)
            {
                //Perform jump:
                Vector3 jumpforce = RotatedMoveInput.normalized * settings.jumpPower.x;             //Get horizontal jump power
                jumpforce.y = settings.jumpPower.y;                                                 //Get vertical jump power
                if (rawMoveInput == Vector2.zero) jumpforce.y *= settings.stationaryJumpMultiplier; //Apply multiplier to vertical jump if rat is stationary
                audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.jumpSounds));        //Play a random jump sound
                Launch(jumpforce);                                                                  //Launch rat using jump force
            }
            jumpButtonPressed = true; //Indictate that jump button is now pressed
        }
        else if (context.canceled) //Jump button has just been released
        {
            jumpButtonPressed = false; //Indicate that jump button is no longer pressed
        }
    }
    public void OnMousePositionMove(InputAction.CallbackContext context)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(context.ReadValue<Vector2>());                                      //Create a ray which shoots out of the camera
        if (Physics.Raycast(mouseRay, out RaycastHit hit, 10000, settings.throwTargetLayers)) { latestMouseHit = hit; } //Use mouseray to check for walls we can throw rats at
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Spawns a new rat and adds it to the swarm.
    /// </summary>
    public RatBoid SpawnRat()
    {
        //Initialize:
        Transform newRat = Instantiate(settings.basicRatPrefab).transform; //Spawn new rat
        RatBoid ratController = newRat.GetComponent<RatBoid>();            //Get controller from spawned rat
        float rotationOffset = CameraTrigger.GetRotationOffset;            //Get current rotation offset from camera

        //Get spawn position:
        Vector3 spawnDirection = new Vector3(Random.Range(-settings.spawnArea.x / 2, settings.spawnArea.x / 2), 0, //Get vector which moves spawnpoint away from center by a random amount
                                             Random.Range(-settings.spawnArea.y / 2, settings.spawnArea.y / 2));   //Add random amount to depth separately
        Vector3 spawnPoint = settings.spawnOffset + spawnDirection;                                                //Initialize spawnpoint at world zero with offset and random bias as basis
        spawnPoint = Quaternion.AngleAxis(-rotationOffset, Vector3.up) * spawnPoint;                               //Rotate spawnpoint around center to correct for current camera rotation
        spawnPoint += transform.position;                                                                          //Add position to get final spawnpoint

        //Get launch characteristics:
        Vector3 launchVel = Vector3.up * Random.Range(settings.spawnForce.x, settings.spawnForce.y); //Initialize force vector for launching rat (with random force value)
        float launchAngle = Random.Range(settings.spawnAngle.x, settings.spawnAngle.y);              //Randomize angle at which rat is launched
        float spawnAngle = Vector2.SignedAngle(Vector2.down, RatBoid.FlattenVector(spawnDirection)); //Get angle between spawnpoint and mother rat position
        launchVel = Quaternion.AngleAxis(launchAngle, Vector3.right) * launchVel;                    //Rotate launch vector forward by launch angle
        launchVel = Quaternion.AngleAxis(spawnAngle - rotationOffset, Vector3.up) * launchVel;       //Rotate launch vector around mother rat by spawn angle (also correct for camera rotation)
        launchVel += RatBoid.UnFlattenVector(velocity);                                              //Add current velocity of mother rat to launch velocity of child

        //Cleanup:
        newRat.position = spawnPoint;                                                 //Set rat position to spawnpoint
        ratController.flatPos = RatBoid.FlattenVector(spawnPoint);                    //Update flat position tracker of spawned rat
        ratController.Launch(launchVel);                                              //Launch spawned rat
        audioSource.PlayOneShot(soundSettings.RandomClip(soundSettings.spawnSounds)); //Play a random spawn sound
        return ratController;                                                         //Return control script of launched rat
    }
    /// <summary>
    /// Launches rat into the air.
    /// </summary>
    /// <param name="force">Direction and power with which rat will be launched.</param>
    public void Launch(Vector3 force, bool placeMarker = true)
    {
        //Place jump marker:
        if (force.normalized == Vector3.up) //Vertical jump
        {

        }
        else if (placeMarker &&            //Jump marker is requested
                 TotalFollowerCount > 0 && //Rat has at least one follower
                 trail.Count > 1)          //There is a trail to place the jump marker on
        {
            Vector3 offsetPos = transform.position; offsetPos.y += settings.jumpValidationOffset.y;                            //Get position with vertical validation offset applied
            Vector3 jumpValPos = offsetPos + (RatBoid.UnFlattenVector(velocity).normalized * settings.jumpValidationOffset.x); //Predict position to check for jump validity with
            Debug.DrawLine(offsetPos, jumpValPos, Color.red, 5);
            if (!Physics.Linecast(offsetPos, jumpValPos, settings.blockingLayers)) //Jump is predicted to be valid
            {
                trail[0].MakeJumpMarker(force); //Make a jump marker at first trail point
            }
            
        }

        //Modify velocity:
        velocity = Vector2.zero; //Erase conventional velocity
        airVelocity = force;     //Apply force to airborne velocity

        //Cleanup:
        if (placeMarker) anim.SetTrigger("Jump"); //Play jumping animation
        else anim.SetTrigger("Fall");             //Play falling animation
        falling = true;                           //Indicate that rat is now falling
    }
    /// <summary>
    /// Call to add given rat as a follower.
    /// </summary>
    public void AddRatAsFollower(RatBoid newFollower)
    {
        //Initialize:
        if (followerRats.Contains(newFollower)) return;      //Ignore if rat is already a follower
        followerRats.Add(newFollower);                       //Add rat to followers list
        if (TotalFollowerCount == prevFollowerCount) return; //Ignore if this addition does not change follower count (likely due to aerial followers)

        //Jump marker maintenance:
        if (currentJumpMarkers > 0) //System has jump markers in place
        {
            for (int i = 0; i < trail.Count; i++) //Iterate through trail (from beginning to end)
            {
                if (trail[i].TrailValue >= newFollower.lastTrailValue) break; //Don't check any points which are behind position at which rat joined
                if (trail[i].IsJumpMarker) trail[i].jumpTokens++;             //Add a jump token to any jump markers ahead of new rat
            }
        }

        //Cleanup:
        followerCountChanged.Invoke(); //Perform updates triggered by change in follower count
    }
    /// <summary>
    /// Call to remove given rat from follower list.
    /// </summary>
    public void RemoveRatAsFollower(RatBoid oldFollower)
    {
        //Initialize:
        if (followerRats.Contains(oldFollower)) followerRats.Remove(oldFollower); //Remove rat from follower list if applicable
        if (TotalFollowerCount == prevFollowerCount) return;                      //Ignore if this subtraction does not change follower count (likely due to aerial followers)

        //Jump marker maintenance:
        if (currentJumpMarkers > 0) //System has jump markers in place
        {
            for (int i = 0; i < trail.Count; i++) //Iterate through trail (from beginning to end)
            {
                if (trail[i].TrailValue >= oldFollower.lastTrailValue) break; //Don't check any points which are behind position which rat left
                trail[i].TryExpendToken();                                    //Remove a token from any trail point ahead of removed rat
            }
        }

        //Cleanup:
        followerCountChanged.Invoke(); //Perform updates triggered by change in follower count
    }
    /// <summary>
    /// Causes main rat to die and respawn at current spawnpoint.
    /// </summary>
    public void Kill()
    {
        //Destroy all rats:
        RatBoid.DestroyAll(); //Destroy all spawned rats

        //Simulate rat destruction:
        stasis = true;                //Remove rat from player control
        billboarder.SetVisibility(0); //Make rat invisible
        audioSource.PlayOneShot(soundSettings.deathSound); //Play death sound

        //Cleanup:
        velocity = Vector2.zero;         //Zero-out velocity
        airVelocity = Vector2.zero;      //Zero-out air velocity
        StartCoroutine(DeathSequence()); //Begin death sequence
    }
    /// <summary>
    /// Updates general stuff which depends on current number of follower rats. Works with rat addition and removal.
    /// </summary>
    private void OnFollowerCountChanged()
    {
        //Initialize:
        prevFollowerCount = TotalFollowerCount; //Update follower count memory

        //Update UI:
        InterfaceMaster.SetCounter(TotalFollowerCount); //Set rat counter

        //Update swarm settings:
        if (currentSwarmSettings == null) //Swarm settings have not been set up yet
        {
            if (swarmSettings.Count == 0) { Debug.LogError("Big Rat needs at least one swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
            else if (swarmSettings.Count == 1) { currentSwarmSettings = swarmSettings[0]; }                                     //Just make currentSwarmSettings a reference if only one settings object is given
            else currentSwarmSettings = ScriptableObject.CreateInstance<SwarmSettings>();                                       //Create a temporary object instance for swarm settings as part of normal setup
        }
        if (swarmSettings.Count == 1) return; //Ignore if swarm settings are just a copy of single given settings object
        int ratNumber = TotalFollowerCount; //Get current number of follower rats
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
    /// Returns point in trail which corresponds to given value.
    /// </summary>
    /// <param name="trailValue">Number between 0 and 1 representing position on trail (0 is the leader, 1 is the end).</param>
    public TrailPointData GetTrailPointFromValue(float trailValue)
    {
        //Edge cases:
        if (trail.Count < 2) return new TrailPointData(FlatPos, trail.ToArray());                                                                                                             //Return position of leader if trail has no segments (return extant trail points for good measure)
        if (trail.Count == 2) return new TrailPointData(Vector2.Lerp(trail[0].point, trail[1].point, trailValue), trail.ToArray(), (trail[0].point - trail[1].point).normalized, trailValue); //If there is just one segment, simply lerp between its two points

        //Find point:
        float distanceRemaining = trailValue * totalTrailLength; //Get exact amount of distance which needs to be covered to get to point
        for (int i = 0; i < trail.Count - 1; i++) //Iterate through trail points with at least one following point
        {
            if (trail[i].segLength >= distanceRemaining) //Point is inside this segment
            {
                Vector2 point = Vector2.MoveTowards(trail[i].point, trail[i + 1].point, distanceRemaining);          //Find point within current segment based on remaining distance
                Vector2 direction = (trail[i].point - trail[i + 1].point).normalized;                                //Get direction of trail at this point
                return new TrailPointData(point, new TrailPoint[] { trail[i], trail[i + 1]}, direction, trailValue); //Return point data
            }
            else distanceRemaining -= trail[i].segLength; //Otherwise, subtract segment length from remaining distance and move on to next point
        }

        //Point could not be found:
        Debug.LogWarning("GetTrailPointFromValue failed to get point within trail, TotalTrailLength may be inaccurate.");                                  //Post warning
        return new TrailPointData(trail[^1].point, new TrailPoint[] { trail[^2], trail[^1] }, (trail[^2].point - trail[^1].point).normalized, trailValue); //Assume point is at very end of trail
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
    /// <summary>
    /// Checks aim time and returns value representing intensity of potential throw.
    /// </summary>
    /// <returns>Value between 0 and 1, or -1 if rat is not currently aiming.</returns>
    private float GetAimValue()
    {
        if (aimTime < 0) return -1;                                                                     //Return negative if rat is not aiming
        if (aimTime < settings.throwChargeWait) return 0;                                               //Return minimum aim value if in waiting phase of aim charge
        return Mathf.Clamp01(Mathf.InverseLerp(settings.throwChargeWait, MaxThrowChargeTime, aimTime)); //Return value representing strength of charge
    }
    /// <summary>
    /// Resets trail state and erases existing trail.
    /// </summary>
    private void ResetTrail()
    {
        trail.Clear();                            //Clear trail list
        trail.Insert(0, new TrailPoint(FlatPos)); //Add new trailPoint for current position
        totalTrailLength = 0;                     //Reset trail length tracker
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