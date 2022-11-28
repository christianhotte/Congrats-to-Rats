using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for objects which have some effect on rat behavior.
/// </summary>
public class EffectZone : MonoBehaviour
{
    //Objects & Components:
    private List<RatBoid> zoneRats = new List<RatBoid>(); //List of rats which are currently within this zone
    private BoxCollider coll;                             //Box collider used to define the area of this zone

    //Settings:
    [Header("General Settings:")]
    [Tooltip("If true, rats within this zone will target the leader, rather than the path")] public bool disablesPath;
    [Header("Buffer Settings:")]
    [SerializeField, Min(0), Tooltip("Distance rats can be from edge of zone within which zone effects will be less intense")] private float bufferDepth;
    [SerializeField, Tooltip("Curve determining falloff in zone intensity for rats within buffer")]                            private AnimationCurve bufferCurve;

    //Runtime Variables:

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        coll = GetComponent<BoxCollider>(); //Get box collider from local object
    }
    private void Update()
    {
        //Affect rats:
        foreach (RatBoid rat in zoneRats) AffectRat(rat, Time.deltaTime); //Apply effect to every rat in zone
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
    /// Returns multiplier (between 0 and 1) based on rat position within collider and buffer depth. NOTE: Doesn't work :(
    /// </summary>
    /// <param name="rat">Relevant rat.</param>
    /// <returns>Multiplier value between 0 and 1, will be 0 if given coordinates are outside collider.</returns>
    private float GetBufferMultiplier(RatBoid rat)
    {
        //Initialization:
        if (!zoneRats.Contains(rat)) return 0;                                                                   //Return zero if rat is not inside this effector zone
        if (bufferDepth == 0) return 1;                                                                          //Always use full effect if buffer system is not being used
        float penetration = Vector3.Distance(coll.ClosestPoint(rat.transform.position), rat.transform.position); //Get rat penetration depth
        if (penetration >= bufferDepth) return 1;                                                                //Use full effect if rat is inside buffer area

        //Buffer calculation:
        return bufferCurve.Evaluate(penetration / bufferDepth); //Return evaluation of buffer curve depending on penetration depth
    }
}
