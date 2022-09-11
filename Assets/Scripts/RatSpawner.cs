using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains data used to spawn rats at contextual positions.
/// </summary>
public class RatSpawner : MonoBehaviour
{
    //Static Vars:
    /// <summary>
    /// List of all rat spawners in scene.
    /// </summary>
    public static List<RatSpawner> spawners = new List<RatSpawner>();

    //Settings:
    [Header("Settings:")]
    [SerializeField, Min(0), Tooltip("Radius of random circle around which this point can spawn rats")] private float randomness;

    //Runtime Vars:
    internal Vector2 point; //Position of this point as a 2D vector

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialize:
        spawners.Add(this);                                              //Add this spawner to list
        point = new Vector2(transform.position.x, transform.position.z); //Get position of spawner
    }
    private void OnDestroy()
    {
        //Cleanup:
        spawners.Remove(this); //Remove this spawner from list
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Returns a random spawnpoint within given radius of given point.
    /// </summary>
    public static Vector2 GetPointWithinRadius(Vector2 origin, float radius)
    {
        Vector2[] validPoints = GetValidPoints(origin, radius);                                                                                        //Get list of valid spawnpoints
        if (validPoints.Length == 0) return new Vector2(MasterRatController.main.transform.position.x, MasterRatController.main.transform.position.z); //Just return position of big rat if no valid spawners are nearby
        return validPoints[Random.Range(0, validPoints.Length)];                                                                                       //Return random valid point
    }
    /// <summary>
    /// Returns a number of random spawnpoints within given radius of given point (some may be duplicates).
    /// </summary>
    /// <param name="origin">Point which spawnpoints will be close to.</param>
    /// <param name="radius">Maximum distance points may be from origin.</param>
    /// <param name="quantity">Number of random points to return (points may repeat tetris-style).</param>
    /// <returns></returns>
    public static Vector2[] GetPointsWithinRadius(Vector2 origin, float radius, int quantity)
    {
        //Initialization:
        List<Vector2> remainingPoints = new List<Vector2>(GetValidPoints(origin, radius)); //Create a temporary list of valid spawnpoints
        if (remainingPoints.Count == 0) return remainingPoints.ToArray();                  //Just return empty array if no valid spawners are nearby
        List<Vector2> returnPoints = new List<Vector2>();                                  //Initialize list to store return points in

        //Randomize spawn points:
        while (returnPoints.Count < quantity) //Iterate until desired quantity of spawnpoints has been returned
        {
            if (remainingPoints.Count == 0) remainingPoints = new List<Vector2>(GetValidPoints(origin, radius)); //Refill list of remaining points if empty (using new array so that individual points are re-randomized)
            int index = Random.Range(0, remainingPoints.Count);                                                  //Get random index of point remaining in list
            returnPoints.Add(remainingPoints[index]);                                                            //Add point to returnPoints list
            remainingPoints.RemoveAt(index);                                                                     //Remove point from remaining points list (so that it is never selected more than twice in a row)
        }
        return returnPoints.ToArray(); //Return random points as array
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns list of spawnpoints which are within given radius of given point in space (points are randomized according to parameters of individual spawners).
    /// </summary>
    private static Vector2[] GetValidPoints(Vector2 origin, float radius)
    {
        List<Vector2> validPoints = new List<Vector2>(); //Initialize list to store valid found points
        foreach (RatSpawner spawner in spawners) //Iterate through all spawners in scene
        {
            if (Vector2.Distance(spawner.point, origin) <= radius) //Spawner is an acceptable distance from given point
            {
                Vector2 spawnPoint = spawner.point; //Get temporary version of this spawners point
                if (spawner.randomness > 0) //Spawner is using point randomization
                {
                    spawnPoint.x += Random.Range(-spawner.randomness, spawner.randomness); //Add randomness to X axis of spawnpoint
                    spawnPoint.y += Random.Range(-spawner.randomness, spawner.randomness); //Add randomness to Y axis of spawnpoint
                }
                validPoints.Add(spawnPoint); //Add generated point to list
            }
        }
        return validPoints.ToArray(); //Return found list as array
    }
}
