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

    //Objects & Components:
    private SpriteRenderer r; //Render component for this rat's sprite

    //Settings: NOTE: Put these in a scriptableObject
    [Header("Settings:")]
    [Min(0), Tooltip("Maximum allowed variance in scale (can go up or down)")]     public float scaleRandomness;
    [Tooltip("Curve describing motion of bob movement")]                           public AnimationCurve bobCurve;
    [Tooltip("Default height at which rat hovers")]                                public float baseHeight;
    [Tooltip("Maximum height mouse can naturally bob to")]                         public float maxBobHeight;
    [Tooltip("Speed of bob cycle")]                                                public float bobTime;
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
    
    private float timeUntilFlip;  //Time before this rat is able to flip its sprite orientation (prevents jiggling)
    private float currentBobTime; //Current position of rat in bob cycle
    
    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        r = GetComponentInChildren<SpriteRenderer>(); //Get renderer from child object

        //Initialization:
        spawnedRats.Add(this); //Add this rat to master list of spawned rats

        //Randomize appearance:
        float newScale = 1 + Random.Range(-scaleRandomness, scaleRandomness); //Get value of new scale
        transform.localScale = Vector3.one * newScale;                        //Set new scale
        r.color = Color.Lerp(r.color, altColor, Random.value);                //Randomize color
        currentBobTime = Random.Range(0, bobTime);                            //Randomize point in bob cycle
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
                if (velocity.x < 0) r.flipX = false;
                else if (velocity.x > 0) r.flipX = true;
                if (prevFlip != r.flipX) timeUntilFlip = timeBetweenFlips;
            }
        }
        else if (velocity.x < 0 && r.flipX || velocity.x > 0 && r.flipX)
        {
            r.flipX = !r.flipX;
            timeUntilFlip = timeBetweenFlips;
        }

        //Adjust bob height (TEMP):
        currentBobTime += Time.deltaTime;
        if (currentBobTime > bobTime) currentBobTime -= bobTime;
        DoBob();
    }

    //STATIC METHODS:
    /// <summary>
    /// Updates status and positions of all spawned rats.
    /// </summary>
    /// <param name="deltaTime"></param>
    public static void UpdateRats(float deltaTime, SwarmSettings settings)
    {
        //Update rat status & data:
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            //Update rat position:
            rat.flatPos += rat.velocity * deltaTime;                                                      //Apply velocity to rat position tracker
            rat.transform.position = new Vector3(rat.flatPos.x, rat.transform.position.y, rat.flatPos.y); //Move rat to match new position

            //Reset relationship lists:
            rat.currentNeighbors = new List<RatBoid>();  //Clear neighbors list
            rat.currentSeparators = new List<RatBoid>(); //Clear separators list
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
                            rat.currentSeparators.Add(otherRat); //Add other rat to separators list
                            otherRat.currentSeparators.Add(rat); //Add this rat to other rat's separators list
                        }
                    }
                }
            }
        }

        //RAT RULES:
        float leaderSpeed = MasterRatController.main.velocity.magnitude; //Pre-emptively get velocity of leader
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            if (rat.currentNeighbors.Count != 0) //Rat must have neighbors for these rules to apply
            {
                //RULE - Cohesion: (nearby rats stick together)
                Vector2 localCenterMass = GetCenterMass(rat.currentNeighbors.ToArray()); //Find center mass of this rat's current neighborhood
                Vector2 cohesionVel = localCenterMass - rat.flatPos;                     //Get velocity vector representing direction and magnitude of rat separation from center mass
                cohesionVel = (cohesionVel / 100) * settings.cohesionWeight;             //Apply weight and balancing values to cohesion velocity
                rat.velocity += Vector2.ClampMagnitude(cohesionVel, settings.maxSpeed);  //Apply clamped velocity to rat

                //RULE - Conformity: (rats tend to match the velocity of nearby rats)
                Vector2 conformVel = Vector2.zero; //Initialize container to store gross conformance-induced velocity
                foreach (RatBoid otherRat in rat.currentNeighbors) //Iterate through list of rats which are close enough to influence this rat
                {
                    conformVel += otherRat.velocity; //Add velocity of rat to local velocity total
                }
                conformVel /= rat.currentNeighbors.Count;                  //Get average velocity of neighbor rats
                conformVel = (conformVel / 8) * settings.conformityWeight; //Apply weight and balancing values to conformance velocity
                rat.velocity += Vector2.ClampMagnitude(conformVel, settings.maxSpeed); //Apply clamped velocity to rat
            }

            //RULE - Separation: (rats maintain a small distance from each other)
            if (rat.currentSeparators.Count != 0) //Only do separation if rat has separators
            {
                Vector2 separationVel = Vector2.zero; //Initialize container to store gross separation-induced velocity
                foreach (RatBoid otherRat in rat.currentSeparators) //Iterate through list of rats which are currently too close to this rat
                {
                    separationVel += rat.flatPos - otherRat.flatPos; //Add velocity which moves these rats away from each other
                }
                separationVel *= settings.separationWeight;                               //Apply weight setting to separation rule
                rat.velocity += Vector2.ClampMagnitude(separationVel, settings.maxSpeed); //Apply clamped velocity to rat
            }

            if (rat.follower) //Rat must be a follower for these rules to apply
            {
                MasterRatController.TrailPointData data = MasterRatController.main.GetClosestPointOnTrail(rat.flatPos); //Get data for point closest to rat position
                Vector2 target = data.point;                                                                            //Get position of target from data
                float targetDistance = Vector2.Distance(rat.flatPos, target);                                           //Get separation between rat and target
                if (targetDistance > settings.targetRadius) //Rat must be outside target distance for these rules to apply
                {
                    //RULE - Targeting: (rats tend to move toward target)
                    Vector2 targetVel = (target - rat.flatPos).normalized;                //Get direction toward target
                    targetVel *= 0.01f * settings.targetWeight;                           //Apply weight and balancing values to target velocity
                    rat.velocity += Vector2.ClampMagnitude(targetVel, settings.maxSpeed); //Apply clamped velocity to rat
                }
                else //Rat must be within target distance for these rules to apply
                {
                    //RULE - Following: (rats on a trail will move along it toward the leader)
                    Vector2 followVel = data.forward;                                     //Get follow velocity from forward direction of trail
                    followVel = (followVel / 100) * settings.followWeight;                //Apply weight and balancing values to follow velocity
                    followVel *= settings.EvaluateFollowStrength(data.linePosition);      //Modify follow strength depending on where target is along line
                    rat.velocity += Vector2.ClampMagnitude(followVel, settings.maxSpeed); //Apply clamped velocity to rat
                    
                    //RULE - Leading: (rats on a trail will speed up when leader is moving)
                    Vector2 leadVel = data.forward * leaderSpeed;                       //Get lead velocity from forward direction of trail and speed of leader
                    leadVel *= settings.leadWeight;                                     //Apply weight value to lead velocity
                    rat.velocity += Vector2.ClampMagnitude(leadVel, settings.maxSpeed); //Apply clamped velocity to rat
                }

                if (data.linePosition <= 0) //Rat must be ahead of line for these rules to apply
                {
                    //RULE - Staying Behind: (rats in front of leader tend to want to get behind it)
                    Vector2 stayBackVel = -MasterRatController.main.forward;                //Make stay back velocity directly oppose facing direction of leader
                    stayBackVel *= settings.stayBackWeight;                                 //Apply weight value to stay back velocity
                    rat.velocity += Vector2.ClampMagnitude(stayBackVel, settings.maxSpeed); //Apply clamped velocity to rat
                }
                else if (data.linePosition >= 1) //Rat must be behind line for these rules to apply
                {
                    //RULE - Straggler Prevention: (rats behind the line will speed up)
                    Vector2 stragglerVel = data.forward;
                    stragglerVel *= settings.stragglerWeight;
                    rat.velocity += Vector2.ClampMagnitude(stragglerVel, settings.maxSpeed);;
                }
            }

            //RULE - Velocity Clamping: (rats cannot go faster than their max speed)
            rat.velocity = Vector2.ClampMagnitude(rat.velocity, settings.maxSpeed); //Clamp rat velocity to maximum speed
        }
    }

    //UTILITY METHODS:
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
    private void DoBob()
    {
        float t = bobCurve.Evaluate(currentBobTime / bobTime);
        float scaleMultiplier = (1 - transform.localScale.x) + 1;
        float currentMaxHeight = maxBobHeight * scaleMultiplier;
        float height = Mathf.Lerp(baseHeight, currentMaxHeight, t);
        Vector3 newPos = transform.position; newPos.y = height;
        transform.position = newPos;
    }
}
