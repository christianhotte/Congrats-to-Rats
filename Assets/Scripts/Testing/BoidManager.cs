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
    [Header("General Settings:")]
    [SerializeField, Tooltip("Height at which new rats will be spawned")]                           private float spawnHeight;
    [SerializeField, Tooltip("Random distance range by which newly-spawned rats will be offset")]   private float spawnPositionRandomness;
    [SerializeField, Min(0), Tooltip("Maximum speed at which rats can travel")]                     private float maxSpeed;
    [SerializeField, Min(0), Tooltip("Separation distance under which rats can affect each other")] private float neighborRadius;
    [SerializeField, Min(0), Tooltip("Target separation distance between rats")]                    private float separation;
    [Header("Target Settings:")]
    [SerializeField, Min(0), Tooltip("Number of units by which trail length is reduced every second")]               private float trailFadeSpeed;
    [SerializeField, Min(0), Tooltip("Maximum length (in units) of target trail")]                                   private float maxTrailLength;
    [SerializeField, Min(0), Tooltip("Range at which adjacency to target will begin to slow rats down")]             private float targetDragRange;
    [SerializeField, Tooltip("Curve describing drag depending on how close rats are to target")]                     private AnimationCurve targetDragCurve;
    [SerializeField, Tooltip("Curve describing trail-induced velocity falloff over trail length")]                   private AnimationCurve trailFalloffCurve;
    [SerializeField, Min(0), Tooltip("Distance from target at which rats will begin to have a higher max speed")]    float targetSpeedIncreaseRadius;
    [SerializeField, Tooltip("Curve describing additive multiplier for max speed depending on adjacency to target")] private AnimationCurve targetAddSpeedCurve;
    [Header("Rule Weights:")]
    [SerializeField, Tooltip("Influence clumping rule has on rat behavior")]           private float clumpWeight = 1;
    [SerializeField, Tooltip("Influence separation rule has on rat behavior")]         private float separationWeight = 1;
    [SerializeField, Tooltip("Influence conformance rule has on rat behavior")]        private float conformWeight = 1;
    [SerializeField, Tooltip("Influence target rule has on rat behavior")]             private float targetWeight = 1;
    [SerializeField, Tooltip("Influence target drag rule has on rat behavior")]        private float targetDragWeight = 1;

    //Runtime vars:
    private List<Transform> rats = new List<Transform>();    //List of all spawned ratboids in scene
    private List<Vector2> targetTrail = new List<Vector2>(); //List of previous positions of target stored in memory
    private Vector2 currentTargetPos;                        //Target position of targetRat (updated by input)
    private float targetRatHeight;                           //Set height of targetRat

    //RUNTIME METHODS:
    private void Start()
    {
        //Get initial variables:
        targetRatHeight = spawnHeight * targetRat.localScale.x; //Determine height of targetRat based on set spawn height and target scale
        targetTrail.Add(Vector2.zero);                          //Start target trail list with an initial position at world zero
    }
    private void FixedUpdate()
    {
        //Update target position:
        if (targetRat != null) UpdateTarget(Time.fixedDeltaTime); //Update position of target rat

        //Move ratboids:
        if (rats.Count > 0) SimulateRatMovement(Time.fixedDeltaTime); //Move ratboids if there is at least one ratboid in scene
    }
    private void UpdateTarget(float deltaTime)
    {
        //Check for change in position:
        Vector2 targetPos = new Vector2(targetRat.position.x, targetRat.position.z); //Get actual position of target as 2D vector
        float posDiff = Vector2.Distance(currentTargetPos, targetPos);               //Get linear distance target has traveled since last check
        if (posDiff > 0) //Target has moved since last check
        {
            targetTrail.Insert(0, currentTargetPos);                                                     //Add new target position to beginning trail list (ensuring that no two entries occupy the same point)
            Vector3 newTargetPos = new Vector3(currentTargetPos.x, targetRatHeight, currentTargetPos.y); //Get 3D vector for new targetRat position
            targetRat.position = newTargetPos;                                                           //Move targetRat object to designated position
        }

        //Decrease trail length:
        if (targetTrail.Count > 1) //TargetTrail length is still greater than zero
        {
            //Get total length:
            float totalLength = 0; //Initialize container for storing total trail length
            for (int i = 1; i < targetTrail.Count; i++) //Iterate through each item in target trail (excluding first entry)
            {
                totalLength += Vector2.Distance(targetTrail[i - 1], targetTrail[i]); //Add distance between current and previous points to total trail length value
            }

            //Determine reduction amount:
            float trimLength = trailFadeSpeed * deltaTime;                           //Get base (constant) length reduction amount
            trimLength += Mathf.Max(0, (totalLength - trimLength) - maxTrailLength); //If (after pruning) the trail will exceed max length, add all excess length to trim amount

            //Trim trail:
            for (int i = targetTrail.Count - 1; i > 0; i--) //Iterate backwards through trail, excluding first element
            {
                float segmentLength = Vector2.Distance(targetTrail[i], targetTrail[i - 1]); //Get length of current segment
                if (segmentLength > trimLength) //Segment is longer than remaining distance which needs to be pruned
                {
                    targetTrail[i] = Vector2.MoveTowards(targetTrail[i], targetTrail[i - 1], trimLength); //Reduce length of trail end by remaining prune length
                    break;                                                                                //Break from loop
                }
                else //Segment will be completely removed by pruning operation
                {
                    targetTrail.RemoveAt(targetTrail.Count - 1); //Remove segment from trail
                    trimLength -= segmentLength;                 //Subtract segment length from prune length (since entire segment has been pruned)
                    if (trimLength == 0) break;                  //Special case for loop break (in case prune length was exactly equal to segment length)
                }
            }
        }

        //Visualize trail:
        if (targetTrail.Count > 1) //TargetTrail contains multiple elements
        {
            Vector3 prevPoint = new Vector3(targetTrail[0].x, targetRatHeight, targetTrail[0].y); //Initialize container for storing adjacent points and set value to that of first point in trail
            for (int i = 1; i < targetTrail.Count; i++) //Iterate through each item in target trail (excluding first entry)
            {
                Vector3 point = new Vector3(targetTrail[i].x, targetRatHeight, targetTrail[i].y); //Get world position of current point in trail
                Debug.DrawLine(prevPoint, point, Color.green);                                    //Draw line between each point pair
                prevPoint = point;                                                                //Save calculated point for use by next item
            }
        }
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

            //Do target rule (with trail):
            Vector2 targetVel = targetTrail[0] - pos;
            if (targetTrail.Count > 1) //There is more than one item in target trail (for rats to home toward)
            {
                for (int i = 1; i < targetTrail.Count; i++) //Iterate through each point in target trail, skipping first item
                {
                    float forceModifier = trailFalloffCurve.Evaluate(1 - (i / targetTrail.Count)); //Evaluate curve to get modifier for trail force falloff
                    targetVel += (targetTrail[i] - pos) * forceModifier;                           //Add additional velocity for each item in trail
                }
            }
            float actualTargetDistance = Vector2.Distance(pos, new Vector2(targetRat.position.x, targetRat.position.z)); //Get distance between rat and actual target position
            float currentMaxSpeed = maxSpeed;
            if (actualTargetDistance < targetSpeedIncreaseRadius) //Rat is close enough to target for a speed boost
            {
                targetVel *= 1 + targetAddSpeedCurve.Evaluate(Mathf.InverseLerp(targetSpeedIncreaseRadius, 0, actualTargetDistance)); //Add speed boost depending on distance from target
            }

            //Apply rules:
            newVel += clumpingVel * clumpWeight;
            newVel += separationVel * separationWeight;
            newVel += conformVel * conformWeight;
            newVel += targetVel * targetWeight;

            //Do target drag rule (late):
            Vector2 targetDragVel = Vector2.zero;
            if (actualTargetDistance <= targetDragRange) //Rat is close enough to target to induce drag
            {
                float dragInterpolant = Mathf.InverseLerp(targetDragRange, 0, actualTargetDistance);
                targetDragVel = -newVel;                                    //Initialize drag as reverse velocity of rat
                targetDragVel *= targetDragCurve.Evaluate(dragInterpolant); //Apply distance-sensitive intensity curve to drag force
            }
            newVel += targetDragVel * targetDragWeight;

            //Apply velocity:
            if (newVel.magnitude > currentMaxSpeed) newVel = newVel.normalized * currentMaxSpeed; //Clamp velocity
            rat.GetComponent<TestBoidController>().velocity = newVel;
            newVel *= deltaTime; //Temporarily apply deltaTime to velocity
            rat.position = new Vector3(rat.position.x + newVel.x, spawnHeight, rat.position.z + newVel.y);
        }
    }

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
            currentTargetPos = new Vector2(hit.point.x, hit.point.z); //Update current target position
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
