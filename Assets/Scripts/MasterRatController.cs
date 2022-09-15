using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;

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

    [Header("Settings:")]
    [SerializeField, Tooltip("Interchangeable data object describing movement settings of main rat")]                               private BigRatSettings settings;
    [SerializeField, Tooltip("Place any number of swarm settings objects here (make sure they have different Target Rat Numbers)")] private List<SwarmSettings> swarmSettings = new List<SwarmSettings>();

    //Runtime Vars:
    private SwarmSettings currentSwarmSettings;                //Instance of swarmSettings object used to interpolate between rat behaviors
    internal List<RatBoid> followerRats = new List<RatBoid>(); //List of all rats currently following this controller
    private List<Vector2> trail = new List<Vector2>();         //List of points in current trail (used to assemble ratswarm behind main rat)
    private List<float> segLengths = new List<float>();        //List of lengths corresponding to segments in trail
    private float totalTrailLength = 0;                        //Current length of trail

    internal Vector2 velocity;   //Current speed and direction of movement
    internal Vector2 forward;    //Normalized vector representing which direction big rat was most recently moving
    internal float currentSpeed; //Current speed at which rat is moving
    private Vector2 moveInput;   //Current input direction for movement

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (settings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); } //Make sure big rat has ratSettings
        if (main == null) main = this; else Destroy(this);                                                //Singleton-ize this script instance
        trail.Insert(0, PosAsVector2());                                                                  //Add starting position as first point in trail
        UpdateSwarmSettings();                                                                            //Set up swarm settings and do initial update
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>(); //Get spriteRenderer component
    }
    private void Update()
    {
        MoveRat(Time.deltaTime);                                  //Move the big rat
        RatBoid.UpdateRats(Time.deltaTime, currentSwarmSettings); //Move all the little rats

        UpdateSwarmSettings(); //TEMP: Keep swarm settings up-to-date for debugging purposes

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
            if (moveInput.x != 0) sprite.flipX = moveInput.x > 0; //Flip sprite to direction of horizontal move input
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

        //Get new position:
        if (velocity != Vector2.zero) //Only update position if player has velocity
        {
            forward = velocity.normalized;       //Update forward direction tracker

            Vector3 newPos = transform.position; //Get current position of rat
            newPos.x += velocity.x * deltaTime;  //Add X velocity over time to position
            newPos.z += velocity.y * deltaTime;  //Add Y velocity over time to position
            transform.position = newPos;         //Apply new position
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

            //Check for kink in line:
            //NOTE: Check angle between adjacent points and delete points until angles are good (this probably only has to be done for first few points)
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
            if (context.ReadValue<float>() > 0) AddRat(settings.basicRatPrefab); //Spawn rats when wheel is scrolled up
            else if (followerRats.Count > 0) RemoveRat(followerRats[^1]);        //Despawn rats when wheel is scrolled down
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Spawns a new rat and adds it to the swarm.
    /// </summary>
    public void AddRat(GameObject prefab)
    {
        //Initialize:
        Vector2 spawnPoint = RatSpawner.GetPointWithinRadius(PosAsVector2(), settings.spawnRadius); //Get spawnpoint
        Transform newRat = Instantiate(prefab).transform;                                           //Spawn new rat
        RatBoid ratController = newRat.GetComponent<RatBoid>();                                     //Get controller from spawned rat
        
        //Set position:
        newRat.position = new Vector3(spawnPoint.x, 0, spawnPoint.y); //Move rat to spawn position
        ratController.flatPos = spawnPoint;                           //Update flat position tracker of spawned rat

        //Set as follower:
        ratController.follower = true;   //Indicate that new rat is currently following big rat
        followerRats.Add(ratController); //Add rat to followers
        UpdateSwarmSettings();           //Update settings
    }
    /// <summary>
    /// Removes a specific rat from the swarm.
    /// </summary>
    public void RemoveRat(RatBoid target)
    {
        //Despawn rat:
        followerRats.Remove(target); //Remove rat from followers
        UpdateSwarmSettings();       //Update settings
        Destroy(target.gameObject);  //Destroy rat
    }
    /// <summary>
    /// Returns the point on trail which is closest to given reference point (NOTE: very expensive).
    /// </summary>
    public TrailPointData GetClosestPointOnTrail(Vector2 origin)
    {
        //Validity checks:
        if (trail.Count == 1) return new TrailPointData(trail[0]); //Simply return only point in trail if applicable

        //Find two closest adjacent points on trail:
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

        //Get closest point between points:
        Vector2 dir = pointB - pointA;                   //Get direction of line between two points
        float lineLength = dir.magnitude;                //Get distance between points
        dir.Normalize();                                 //Normalize directional vector
        Vector2 lhs = origin - pointA;                   //Get the left hand side vector
        float product = Vector2.Dot(lhs, dir);           //Get dot product of direction and side
        product = Mathf.Clamp(product, 0, lineLength);   //Clamp length to make sure projection is on line
        Vector2 closestPoint = pointA + (dir * product); //Do projection to get actual closest point on trail

        //Get position of point in line:
        float trailValue = 0; //Initialize container to store total line distance
        for (int i = 0; i < closestIndex; i++) //Iterate through each segment before segment containing target
        {
            trailValue += segLengths[i]; //Add up segment lengths
        }
        trailValue += Vector2.Distance(closestPoint, pointB); //Add partial distance of current segment to total distance
        trailValue = trailValue / totalTrailLength;           //Get value as percentage of total length of trail

        //Return point data:
        if (closestPoint == trail[0]) return new TrailPointData(closestPoint); //If the closest point is the very beginning of the trail, give it no direction
        return new TrailPointData(closestPoint, -dir.normalized, trailValue);  //Otherwise, return closest point with known direction
        
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns current position of rat as 2D vector (relative to world down).
    /// </summary>
    public Vector2 PosAsVector2()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
    /// <summary>
    /// Updates currentSwarmSettings according to given settings list and current number of rats. Should be done every time follower count changes, and at beginning of runtime.
    /// </summary>
    private void UpdateSwarmSettings()
    {
        //Setup:
        if (currentSwarmSettings == null) //Swarm settings have not been set up yet
        {
            if (swarmSettings.Count == 0) { Debug.LogError("Big Rat needs at least one swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
            else if (swarmSettings.Count == 1) { currentSwarmSettings = swarmSettings[0]; }                                     //Just make currentSwarmSettings a reference if only one settings object is given
            else currentSwarmSettings = ScriptableObject.CreateInstance<SwarmSettings>();                                       //Create a temporary object instance for swarm settings as part of normal setup
        }
        if (swarmSettings.Count == 1) return; //Ignore if swarm settings are just a copy of single given settings object

        //Get interpolant settings:
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
