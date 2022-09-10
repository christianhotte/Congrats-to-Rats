using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

/// <summary>
/// Controls the big rat and governs behavior of all the little rats.
/// </summary>
public class MasterRatController : MonoBehaviour
{
    //Static Stuff:
    /// <summary>
    /// Singleton instance of Big Rat in scene.
    /// </summary>
    public static MasterRatController main;

    //Objects & Components:
    private SpriteRenderer sprite; //Sprite renderer component for big rat

    [Header("Settings:")]
    [SerializeField, Tooltip("Interchangeable data object describing movement settings of main rat")] private BigRatSettings settings;
    [SerializeField, Tooltip("Interchangeable data object describing rat horde behavior")]            private SwarmSettings swarmSettings;

    //Runtime Vars:
    internal List<RatBoid> followerRats = new List<RatBoid>(); //List of all rats currently following this controller
    private List<Vector2> trail = new List<Vector2>();         //List of points in current trail (used to assemble ratswarm behind main rat)
    private float totalTrailLength = 0;                        //Current length of trail

    private Vector2 velocity;  //Current speed and direction of movement
    private Vector2 moveInput; //Current input direction for movement

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (settings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); }        //Make sure big rat has ratSettings
        if (swarmSettings == null) { Debug.LogError("Big Rat is missing swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
        if (main == null) main = this; else Destroy(this);                                                       //Singleton-ize this script instance
        trail.Insert(0, PosAsVector2());                                                                         //Add starting position as first point in trail
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>(); //Get spriteRenderer component
    }
    private void Update()
    {
        MoveRat(Time.deltaTime);      //Move the big rat
        MoveRatBoids(Time.deltaTime); //Move all the little rats

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
        else return; //Return if velocity is zero and no input is given
        float currentSpeed = velocity.magnitude; //Get current speed of main rat
        if (currentSpeed > settings.speed) //Check if current speed is faster than target speed
        {
            velocity = Vector2.ClampMagnitude(velocity, settings.speed); //Clamp velocity to target speed
            currentSpeed = settings.speed;                               //Update current speed value
        }

        //Get new position:
        Vector3 newPos = transform.position; //Get current position of rat
        newPos.x += velocity.x * deltaTime;  //Add X velocity over time to position
        newPos.z += velocity.y * deltaTime;  //Add Y velocity over time to position
        transform.position = newPos;         //Apply new position

        //Update trail characteristics:
        trail.Insert(0, PosAsVector2());                        //Add new trailPoint for current position
        float segLength = Vector2.Distance(trail[0], trail[1]); //Get length of new segment being created
        totalTrailLength += segLength;                          //Add length of new segment to total length of trail
        if (trail.Count > 2) //Only perform culling operations if trail is long enough
        {
            //Grow first segment to minimum length:
            float secondSegLength = Vector2.Distance(trail[1], trail[2]); //Get length of second segment in trail
            if (secondSegLength < settings.minTrailSegLength) //Check if second segment in trail is too short (first segment can be any length)
            {
                totalTrailLength -= segLength + secondSegLength;  //Subtract lengths of both removed segments from total
                trail.RemoveAt(1);                                //Remove second segment from trail
                segLength = Vector2.Distance(trail[0], trail[1]); //Get new length of first segment
                totalTrailLength += segLength;                    //Add length of new segment back to total
            }

            //Limit trail length:
            float targetTrailLength = (followerRats.Count * settings.trailLengthPerRat) + 0.01f;                  //Get target trail length based off of follower count and settings
            targetTrailLength *= Mathf.Lerp(1, settings.velTrailLengthMultiplier, currentSpeed / settings.speed); //Apply velocity-based length multiplier to target trail length
            while (totalTrailLength > targetTrailLength) //Current trail is longer than target length (and is non-zero)
            {
                float extraLength = totalTrailLength - targetTrailLength;     //Get amount of extra length left in trail
                float lastSegLength = Vector2.Distance(trail[^1], trail[^2]); //Get distance between last two segments in trail
                if (extraLength >= lastSegLength) //Last segment is shorter than length which needs to be removed
                {
                    trail.RemoveAt(trail.Count - 1);   //Remove last segment from trail
                    totalTrailLength -= lastSegLength; //Subtract length of removed segment from total
                }
                else //Last segment is longer than length which needs to be removed
                {
                    trail[^1] = Vector2.MoveTowards(trail[^1], trail[^2], extraLength); //Shorten last segment by extra length
                    totalTrailLength -= extraLength;                                    //Subtract remaining extra length from total
                }
            }

            //Check for crossed lines:
            
        }
    }
    /// <summary>
    /// Moves all RatBoids currently following this rat.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void MoveRatBoids(float deltaTime)
    {

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
    /// Returns the point on trail which is closest to given reference point (NOTE: very expensive).
    /// </summary>
    public Vector2 GetClosestPointOnTrail(Vector2 origin)
    {
        //Validity checks:
        if (trail.Count == 1) return trail[0]; //Simply return only point in trail if applicable

        //Find two closest adjacent points on trail:
        Vector2 pointA = trail[1]; //Initialize first point at second item in trail
        Vector2 pointB = trail[0]; //Initialize second point at first item in trail
        if (trail.Count > 2) //Only search harder if there are more than two points to check
        {
            //Get closest point:
            float closestDistance = Vector2.Distance(origin, pointA); //Initialize closest point tracker at distance between origin and point A
            int closestIndex = 1;                                     //Initialize container to store index of closest point
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
            if (Vector2.Distance(origin, trail[closestIndex - 1]) <= Vector2.Distance(origin, trail[closestIndex + 1])) pointB = trail[closestIndex - 1]; //Left adjacent point is closer
            else { pointB = trail[closestIndex + 1]; } //Right adjacent point is closer
        }

        //Get closest point between points:
        Vector2 dir = pointB - pointA;                 //Get direction of line between two points
        float lineLength = dir.magnitude;              //Get distance between points
        dir.Normalize();                               //Normalize directional vector
        Vector2 lhs = origin - pointA;                 //Get the left hand side vector
        float product = Vector2.Dot(lhs, dir);         //Get dot product of direction and side
        product = Mathf.Clamp(product, 0, lineLength); //Clamp length to make sure projection is on line
        return pointA + (dir * product);               //Return point on line which is closest to given origin
    }
    /// <summary>
    /// Spawns a new rat and adds it to the swarm.
    /// </summary>
    public void AddRat(GameObject prefab)
    {
        //Spawn rat:
        Vector2 spawnPoint = RatSpawner.GetPointWithinRadius(PosAsVector2(), settings.spawnRadius); //Get spawnpoint
        Transform newRat = Instantiate(prefab).transform;                                           //Spawn new rat
        newRat.position = new Vector3(spawnPoint.x, swarmSettings.height, spawnPoint.y);            //Move rat to spawn position
        followerRats.Add(newRat.GetComponent<RatBoid>());                                           //Add rat to followers
    }
    /// <summary>
    /// Removes a specific rat from the swarm.
    /// </summary>
    public void RemoveRat(RatBoid target)
    {
        //Despawn rat:
        followerRats.Remove(target); //Remove rat from followers
        Destroy(target.gameObject);  //Destroy rat
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns current position of rat as 2D vector (relative to world down).
    /// </summary>
    private Vector2 PosAsVector2()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
}
