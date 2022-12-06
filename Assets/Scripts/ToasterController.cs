using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToasterController : MonoBehaviour
{
    //Objects & Components:
    private List<RatBoid> containedRats = new List<RatBoid>(); //List of ratboids contained in toaster
    private Transform pusher;                                  //Pusher object transform

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Vertical distance above object center to which loaded rats are moved")] private float loadOffsetY;
    [Header("Animation Settings:")]
    [SerializeField, Tooltip("Y position where pusher is fully depressed")]                               private float pusherDownPos;
    [SerializeField, Tooltip("How fast pusher moves into pushed position when big rat lands in toaster")] private float pusherLerpRate;

    //Runtime Variables:
    private float pusherStartPos;         //Initial vertical position of pusher
    private Vector3 loadPos;              //Position rats are loaded to
    private bool bigRatContained = false; //True when big rat is contained in this toaster

    //RUNTIME METHODS:
    private void Awake()
    {
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

    }
}
