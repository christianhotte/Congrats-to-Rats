using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToasterController : MonoBehaviour
{
    //Objects & Components:
    public static ToasterController main;                      //Single instance of toaster in scene
    private List<RatBoid> containedRats = new List<RatBoid>(); //List of ratboids contained in toaster
    private Transform pusher;                                  //Pusher object transform

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Force with which rats are fired when launched out of this toaster")]         private float launchPower;
    [SerializeField, Tooltip("Direction at which rats are launched from toaster")]                         private Vector3 launchDirection;
    [SerializeField, Tooltip("Horizontal dimensions (and y-offset) of area from which rats are launched")] private Vector3 launchArea;
    [SerializeField, Tooltip("Total time (in seconds) taken for all rats to be launched")]                 private float launchTime;
    [SerializeField, Tooltip("Vertical distance above object center to which loaded rats are moved")]      private float loadOffsetY;
    [Header("Animation Settings:")]
    [SerializeField, Tooltip("Y position where pusher is fully depressed")]                               private float pusherDownPos;
    [SerializeField, Tooltip("How fast pusher moves into pushed position when big rat lands in toaster")] private float pusherLerpRate;

    //Runtime Variables:
    private float pusherStartPos;          //Initial vertical position of pusher
    private Vector3 loadPos;               //Position rats are loaded to
    internal bool bigRatContained = false; //True when big rat is contained in this toaster
    private bool launching = false;        //True while toaster is launching rats

    //Events & Coroutines:
    /// <summary>
    /// Releases rats over a period of time (period is launchTime).
    /// </summary>
    IEnumerator EjectRatsOverTime()
    {
        //Initialize:
        float secsPerRat = launchTime / containedRats.Count;            //Get number of seconds to wait between each rat launch
        Vector3 baseLaunchPos = transform.position;                     //Initialize base launch position for all rats
        baseLaunchPos.y += launchArea.y;                                //Use Y value of launchArea as direct vertical offset for launch position
        Vector3 launchForce = launchDirection.normalized * launchPower; //Get value for force used to launch all rats

        while (containedRats.Count > 0) //Iterate until all rats are launched
        {
            //Move rat:
            RatBoid rat = containedRats[0];                            //Get rat reference from list
            Vector3 ratPos = baseLaunchPos;                            //Get predetermined base launch position
            ratPos.x += Random.Range(-launchArea.x, launchArea.x) / 2; //Randomize X position within area range
            ratPos.z += Random.Range(-launchArea.z, launchArea.z) / 2; //Randomize Z position within area range
            rat.transform.position = ratPos;                           //Move rat to randomized position

            //Launch rat:
            rat.Launch(launchForce); //Apply launch force to rat

            //Cleanup:
            rat.stasis = false;                          //Take rat out of stasis
            rat.tempUseLeaderPhys = true;                //Have rat use leader physics so landing zone is more predictable
            containedRats.Remove(rat);                   //Remove launched rat from list
            yield return new WaitForSeconds(secsPerRat); //Wait until next rat is ready to launch
        }

        //Cleanup:
        launching = false; //Indicate toaster has finished launching rats and is now good to go again
    }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (main == null) main = this; else Destroy(gameObject); //Singleton-ize this toaster instance

        //Get objects & components:
        pusher = transform.Find("ToasterPusher"); //Get pusher object

        //Setup variables:
        pusherStartPos = pusher.localPosition.y;                //Get starting position of pusher
        loadPos = transform.position; loadPos.y += loadOffsetY; //Set up load position
    }
    private void FixedUpdate()
    {
        if (bigRatContained) //Mama rat is currently loaded and the toaster is good to go
        {
            float newPusherY = Mathf.Lerp(pusher.localPosition.y, pusherDownPos, pusherLerpRate);
            Vector3 newPusherPos = pusher.localPosition; newPusherPos.y = newPusherY;
            pusher.localPosition = newPusherPos;
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Loads given rat into toaster.
    /// </summary>
    public void LoadRat(RatBoid rat)
    {
        //Initialization:
        if (launching) return;                   //Skip this if toaster is currently launching rats
        if (containedRats.Contains(rat)) return; //Skip this rat if it's already contained in the toaster

        //Cleanup:
        containedRats.Add(rat);           //Add rat to contained rats list
        rat.stasis = true;                //Put rat in stasis
        rat.transform.position = loadPos; //Move rat inside toaster
    }
    /// <summary>
    /// Load mama rat into toaster.
    /// </summary>
    public void LoadRat()
    {
        //Initialization:
        if (launching) return;       //Skip this if toaster is currently launching rats
        if (bigRatContained) return; //Skip this if big rat is already contained

        //Cleanup:
        bigRatContained = true;                                //Indicate that mama rat is now contained in toaster
        MasterRatController.main.stasis = true;                //Put mama rat in stasis
        MasterRatController.main.transform.position = loadPos; //Move rat inside toaster
    }
    /// <summary>
    /// Launch all contained rats out of toaster.
    /// </summary>
    public void LaunchRats()
    {
        //Move pusher:
        Vector3 newPusherPos = pusher.localPosition; //Initialize value for new pusher position
        newPusherPos.y = pusherStartPos;             //Get starting Y value of pusher position
        pusher.localPosition = newPusherPos;         //Move pusher back to starting position

        //Launch mama rat:
        MasterRatController.main.stasis = false;                                   //Take mama rat out of stasis
        Vector3 launchPos = transform.position;                                    //Initialize variable for launch position
        launchPos.y += launchArea.y;                                               //Apply launch Y offset to position
        MasterRatController.main.transform.position = launchPos;                   //Move mama rat into launch position
        MasterRatController.main.Launch(launchDirection.normalized * launchPower); //Launch mama rat

        //Cleanup:
        bigRatContained = false;             //Indicate toaster no longer contains big rat
        launching = true;                    //Indicate toaster is now launching rats
        StartCoroutine(EjectRatsOverTime()); //Begin ejecting baby rats
    }
}
