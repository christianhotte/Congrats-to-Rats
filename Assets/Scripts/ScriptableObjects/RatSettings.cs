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
    [Tooltip("Maximum speed at which rat can normally travel on ground")] public float maxSpeed;
    [Header("Obstacles & Floor:")]
    [Tooltip("Height above ground at which rat will rest")]                                       public float baseHeight;
    [Tooltip("Height at and above which rat will fall off ledges instead of climbing down them")] public float fallHeight;
    [Tooltip("Maximum speed at which rat can move up or down (in units per second)")]             public float heightChangeRate;
    [Tooltip("Determines which objects will be treated as walls and floors by this rat")]         public LayerMask obstructionLayers;
    [Space()]
    [Tooltip("Desired distance this rat keeps between itself and walls/cliffs")] public float obstacleSeparation;
    [Min(0), Tooltip("Tendency for rats to avoid walls and cliffs")]             public float obstacleAvoidanceWeight;

    [Header("Visuals:")]
    [Tooltip("Maximum amount of random scale increase or decrease when spawning a new rat")]                    public float sizeVariance;
    [Tooltip("Increase this to prevent rat from flipping back and forth rapidly")]                              public float timeBetweenFlips;
    [Tooltip("Minimum crush amount to initiate pile system, crush value which corresponds to max pile amount")] public Vector2 crushRange;
    [Range(0, 1), Tooltip("How intense crush occlusion effect will be")]                                        public float occlusionIntensity;
    [Tooltip("Colors which rat crush occlusion lerps between depending on crush value")]                        public ColorScheme occlusionColors;
    [Min(0), Tooltip("Maximum additional height rat can reach from having other rats pressing against it")]     public float maxPileHeight;
    [Tooltip("Curve describing occlusion intensity depending on crush amount")]                                 public AnimationCurve occlusionCurve;
    [Tooltip("Curve describing transition between primary and secondary occlusion color depending on crush")]   public AnimationCurve occlusionColorCurve;
    [Tooltip("Curve describing cross-sectional shape of rat pile")]                                             public AnimationCurve pileCurve;
    [Space()]
    [Tooltip("Sets whether or not rats check for surface shadows (note: performance expensive)")]                         public bool doShadowMatching;
    [Tooltip("Determines how rat coloration reacts to environmental brightness")]                                         public AnimationCurve shadowSensitivityCurve;
    [Min(0), Tooltip("Maximum change in shadow value per second. Increase this to make rat shadow transitions smoother")] public float maxShadowDelta;
    [Space()]
    [Tooltip("Array of coloration schemes for rat fur (Color A is lightest variant, Color B is darkest variant)")] public ColorScheme[] furColorSchemes;
    [Tooltip("Array of coloration schemes for party hat (Color A is base, Color B is stripe)")]                    public ColorScheme[] hatColorSchemes;

    [Header("Jumping & Falling:")]
    [Tooltip("Horizontal and vertical power of jumps autonomously made by rats (for navigational purposes)")]                       public Vector2 autoJumpPower;
    [Tooltip("Random percentage up or down (along each individual axis) by which jumps may deviate")]                               public Vector2 jumpRandomness;
    [Range(0, 1), Tooltip("How close a rat has to get to a ledge before it is willing to hop off (in order to follow the leader)")] public float ledgeHopFear;
    [Space()]
    [Tooltip("Force of gravity acting on velocity of airborne rats")]                                                               public float gravity;
    [Tooltip("Force of air resistance acting on airborne rats")]                                                                    public float drag;
    [Min(0.001f), Tooltip("Determines how much force will be imparted on an object when this rat hits it")]                         public float mass;
    [Range(0, 1), Tooltip("Percentage of velocity which is retained each time rat bounces off of a wall")]                          public float bounciness;
    [Range(0, 1), Tooltip("Percentage of velocity which is retained each time rat bounces off of a wall after being thrown")]       public float thrownBounciness;
    [Range(0, 180), Tooltip("Angle between bounce exit velocity and leader direction at which rat will bounce back toward leader")] public float returnBounceAngle;
    [Min(0), Tooltip("Upward force applied to trajectory when bouncing toward leader")]                                             public float leaderBounceLift;

    [Header("Status Effects:")]
    [Min(0), Tooltip("Rate (in degrees per second) at which rat will try to maintain base temperature (counteracted by temperature zones)")]                        public float tempMaintain;
    [Min(0), Tooltip("Describe how maximum speed of rat decreases as it reaches its freezing temperature")]                                                         public AnimationCurve coldSpeedCurve;
    [Tooltip("Color rat turns when it gets cold")]                                                                                                                  public Color coldColor;
    [MinMaxSlider(-30, 150), Tooltip("Upper value is temperature at which rat will begin to freeze, lower value is temperature at which rat will die of exposure")] public Vector2 coldTempRange;
    [MinMaxSlider(-30, 150), Tooltip("Lower value is temperature at which rat will begin to cook, upper value is temperature at which rat suffer heat stroke")]     public Vector2 hotTempRange;

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
