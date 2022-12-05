using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designates a zone which affects the temperature of rats.
/// </summary>
public class TempZone : EffectZone
{
    //Settings:
    [Header("TempZone Settings:")]
    [SerializeField, Tooltip("Change in temperature (in degrees per second) this zone induces on rats inside it")] private float deltaTemp;

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Applies temperature effect to given rat.
    /// </summary>
    public override void AffectRat(RatBoid rat, float deltaTime)
    {
        if (!checkObstruction || !RatObstructed(rat)) //Apply temperature if rat is unobstructed or if obstructions don't matter
        {
            //Set temperature:
            rat.temperature += (deltaTemp + (rat.settings.tempMaintain * Mathf.Sign(deltaTemp))) * deltaTime; //Apply temperature change to rat based on time value (relative to rat's tempMaintain value)
            rat.temperature = Mathf.Clamp(rat.temperature, rat.settings.coldTempRange.x, rat.settings.hotTempRange.y); //Clamp rat temperature to lower and upper bounds of survivability

            //Death states:
            if (rat.temperature <= rat.settings.coldTempRange.x) //Rat is dying of cold
            {
                Destroy(rat.gameObject);
                return;
            }
            if (rat.temperature >= rat.settings.hotTempRange.y) //Rat is dying of heat
            {
                Destroy(rat.gameObject);
                return;
            }
            
        }
    }
}
