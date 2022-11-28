using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindZone : EffectZone
{
    //Settings:
    [Header("Wind Settings:")]
    [SerializeField, Min(0), Tooltip("How powerful the wind effect is")]                                   private float strength;
    [SerializeField, Tooltip("Direction of wind force (relative to local forward orientation of object)")] private Vector3 direction;
    [SerializeField, Tooltip("Prevents wind from affecting rats which are obstructed")]                    private bool checkObstruction = true;

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Applies wind effect to given rat.
    /// </summary>
    public override void AffectRat(RatBoid rat, float deltaTime)
    {
        //Initialization
        Vector3 dir = transform.rotation * direction; //Get actual direction of wind
        float str = strength * deltaTime;             //Get actual strength of wind this frame

        //Check for obstructions:
        if (checkObstruction) //System is checking for obstructions to wind effect
        {
            Vector3 pointOnSource = Vector3.ProjectOnPlane(rat.transform.position, transform.forward);           //Get point of rat projected onto plane aligned with source object
            pointOnSource += Vector3.Project(transform.position, transform.forward);                             //Move point to actual world space of source object
            Debug.DrawLine(rat.transform.position, pointOnSource);
            if (Physics.Linecast(rat.transform.position, pointOnSource, rat.settings.obstructionLayers)) return; //Ignore wind effect on rat if there is not a direct path between it and the wind source
        }

        //Apply wind effect:
        rat.velocity += RatBoid.FlattenVector(dir * str); //Modify rat's velocity based on wind direction and strength
    }
}