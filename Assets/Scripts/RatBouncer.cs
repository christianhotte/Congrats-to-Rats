using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RatBouncer : MonoBehaviour
{
    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Rat velocity multiplier applied when bouncing")] private float bounceValue = 1;
    [Header("Advanced Settings:")]
    [SerializeField, Tooltip("If set to anything other than zero, this is the direction rats will always be bounced at when they touch this object")]              private Vector3 bounceDirection = Vector3.zero;
    [SerializeField, Tooltip("When using deterministic bounce method, this angle determines which rat bounces are valid and which aren't (relative to world.up)")] private float maxBounceAngle = 180;
    [SerializeField, Tooltip("Velocity at which rat is launched when being bounced deterministically")]                                                            private float bounceVel = 1;

    //UTILITY VARIABLES:
    /// <summary>
    /// Returns true if bouncer bounces rats at a specific direction.
    /// </summary>
    public bool IsDirectional { get { return bounceDirection != Vector3.zero; } }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Returns velocity after bounce effect has been applied, effect is determined by bouncer settings.
    /// </summary>
    /// <param name="vel">Entry velocity.</param>
    /// <param name="normal">Normal of polygon hit by rat.</param>
    public Vector3 GetBounceVelocity(Vector3 vel, Vector3 normal)
    {
        vel = Vector3.Reflect(vel, normal); //Reflect rat velocity against hit surface
        if (bounceDirection == Vector3.zero) //Simple bounce method is being used
        {
            return vel * bounceValue; //Use bounce value as a simple multiplier
        }
        else //Directional bounce is being used
        {
            if (Vector3.Angle(Vector3.up, normal) > maxBounceAngle) //Rat has not hit top of object
            {
                return vel * bounceValue; //Use secondary bounce value to rebound rat
            }
            else //Rat is being bounced deterministically
            {
                return bounceDirection.normalized * bounceVel; //Bounce rat away at precise velocity and direction
            }
        }
    }
}
