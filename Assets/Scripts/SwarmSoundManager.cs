using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmSoundManager : MonoBehaviour
{
    //Objects & Components:
    private MasterRatController bigRatController; //The script controlling the big rat

    //Settings:
    [Header("Settings:")]
    [Min(1), SerializeField, Tooltip("Determines how many sources which will be spawned depending on how many rats there are in swarm")] private float ratsPerSource;
    [Min(-1), SerializeField, Tooltip("Maximum number of audio sources which can be spawned (keep negative for uncapped number)")]       private int maxSourceCount = -1;

    //Runtime Variables:
    private List<Transform> sourceChain = new List<Transform>(); //List of all audio source nodes in system

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out bigRatController)) Debug.LogError("SwarmSoundManager could not find MasterRatController. Make sure both scripts are on the same object."); //Get reference to master rat controller component

        //Event subscriptions:
        bigRatController.followerCountChanged += UpdateSourceCount; //Subscribe to follower number change event
    }
    private void Update()
    {
        //Update source positions:
        for (int i = 0; i < sourceChain.Count; i++) //Iterate through source chain
        {
            float trailValue = (float)i / sourceChain.Count;                                                    //Get trail value for source point so that sources are evenly-distributed
            MasterRatController.TrailPointData pointData = bigRatController.GetTrailPointFromValue(trailValue); //Get point on trail at given value
            sourceChain[i].position = RatBoid.UnFlattenVector(pointData.point);                                 //Move node to point on trail
        }
    }
    private void OnDestroy()
    {
        //Event unsubscriptions:
        bigRatController.followerCountChanged -= UpdateSourceCount; //Unsubscribe from follower number change event
    }

    //FUNCTIONALITY METHODS:
    private void UpdateSourceCount()
    {
        //Get target source count:
        int targetCount = Mathf.CeilToInt(bigRatController.TotalFollowerCount / ratsPerSource); //Get raw target based on current follower count and manager settings
        targetCount = Mathf.Max(1, targetCount);                                                //Clamp count to make sure there is always at least one source
        if (maxSourceCount > 0) targetCount = Mathf.Min(targetCount, maxSourceCount);           //Keep target count at or below maximum if max number is designated

        //Spawn/despawn sources:
        while (sourceChain.Count != targetCount) //Spawn/despawn sources until target count is met
        {
            if (sourceChain.Count < targetCount) //More sources need to be spawned
            {
                //Spawn new node:
                Transform newNode = new GameObject().transform;                   //Instantiate new game object
                newNode.name = "SoundNode_" + (sourceChain.Count + 1).ToString(); //Name node appropriately
                newNode.gameObject.AddComponent<AudioSource>();                   //Add audiosource to node
                sourceChain.Add(newNode);                                         //Add new audiosource to chain
            }
            else //Sources need to be despawned
            {
                GameObject oldNode = null; if (sourceChain[^1] != null) oldNode = sourceChain[^1].gameObject; //Get last node in chain (if possible)
                sourceChain.RemoveAt(sourceChain.Count - 1);                                                  //Remove source from chain
                if (oldNode != null) Destroy(oldNode);                                                        //Destroy node
            }
        }
    }
}
