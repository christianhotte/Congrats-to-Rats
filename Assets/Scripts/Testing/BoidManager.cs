using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoidManager : MonoBehaviour
{
    //Objects & Components:
    [SerializeField, Tooltip("Prefab object for spawned rats")]             private GameObject ratPrefab;
    [SerializeField, Tooltip("Object which spawned rats will seek toward")] private Transform targetRat;

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Height at which new rats will be spawned")]                           private float spawnHeight;
    [SerializeField, Tooltip("Random distance range by which newly-spawned rats will be offset")]   private float spawnPositionRandomness;
    [SerializeField, Min(0), Tooltip("Maximum speed at which rats can travel")]                     private float maxSpeed;
    [SerializeField, Min(0), Tooltip("Separation distance under which rats can affect each other")] private float neighborRadius;
    [SerializeField, Min(0), Tooltip("Target separation distance between rats")]                    private float separation;
    [Space()]
    [SerializeField, Min(0), Tooltip("Range at which adjacency to target will begin to slow rats down")] private float targetDragRange;
    [SerializeField, Tooltip("Curve describing drag depending on how close rats are to target")]         private AnimationCurve targetDragCurve;
    [Header("Rule Weights:")]
    [SerializeField, Tooltip("Influence clumping rule has on rat behavior")]           private float clumpWeight = 1;
    [SerializeField, Tooltip("Influence separation rule has on rat behavior")]         private float separationWeight = 1;
    [SerializeField, Tooltip("Influence conformance rule has on rat behavior")]        private float conformWeight = 1;
    [SerializeField, Tooltip("Influence target rule has on rat behavior")]             private float targetWeight = 1;
    [SerializeField, Tooltip("Influence target drag rule has on rat behavior")]        private float targetDragWeight = 1;
    [SerializeField, Tooltip("Influence camera confinement rule has on rat behavior")] private float cameraConfineWeight = 1;

    //Runtime vars:
    private List<Transform> rats = new List<Transform>(); //List of all spawned ratboids in scene

    //RUNTIME METHODS:
    private void FixedUpdate()
    {
        //Move ratboids:
        if (rats.Count > 0) SimulateRatMovement(Time.deltaTime); //Move ratboids if there is at least one ratboid in scene
    }
    private void SimulateRatMovement(float deltaTime)
    {
        foreach (Transform rat in rats) //Iterate through list of ratboids
        {
            //Initialize:
            List<Transform> others = new List<Transform>(rats); others.Remove(rat); //Get list of all rats excluding this one
            Vector2 pos = new Vector2(rat.position.x, rat.position.z);              //Get position of current rat
            Vector2 newVel = rat.GetComponent<TestBoidController>().velocity;       //Get velocity from rat

            //Find neighbors:
            List<Transform> separators = new List<Transform>(); //Initialize list to store rats which are within separation distance from this rat
            List<Transform> neighbors = new List<Transform>();  //Initialize list to store this rat's neighbors (NOTE: use ratController scripts to have neighbor rats autofill this information for each other)
            foreach (Transform otherRat in others) //Iterate through list of ratboids
            {
                Vector2 otherPos = new Vector2(otherRat.position.x, otherRat.position.z); //Get position of other rat in 2D space
                float distance = Vector2.Distance(pos, otherPos);                         //Get position difference between rats
                if (distance < separation) separators.Add(otherRat);                      //Add rat to separators list if it is close enough to be a separator
                if (distance < neighborRadius) neighbors.Add(otherRat);                   //Add rat to neighbors list if it is close enough to be a neighbor
            }

            //Do clumping rule:
            Vector2 center = GetCenterMass(others.ToArray()); //Get percieved center of mass
            Vector2 clumpingVel = center - pos;
            clumpingVel /= 100;

            //Do separation rule:
            Vector2 separationVel = Vector2.zero;
            foreach (Transform otherRat in separators)
            {
                separationVel += pos - new Vector2(otherRat.position.x, otherRat.position.z);
            }

            //Do conformance rule:
            Vector2 conformVel = Vector2.zero;
            if (neighbors.Count > 0)
            {
                foreach (Transform otherRat in neighbors)
                {
                    conformVel += otherRat.GetComponent<TestBoidController>().velocity;
                }
                conformVel /= neighbors.Count;
                conformVel -= rat.GetComponent<TestBoidController>().velocity;
                conformVel /= 8;
            }

            //Do target rule:
            Vector3 ratPosDiff = targetRat.transform.position - rat.transform.position;
            Vector2 targetVel = new Vector2(ratPosDiff.x, ratPosDiff.z); //Get direction of velocity moving rats toward target

            //Do camera confinement rule:


            //Apply rules:
            newVel += clumpingVel * clumpWeight;
            newVel += separationVel * separationWeight;
            newVel += conformVel * conformWeight;
            newVel += targetVel * targetWeight;

            //Do target drag rule (late):
            float targetDistance = ratPosDiff.magnitude; //Get distance between target and rat
            Vector2 targetDragVel = Vector2.zero;
            if (targetDistance <= targetDragRange) //Rat is close enough to target to induce drag
            {
                float dragInterpolant = Mathf.InverseLerp(targetDragRange, 0, targetDistance);
                targetDragVel = -newVel; //Initialize drag as reverse velocity of rat
                targetDragVel *= targetDragCurve.Evaluate(dragInterpolant); //Apply distance-sensitive intensity curve to drag force
            }
            newVel += targetDragVel * targetDragWeight;

            //Apply velocity:
            if (newVel.magnitude > maxSpeed) newVel = newVel.normalized * maxSpeed; //Clamp velocity
            rat.GetComponent<TestBoidController>().velocity = newVel;
            newVel *= deltaTime; //Temporarily apply deltaTime to velocity
            rat.position = new Vector3(rat.position.x + newVel.x, spawnHeight, rat.position.z + newVel.y);
        }
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
    public void OnMoveTarget(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();       //Get value from context
        Ray mouseRay = Camera.main.ScreenPointToRay(value); //Get ray from camera based on pointer position
        if (Physics.Raycast(mouseRay, out RaycastHit hit)) //See if ray is hitting floor plane
        {
            Vector3 targetPoint = hit.point;                      //Get point on plane to move target to
            targetPoint.y = spawnHeight * targetRat.localScale.x; //Use rat scale and spawn height to find static ground offset for target
            targetRat.position = targetPoint;                     //Move target to computed point
        }
    }
    public void OnScrollSpawn(InputAction.CallbackContext context)
    {
        if (context.started) //Scroll wheel has just been moved one tick
        {
            if (context.ReadValue<float>() > 0) SpawnRat(); //Spawn rats when wheel is scrolled up
            else DespawnRat(rats.Count - 1);                //Despawn rats when wheel is scrolled down
        }
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
        centerMass.x += Random.Range(-spawnPositionRandomness, spawnPositionRandomness);
        centerMass.y += Random.Range(-spawnPositionRandomness, spawnPositionRandomness);
        newRat.position = new Vector3(centerMass.x, spawnHeight, centerMass.y);      //Move rat to center of rat swarm at set spawn height
        newRat.eulerAngles = new Vector3(0, GetAverageAlignment(rats.ToArray()), 0); //Rotate rat to match rotation of other rats
        newRat.name = 0.ToString();                                                  //Set name (used to track velocity) to zero
        rats.Add(newRat);                                                            //Add new rat to running list
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
