using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object containing data regarding the nature and settings of a type of rat (unlike SwarmSettings, this does not scale with volume).
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RatSettings", order = 1)]
public class RatSettings : ScriptableObject
{
    //Classes, Structs & Enums:
    [System.Serializable]
    public class ColorScheme
    {
        [Tooltip("Primary color")]   public Color colorA = Color.white;
        [Tooltip("Secondary color")] public Color colorB = Color.white;
    }

    //DATA:
    [Header("Movement:")]
    [Tooltip("Maximum speed at which rat can normally travel on ground")]              public float maxSpeed;
    [Tooltip("Maximum speed at which rat can move relative to leader (if following)")] public float maxOvertakeSpeed;
    [Header("Obstacles & Floor:")]
    [Tooltip("Height above ground at which rat will rest")]                                       public float baseHeight;
    [Tooltip("Height at and above which rat will fall off ledges instead of climbing down them")] public float fallHeight;
    [Tooltip("Maximum speed at which rat can move up or down (in units per second)")]             public float heightChangeRate;
    [Tooltip("Determines which objects will be treated as walls and floors by this rat")]         public LayerMask obstructionLayers;
    [Space()]
    [Tooltip("Desired distance this rat keeps between itself and walls/cliffs")] public float obstacleSeparation;
    [Min(0), Tooltip("Tendency for rats to avoid walls and cliffs")]             public float obstacleAvoidanceWeight;
    [Header("Visuals:")]
    [Tooltip("Maximum amount of random scale increase or decrease when spawning a new rat")] public float sizeVariance;
    [Tooltip("Increase this to prevent rat from flipping back and forth rapidly")]           public float timeBetweenFlips;
    [Space()]
    [Tooltip("Array of coloration schemes for rat fur (Color A is lightest variant, Color B is darkest variant)")] public ColorScheme[] furColorSchemes;
    [Tooltip("Array of coloration schemes for party hat (Color A is base, Color B is stripe)")]                    public ColorScheme[] hatColorSchemes;
    [Header("Airborne Behavior:")]
    [Tooltip("Force of gravity acting on velocity of airborne rats")]                                      public float gravity;
    [Tooltip("Force of air resistance acting on airborne rats")]                                           public float drag;
    [Range(0, 1), Tooltip("Percentage of velocity which is retained each time rat bounces off of a wall")] public float bounciness;

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Returns random fur color based on presets.
    /// </summary>
    public Color GetFurColor()
    {
        //Initial checks:
        if (furColorSchemes.Length == 0) return Color.black;                                                                    //Return "null" color if no schemes are set up
        if (furColorSchemes.Length == 1) return Color.Lerp(furColorSchemes[0].colorA, furColorSchemes[0].colorB, Random.value); //Do not try to randomly select if only one color scheme option is given

        //Randomly select scheme:
        ColorScheme scheme = furColorSchemes[Random.Range(0, furColorSchemes.Length)]; //Get random color scheme from settings list
        return Color.Lerp(scheme.colorA, scheme.colorB, Random.value);                 //Return randomized color within scheme
    }
    /// <summary>
    /// Returns randomly-selected pair of colors.
    /// </summary>
    public Color[] GetHatColors()
    {
        //Initial checks:
        if (hatColorSchemes.Length == 0) return new Color[] { Color.black, Color.black };                             //Return "null" colors if no schemes are set up
        if (hatColorSchemes.Length == 1) return new Color[] { hatColorSchemes[0].colorA, hatColorSchemes[0].colorB }; //Do not try to randomly select if only one color scheme option is given

        //Randomly select scheme:
        ColorScheme scheme = hatColorSchemes[Random.Range(0, hatColorSchemes.Length)]; //Get random color scheme from settings list
        return new Color[] { scheme.colorA, scheme.colorB };                           //Return colors from scheme
    }
}