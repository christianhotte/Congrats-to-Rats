using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToasterController : MonoBehaviour
{
    //Objects & Components:
    private List<RatBoid> containedRats = new List<RatBoid>(); //List of ratboids contained in toaster

    //Settings:

    //Runtime Variables:
    private bool bigRatContained = false; //True when big rat is contained in this toaster

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Loads given rat into toaster.
    /// </summary>
    public void LoadRat(RatBoid rat)
    {
        //Initialization:
        if (containedRats.Contains(rat)) return; //Skip this rat if it's already contained in the toaster

        //Cleanup:
        containedRats.Add(rat);                      //Add rat to contained rats list
        rat.stasis = true;                           //Put rat in stasis
        rat.transform.position = transform.position; //Move rat inside toaster
    }
    /// <summary>
    /// Load mama rat into toaster.
    /// </summary>
    public void LoadRat()
    {
        //Initialization:
        if (bigRatContained) return; //Skip this if big rat is already contained

        //Cleanup:
        bigRatContained = true; //Indicate that mama rat is now contained in toaster
    }
    /// <summary>
    /// Launch all contained rats out of toaster.
    /// </summary>
    public void LaunchRats()
    {

    }
}
