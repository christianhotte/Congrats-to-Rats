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

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        spawnedRats.Add(this); //Add this rat to master list of spawned rats
    }
    private void OnDestroy()
    {
        //Final cleanup:
        spawnedRats.Remove(this); //Remove this rat from master list of spawned rats
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
            rat.flatPos += rat.velocity * deltaTime;                                                    //Apply velocity to rat position tracker
            rat.transform.position = new Vector3(rat.flatPos.x, settings.baseRatHeight, rat.flatPos.y); //Move rat to match new position

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
        foreach (RatBoid rat in spawnedRats) //Iterate through every rat in list
        {
            
            if (rat.currentNeighbors.Count != 0) //Rat must have neighbors for these rules to apply
            {
                //RULE #1 - Cohesion: (nearby rats stick together)
                Vector2 localCenterMass = GetCenterMass(rat.currentNeighbors.ToArray()); //Find center mass of this rat's current neighborhood
                Vector2 cohesionVel = localCenterMass - rat.flatPos;                     //Get velocity vector representing direction and magnitude of rat separation from center mass
                cohesionVel = (cohesionVel / 100) * settings.cohesionWeight;             //Apply weight and balancing values to cohesion velocity
                rat.velocity += cohesionVel;                                             //Apply cohesion-induced velocity to rat

                //RULE #2 - Conformity: (rats tend to match the velocity of nearby rats)
                Vector2 conformVel = Vector2.zero; //Initialize container to store gross conformance-induced velocity
                foreach (RatBoid otherRat in rat.currentNeighbors) //Iterate through list of rats which are close enough to influence this rat
                {
                    conformVel += otherRat.velocity; //Add velocity of rat to local velocity total
                }
                conformVel /= rat.currentNeighbors.Count;                  //Get average velocity of neighbor rats
                conformVel = (conformVel / 8) * settings.conformityWeight; //Apply weight and balancing values to conformance velocity
                rat.velocity += conformVel;                                //Apply conformance-induced velocity to rat
            }

            //RULE #3 - Separation: (rats maintain a small distance from each other)
            if (rat.currentSeparators.Count != 0) //Only do separation if rat has separators
            {
                Vector2 separationVel = Vector2.zero; //Initialize container to store gross separation-induced velocity
                foreach (RatBoid otherRat in rat.currentSeparators) //Iterate through list of rats which are currently too close to this rat
                {
                    separationVel += rat.flatPos - otherRat.flatPos; //Add velocity which moves these rats away from each other
                }
                separationVel *= settings.separationWeight; //Apply weight setting to separation rule
                rat.velocity += separationVel;              //Apply separation-induced velocity to rat
            }

            if (rat.follower) //Rat must be a follower for these rules to apply
            {
                //RULE #4 - Targeting: (rats tend to move toward a target)
                MasterRatController.TrailPointData data = MasterRatController.main.GetClosestPointOnTrail(rat.flatPos); //Get data for point closest to rat position
                Vector2 target = data.point;                                                                            //Get position of target from data
                Vector2 targetVel = target - rat.flatPos;                                                               //Get velocity which directs rat toward target
                targetVel *= settings.targetWeight;                                                                     //Apply weight value to target velocity
                rat.velocity += targetVel;                                                                              //Apply target-induced velocity to rat

                //RULE #5 - Following: (rats on a trail will move along it when the leader moves)
                Vector2 followVel = data.forward * MasterRatController.main.velocity.magnitude; //Get follow velocity from forward direction of trail and speed of main rat
                followVel = (followVel / 100) * settings.followWeight;                          //Apply weight and balancing values to follow velocity
                rat.velocity += followVel;                                                      //Apply follow-induced velocity to rat
            }

            //RULE #6 - Clamping: (rats cannot go faster than their max speed)
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
}
