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
        //Get list of valid points:
        List<Vector2> validPoints = new List<Vector2>(); //Initialize list to store valid found points
        foreach (RatSpawner spawner in spawners) //Iterate through all spawners in scene
        {
            if (Vector2.Distance(spawner.point, origin) <= radius) validPoints.Add(spawner.point); //Add spawner to list if it is within given radius
        }

        //Return random point:
        if (validPoints.Count == 0) return new Vector2(MasterRatController.main.transform.position.x, MasterRatController.main.transform.position.z); //Just return position if big rat if no valid spawners are nearby
        return validPoints[Random.Range(0, validPoints.Count)];                                                                                       //Return random valid point
    }
    /*
    /// <summary>
    /// Returns a number of random spawnpoints within given radius of given point.
    /// </summary>
    /// <param name="origin">Point which spawnpoints will be close to.</param>
    /// <param name="radius">Maximum distance points may be from origin.</param>
    /// <param name="quantity">Number of random points to return (points may repeat tetris-style).</param>
    /// <returns></returns>
    public static Vector2[] GetPointsWithinRadius(Vector2 origin, float radius, int quantity)
    {

    }*/
}
