using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for objects which have some effect on rat behavior.
/// </summary>
public class EffectZone : MonoBehaviour
{
    //Objects & Components:
    private protected BoxCollider coll; //Box collider used to define the area of this zone

    //Settings:
    [Header("General Settings:")]
    [SerializeField, Tooltip("Prevents zone effect from being applied to rats which are not visible by zone object (NOTE: Will check along FORWARD direction of zone)")] private protected bool checkObstruction;
    [SerializeField, Tooltip("If true, this zone will be destroyed/denatured when big rat enters it")]                                                                   private protected bool destroyOnEntry;
    [Tooltip("If true, this zone will be destroyed/denatured when big rat enters it")]                                                                                   public bool checkForRatLeave;

    //Runtime Variables:
    internal List<RatBoid> zoneRats = new List<RatBoid>(); //List of rats which are currently within this zone
    internal bool bigRatInZone = false;                    //True if mama rat is also in this zone
    internal bool deactivated = false;                     //If true, rats should disregard this zone

    //Events & Coroutines:
    /// <summary>
    /// Event called when big rat enters this zone.
    /// </summary>
    public UnityAction OnBigRatEnter;
    /// <summary>
    /// Event called when big rat leaves this zone.
    /// </summary>
    public UnityAction OnBigRatLeave;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        coll = GetComponent<BoxCollider>(); //Get box collider from local object

        //Event subscriptions:
        OnBigRatEnter += BigRatEnteredZone; //Add event to empty method to prevent errors when not in use
        OnBigRatLeave += BigRatLeftZone;    //Add event to empty method to prevent errors when not in use
    }
    private void Update()
    {
        //Affect rats:
        foreach (RatBoid rat in zoneRats) AffectRat(rat, Time.deltaTime); //Apply effect to every rat in zone
    }
    private void OnDestroy()
    {
        //Event unsubscriptions:
        OnBigRatEnter -= BigRatEnteredZone; //Unsubscribe on destruction
        OnBigRatLeave -= BigRatLeftZone;    //Unsubscribe on destruction

        //Cleanup:
        Clear(); //Clear rats in zone
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Indicates that given rat is now within this zone.
    /// </summary>
    public void AddRat(RatBoid rat) { if (!zoneRats.Contains(rat)) zoneRats.Add(rat); }
    /// <summary>
    /// Removes given rat from the effects of this zone.
    /// </summary>
    public void RemoveRat(RatBoid rat) { if (zoneRats.Contains(rat)) zoneRats.Remove(rat); }
    /// <summary>
    /// Applies a designated environmental effect to given rat.
    /// </summary>
    public virtual void AffectRat(RatBoid rat, float deltaTime)
    {
        
    }

    //UTILITY METHODS:
    /// <summary>
    /// Indicates whether or not path between given rat and zone origin is obstructed.
    /// </summary>
    private protected bool RatObstructed(RatBoid rat)
    {
        Vector3 pointOnSource = Vector3.ProjectOnPlane(rat.transform.position, transform.forward);                //Get point of rat projected onto plane aligned with source object
        pointOnSource += Vector3.Project(transform.position, transform.forward);                                  //Move point to actual world space of source object
        if (Physics.Linecast(rat.transform.position, pointOnSource, rat.settings.obstructionLayers)) return true; //Indicate that obstruction is present
        return false;                                                                                             //Return false no obstructions could be found
    }
    /// <summary>
    /// Disables all zone functionality and clears currentZone lists of all creatures within zone.
    /// </summary>
    private protected void Clear()
    {
        //Clear rat lists:
        if (bigRatInZone) MasterRatController.main.currentZones.Remove(this); //Remove this zone from big rat's list (if applicable)
        foreach (RatBoid rat in zoneRats) rat.currentZones.Remove(this);      //Remove this zone from memory of each contained rat

        //Cleanup:
        deactivated = true;   //Indicate that zone is deactivated
        bigRatInZone = false; //Clear memory of big rat
        zoneRats.Clear();     //Clear memory of ratboids
    }
    private void BigRatEnteredZone()
    {
        if (destroyOnEntry) //Zone is set to destroy itself upon big rat entry
        {
            Clear(); //Prevent this zone from further use
        }
    }
    private void BigRatLeftZone()
    {

    }
}

//BONEYARD:
/*
/// <summary>
/// Returns multiplier (between 0 and 1) based on rat position within collider and buffer depth. NOTE: Doesn't work :(
/// </summary>
/// <param name="rat">Relevant rat.</param>
/// <returns>Multiplier value between 0 and 1, will be 0 if given coordinates are outside collider.</returns>
private protected float GetBufferMultiplier(RatBoid rat)
{
    //Initialization:
    if (!zoneRats.Contains(rat)) return 0;                                                                   //Return zero if rat is not inside this effector zone
    if (bufferDepth == 0) return 1;                                                                          //Always use full effect if buffer system is not being used
    float penetration = Vector3.Distance(coll.ClosestPoint(rat.transform.position), rat.transform.position); //Get rat penetration depth
    if (penetration >= bufferDepth) return 1;                                                                //Use full effect if rat is inside buffer area

    //Buffer calculation:
    return bufferCurve.Evaluate(penetration / bufferDepth); //Return evaluation of buffer curve depending on penetration depth
}
*/