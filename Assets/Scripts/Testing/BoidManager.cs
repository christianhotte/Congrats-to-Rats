using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoidManager : MonoBehaviour
{
    //Objects & Components:
    [SerializeField, Tooltip("Prefab object for spawned rats")] private GameObject ratPrefab;

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Height at which new rats will be spawned")]              private float spawnHeight;
    [SerializeField, Tooltip("Maximum speed of rat movement (in units per second)")]   private float maxMoveSpeed;
    [SerializeField, Tooltip("Maximum speed of rat rotation (in degrees per second)")] private float maxRotationSpeed;
    [SerializeField, Tooltip("Target separation distance between rats")]               private float separation;
    [Header("Rule Weights:")]
    [SerializeField, Range(0, 1), Tooltip("Influence separation rule has on rat behavior")] private float separationWeight = 1;

    //Runtime vars:
    private List<Transform> rats = new List<Transform>(); //List of all spawned ratboids in scene
    private List<Vector2> ratVels = new List<Vector2>();  //List of velocities corresponding to ratboids in scene

    //RUNTIME METHODS:
    private void Update()
    {
        //Move ratboids:
        if (rats.Count > 0) SimulateRatMovement(Time.deltaTime); //Move ratboids if there is at least one ratboid in scene
    }
    private void SimulateRatMovement(float deltaTime)
    {
        //Initializations:
        
    }

    //RAT RULES:


    //INPUT METHODS:
    public void OnSpawnRat(InputAction.CallbackContext context)
    {
        if (context.started) SpawnRat(); //Spawn a new rat on button press
    }
    public void OnRemoveRat(InputAction.CallbackContext context)
    {
        if (context.started) DespawnRat(rats.Count - 1); //Remove last rat in list on button prest
    }
    public void OnAltRemoveRat(InputAction.CallbackContext context)
    {
        if (context.started) DespawnRat(0); //Remove first rat in list on button press
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Spawns a rat at center mass of current swarm.
    /// </summary>
    public void SpawnRat()
    {
        //Rat spawn protocol:
        Transform newRat = Instantiate(ratPrefab).transform;                         //Instantiate a version of rat prefab and get its transform
        Vector2 centerMass = GetCenterMass(rats.ToArray());                          //Get center mass of all rats in scene
        newRat.position = new Vector3(centerMass.x, spawnHeight, centerMass.y);      //Move rat to center of rat swarm at set spawn height
        newRat.eulerAngles = new Vector3(0, GetAverageAlignment(rats.ToArray()), 0); //Rotate rat to match rotation of other rats
        rats.Add(newRat);                                                            //Add new rat to running list
        ratVels.Add(Vector2.zero);                                                   //Add velocity tracker to running list
    }
    /// <summary>
    /// Despawns the rat at given index.
    /// </summary>
    /// <param name="ratIndex"></param>
    public void DespawnRat(int ratIndex)
    {
        //Validity checks:
        if (rats.Count <= ratIndex || ratIndex < 0) return; //Cancel if there is no rat at desired index (or if index is below zero for some reason)

        //Rat removal procedure:
        Transform rat = rats[ratIndex]; //Get rat from given index
        rats.RemoveAt(ratIndex);        //Remove rat from rat list
        ratVels.RemoveAt(ratIndex);     //Remove velocity tracker from list
        Destroy(rat.gameObject);        //Destroy rat object
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns center of given group of rats.
    /// </summary>
    private Vector2 GetCenterMass(Transform[] ratGroup)
    {
        //Validity checks:
        if (ratGroup.Length == 0) return Vector2.zero; //Return zero if given group is empty

        //Get center of given transforms:
        Vector2 totalPos = Vector2.zero;                                                             //Initialize container to store total position of all group members
        foreach (Transform rat in ratGroup) totalPos += new Vector2(rat.position.x, rat.position.z); //Get total planar position of all rats in group
        return totalPos / ratGroup.Length;                                                           //Return average position of all given rats
    }
    /// <summary>
    /// Returns average alignment of given group of rats.
    /// </summary>
    private float GetAverageAlignment(Transform[] ratGroup)
    {
        //Validity checks:
        if (ratGroup.Length == 0) return 0; //Return zero if given group is empty

        //Get average rotation of given transforms:
        float totalRot = 0;                                                //Initialize container to store total rotation of all group members
        foreach (Transform rat in ratGroup) totalRot += rat.eulerAngles.y; //Add rotation of each rat in group
        return totalRot / ratGroup.Length;                                 //Return average rotation of all rats in group
    }
}
