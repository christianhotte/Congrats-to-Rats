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

        public TrailPointData(Vector2 position)
        {
            this.point = position;       //Set point vector
            this.forward = Vector2.zero; //Indicate that point has no implied direction
            this.linePosition = 0;       //Set line value to zero (assume point is at head of line)
        }
        public TrailPointData(Vector2 position, Vector2 direction, float lineValue)
        {
            this.point = position;         //Set point vector
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
    private SpriteRenderer sprite; //Sprite renderer component for big rat
    private Animator anim;         //Animator controller for big rat

    [Header("Settings:")]
    [Tooltip("Interchangeable data object describing settings of the main rat")]                                                    public BigRatSettings settings;
    [SerializeField, Tooltip("Place any number of swarm settings objects here (make sure they have different Target Rat Numbers)")] private List<SwarmSettings> swarmSettings = new List<SwarmSettings>();

    //Runtime Vars:
    private SwarmSettings currentSwarmSettings;                //Instance of swarmSettings object used to interpolate between rat behaviors
    internal List<RatBoid> followerRats = new List<RatBoid>(); //List of all rats currently following this controller
    internal List<RatBoid> deployedRats = new List<RatBoid>(); //List of all rats currently deployed by player
    private List<Vector2> trail = new List<Vector2>();         //List of points in current trail (used to assemble ratswarm behind main rat)
    private List<float> segLengths = new List<float>();        //List of lengths corresponding to segments in trail
    internal float totalTrailLength = 0;                       //Current length of trail

    internal Vector2 velocity;   //Current speed and direction of movement
    internal Vector2 forward;    //Normalized vector representing which direction big rat was most recently moving
    internal float currentSpeed; //Current speed at which rat is moving
    private Vector2 moveInput;   //Current input direction for movement
    internal bool falling;       //Whether or not rat is currently falling
    internal bool commanding;    //Whether or not rat is currently deploying rats to a location
    private bool aiming;         //True when rat is preparing for a throw

    //RUNTIME METHODS:
    private void Awake()
    {
        //Validity checks:
        if (settings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); }                   //Make sure big rat has ratSettings
        if (swarmSettings.Count == 0) { Debug.LogError("Big rat needs at least one swarmSettings object"); Destroy(this); } //Make sure rat swarm has settings
        if (main == null) main = this; else Destroy(this);                                                                  //Singleton-ize this script instance

        //Globabl initialization:
        Application.targetFrameRate = 120; //Set target framerate

        //Local initialization:
        trail.Insert(0, PosAsVector2()); //Add starting position as first point in trail
        OnFollowerCountChanged();        //Set up swarm settings and do initial update
        MoveRat(0);                      //Snap rat to floor
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>(); //Get spriteRenderer component
        anim = GetComponentInChildren<Animator>();         //Get animator controller component
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
                Vector3 p1 = new Vector3(trail[i].x, 0.1f, trail[i].y);
                Vector3 p2 = new Vector3(trail[i - 1].x, 0.1f, trail[i - 1].y);
                Debug.DrawLine(p1, p2, Color.blue);
            }
        }
    }

    //UPDATE METHODS:
    /// <summary>
    /// Moves the big rat according to current velocity.
    /// </summary>
    private void MoveRat(float deltaTime)
    {
        //Modify velocity:
        if (moveInput != Vector2.zero) //Player is moving rat in a direction
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
            Vector3 newPos = transform.position; //Initialize new position as current position of rat
            newPos.x += velocity.x * deltaTime;  //Add X velocity over time to position target
            newPos.z += velocity.y * deltaTime;  //Add Y velocity over time to position target

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
                    //INITIATE FALL
                }
            }

            //Cleanup:
            transform.position = newPos;   //Apply new position
            forward = velocity.normalized; //Update forward direction tracker
        }

        //Update trail characteristics:
        trail.Insert(0, PosAsVector2());                        //Add new trailPoint for current position
        float segLength = Vector2.Distance(trail[0], trail[1]); //Get length of new segment being created
        totalTrailLength += segLength;                          //Add length of new segment to total length of trail
        if (trail.Count > 1 && segLengths.Count == 0) //Check for missing segment at start of trail
        {
            segLengths.Add(segLength); //Add length of first segment to lengths list
        }
        if (trail.Count > 2) //Only perform culling operations if trail is long enough
        {
            //Grow first segment to minimum length:
            float secondSegLength = Vector2.Distance(trail[1], trail[2]); //Get length of second segment in trail
            if (secondSegLength < currentSwarmSettings.minTrailSegLength) //Check if second segment in trail is too short (first segment can be any length)
            {
                totalTrailLength -= segLength + secondSegLength;  //Subtract lengths of both removed segments from total
                trail.RemoveAt(1);                                //Remove second segment from trail
                segLength = Vector2.Distance(trail[0], trail[1]); //Get new length of first segment
                totalTrailLength += segLength;                    //Add length of new segment back to total
            }
            else //A new segment is being created
            {
                segLengths.Insert(1, secondSegLength); //Add second segment length to list now that it is official
            }
            segLengths[0] = segLength; //Keep first segment in list constantly updated

            //Limit trail length:
            float targetTrailLength = (1 / currentSwarmSettings.trailDensity) * (followerRats.Count + 0.01f);                 //Get target trail length based off of follower count and settings
            targetTrailLength *= Mathf.Lerp(1, currentSwarmSettings.velTrailLengthMultiplier, currentSpeed / settings.speed); //Apply velocity-based length multiplier to target trail length
            while (totalTrailLength > targetTrailLength) //Current trail is longer than target length (and is non-zero)
            {
                float extraLength = totalTrailLength - targetTrailLength;     //Get amount of extra length left in trail
                float lastSegLength = Vector2.Distance(trail[^1], trail[^2]); //Get distance between last two segments in trail
                if (extraLength >= lastSegLength) //Last segment is shorter than length which needs to be removed
                {
                    trail.RemoveAt(trail.Count - 1);           //Remove last segment from trail
                    segLengths.RemoveAt(segLengths.Count - 1); //Remove last length from segment list
                    totalTrailLength -= lastSegLength;         //Subtract length of removed segment from total
                }
                else //Last segment is longer than length which needs to be removed
                {
                    trail[^1] = Vector2.MoveTowards(trail[^1], trail[^2], extraLength); //Shorten last segment by extra length
                    segLengths[^1] = segLengths[^1] - extraLength;                      //Subtract extra length from last segment length tracker
                    totalTrailLength -= extraLength;                                    //Subtract remaining extra length from total
                }
            }

            //Check for kinks in line:
            while (trail.Count > 2 && Vector2.Angle(trail[1] - trail[0], trail[1] - trail[2]) < 180 - currentSwarmSettings.maxSegAngle) //Trail is kinked (and contains more than one segment)
            {
                //Fuse first two segments:
                trail.RemoveAt(1);                                    //Remove second point from trail (combining first and second segments)
                totalTrailLength -= segLengths[0] + segLengths[1];    //Remove deleted segment lengths from total trail length
                segLengths.RemoveAt(0);                               //Remove segment from lengths list
                segLengths[0] = Vector2.Distance(trail[0], trail[1]); //Update new length of first segment
                totalTrailLength += segLengths[0];                    //Add new segment length to total
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
        if (context.performed) //Throw button has just been pressed
        {

        }
        else //Throw button has just been released
        {

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
        Vector3 spawnPoint = transform.position + settings.spawnOffset + spawnDirection;                   //Use position of mama rat plus offset and random bias as basis for spawnpoint

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
    /// Returns the point on trail which is closest to given reference point, but within given distance behind given trail value.
    /// </summary>
    /// <param name="origin">Reference point which returned position will be as close as possible to.</param>
    /// <param name="prevValue">Previous trail value (0 - 1) used to restrict section of checked trail. Pass negative for a clean check.</param>
    /// <param name="maxBackup">Maximum distance along trail by which returned point can be behind point at prevValue.</param>
    public TrailPointData GetClosestPointOnTrail(Vector3 origin, float prevValue = -1, float maxBackup = 0)
    {
        //Trim down trail:
        if (trail.Count == 1) return new TrailPointData(trail[0]); //Simply return only point in trail if applicable
        List<Vector2> tempTrail = new List<Vector2>(trail);        //Create a temporary clone of trail
        if (prevValue >= 0) //Only trim trail if a valid trail value is supplied
        {
            float valueRemaining = (prevValue * totalTrailLength) + maxBackup; //Get distance value of cut point in trail
            valueRemaining = Mathf.Clamp(valueRemaining, 0, totalTrailLength); //Clamp value to range of totalTrailLength
            for (int i = 0; i < segLengths.Count; i++) //Iterate through segment lengths list
            {
                if (segLengths[i] > valueRemaining) //PrevValue is within this segment
                {
                    tempTrail[i + 1] = Vector2.Lerp(tempTrail[i], tempTrail[i + 1], valueRemaining / segLengths[i]); //Shorten final segment in trail based on remaining value
                    while (tempTrail.Count > i + 2) tempTrail.RemoveAt(i + 2);                                       //Remove all points in temp trail which are outside given range
                    break;                                                                                           //Break for loop
                }
                else valueRemaining -= segLengths[i]; //Otherwise, subtract length of segment from remaining value and pass to next segment
            }
        }

        //Find closest point:
        Vector2 flatOrigin = RatBoid.FlattenVector(origin); //Get origin as flat vector (for efficiency)
        Vector2 pointA = tempTrail[0];                      //Initialize container for first point (will be closest point to start of trail)
        Vector2 pointB = tempTrail[1];                      //Initialize second point at second item in trail
        int closestIndex = 1;                               //Initialize container to store index of closest point (later switches use to index of earliest point in trail)
        if (tempTrail.Count > 2) //Only search harder if there are more than two points to check
        {
            //Get closest point:
            float closestDistance = Vector2.Distance(flatOrigin, pointB); //Initialize closest point tracker at distance between origin and second item in trail
            for (int i = 2; i < tempTrail.Count - 1; i++) //Iterate through points in trail which have two neighbors (and are not already point A)
            {
                float distance = Vector2.Distance(flatOrigin, tempTrail[i]); //Check distance between origin and point
                if (distance < closestDistance) //Current point is closer than previous closest point
                {
                    closestDistance = distance; //Store closest distance
                    closestIndex = i;           //Store closest index
                    pointA = tempTrail[i];      //Update point A
                }
            }

            //Get closest adjacent point:
            if (Vector2.Distance(flatOrigin, tempTrail[closestIndex - 1]) <= Vector2.Distance(flatOrigin, tempTrail[closestIndex + 1])) //Former point is closer to origin
            {
                pointB = pointA;                      //Make point B the latter point
                pointA = tempTrail[closestIndex - 1]; //Make point A the former point
                closestIndex -= 1;                    //Make closestIndex the index of point A
            }
            else //Latter point is closer to origin
            {
                pointB = tempTrail[closestIndex + 1]; //Make point B the latter point
            }
        }
        Vector2 closestPoint = GetClosestPointOnLine(pointA, pointB, flatOrigin); //Get closest point to target between two found points in trail

        //Get position of point in line:
        float trailValue = 0; //Initialize container to store total line distance
        for (int i = 0; i < closestIndex; i++) //Iterate through each segment before segment containing target
        {
            trailValue += segLengths[i]; //Add up segment lengths
        }
        trailValue += Vector2.Distance(closestPoint, pointB); //Add partial distance of current segment to total distance
        trailValue = trailValue / totalTrailLength;           //Get value as percentage of total length of trail

        //Return point data:
        if (closestPoint == tempTrail[0]) return new TrailPointData(closestPoint, (tempTrail[0] - tempTrail[1]).normalized, 0); //If the closest point is the very beginning of the trail, give it a direction which points toward the leader
        return new TrailPointData(closestPoint, -(pointB - pointA).normalized, trailValue);                                     //Otherwise, return closest point with known direction
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
    /// Returns point in trail which corresponds to given value.
    /// </summary>
    /// <param name="trailValue">Number between 0 and 1 representing position on trail (0 is the leader, 1 is the end).</param>
    public TrailPointData GetTrailPointFromValue(float trailValue)
    {
        //Edge cases:
        if (trail.Count < 2) return new TrailPointData(PosAsVector2());                                                                              //Return position of leader if trail has no segments
        if (trail.Count == 2) return new TrailPointData(Vector2.Lerp(trail[0], trail[1], trailValue), (trail[0] - trail[1]).normalized, trailValue); //Simply interpolate between only two points if possible

        //Find point:
        float distanceRemaining = trailValue * totalTrailLength; //Get amount of distance to scrub through
        for (int i = 0; i < segLengths.Count; i++) //Iterate through each segment in trail
        {
            if (segLengths[i] >= distanceRemaining) //Point is inside this segment
            {
                Vector2 point = Vector2.Lerp(trail[i], trail[i + 1], distanceRemaining / segLengths[i]); //Lerp to find point according to distance remaining within segment
                Vector2 direction = (trail[i + 1] - trail[i]).normalized;                                //Get direction of trail at this point
                return new TrailPointData(point, direction, trailValue);                                 //Return point data
            }
            else distanceRemaining -= segLengths[i]; //Otherwise, subtract segment length and move further down trail
        }

        //Point could not be found:
        Debug.LogError("GetTrailPointFromValue failed"); //Post error
        return new TrailPointData();                     //Return empty data container
    }

    //UTILITY METHODS:
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
    /// Returns current position of rat as 2D vector (relative to world down).
    /// </summary>
    public Vector2 PosAsVector2()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
    /// <summary>
    /// Updates stuff which depends on current number of follower rats. Should be called whenever follower count changes.
    /// </summary>
    public void OnFollowerCountChanged()
    {
        //Update UI:
        InterfaceMaster.SetCounter(followerRats.Count); //Set rat counter

        //Update swarm settings:
        if (currentSwarmSettings == null) //Swarm settings have not been set up yet
        {
            if (swarmSettings.Count == 0) { Debug.LogError("Big Rat needs at least one swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
            else if (swarmSettings.Count == 1) { currentSwarmSettings = swarmSettings[0]; }                                     //Just make currentSwarmSettings a reference if only one settings object is given
            else currentSwarmSettings = ScriptableObject.CreateInstance<SwarmSettings>();                                       //Create a temporary object instance for swarm settings as part of normal setup
        }
        if (swarmSettings.Count == 1) return; //Ignore if swarm settings are just a copy of single given settings object
        int ratNumber = followerRats.Count; //Get current number of follower rats
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
}

//BONEYARD:
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