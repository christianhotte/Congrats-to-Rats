using UnityEngine;

/// <summary>
/// Object containing preset data regarding follower rat behavior. NOTE: when evaluating curves, use designated evaluation method.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SwarmSettings", order = 1)]
public class SwarmSettings : ScriptableObject
{
    //Data:
    [Header("General:")]
    [Min(1), Tooltip("The quantity of rats these settings work best for")]                                                     public int targetRatNumber;
    [Min(0), Tooltip("Distance with rats will try to keep between themselves and other rats")]                                 public float separationRadius;
    [Min(0), Tooltip("Distance at which rats will influence each other and clump together")]                                   public float neighborRadius;
    [Min(0), Tooltip("Distance from leader which follower rats will attempt to keep outside (while leader is standing still")] public float mamaRadius;
    [Min(0), Tooltip("Radius around trail within which rats will follow the leader while moving")]                             public float leadRadius;
    [Min(0), Tooltip("Rough radius around target path which rat swarm tries to congregate in")]                                public float targetRadius;

    [Header("Trail:")]
    [Min(0), Tooltip("Number of rats per unit of trail")]                                                                                                public float trailDensity;
    [MinMaxSlider(0, 100), Tooltip("Controls general target trail density")]                                                                             public Vector2 compressionRange;
    [Range(0, 0.5f), Tooltip("Distance rats will try to keep from each end of trail")]                                                                   public float trailBuffer;
    [Min(0), Tooltip("Minimum distance between trail points. NOTE: very short segments may lead to rats having difficulty following leader off cliffs")] public float minTrailSegLength;
    [Min(1), Tooltip("Determines how much trail stretches when big rat is moving")]                                                                      public float velTrailLengthMultiplier = 1;
    [Min(0), Tooltip("Determines how swarm cohesion value is multiplied when leader is standing still")]                                                 public float stillConformMultiplier = 1;
    [Min(0), Tooltip("Minimum allowed angle between segments (prevents kinks/sharp turns)")]                                                             public float maxSegAngle;
    [Range(0, 1), Tooltip("Length of buffer segment (proportional to overall size of trail) when backing up")]                                           public float backtrackBuffer;
    [Min(0), Tooltip("Decrease this to prevent rats from skipping backward along trail")]                                                                public float maxTrailSkip;
    [Min(0), Tooltip("Trail length below which rats will swarm in a blob instead of a trail")]                                                           public float minTrailLength;
    [Min(0), Tooltip("Maximum speed at which follower rats can overtake leader")]                                                                        public float maxOvertakeSpeed;
    [Tooltip("Additional force given to followers when jumping after leader")]                                                                           public float followerJumpBoost;

    //[Header("Curves:")]
    //[Tooltip("Use this curve to assign different multipliers for target compression depending on where rats are in trail (allows trail to be tapered)")] public AnimationCurve compressionCurve;
    //[Tooltip("Use this curve to change rat dispersal and leading characteristics depending on how close they are to the center of the trial")]           public AnimationCurve convectionCurve;

    [Header("Rules:")]
    [Min(0), Tooltip("Tendency for rats to move toward other nearby rats")]      public float cohesionWeight;   //NOTE: Keeps unmanaged swarms glued together
    [Min(0), Tooltip("Tendency for rats to avoid touching other rats")]          public float separationWeight; //NOTE: Enlarges rat swarm. High values may cause regular crystalization
    [Min(0), Tooltip("Tendency for rats to match speed with other nearby rats")] public float conformityWeight; //NOTE: Improves smoothness of rat swarm while moving. May cause instability while stationary
    [Space()]
    [Min(0), Tooltip("Tendency for rats to move toward desired position")]                  public float targetWeight;           //NOTE: Helps rats join and stay within follower swarm
    [Min(0), Tooltip("Tendency for rats to stick to leader and seek target trail density")] public float dispersionWeight;       //NOTE: Helps rats in back of trail pack catch up to swarm
    [Min(0), Tooltip("Tendency for rats to match leader velocity while on path")]           public float leadWeight;             //NOTE: Allows rats in swarm to keep pace with leader while moving
    [Min(0), Tooltip("Tendency for rats to avoid touching mama rat")]                       public float leaderSeparationWeight; //NOTE: Prevents rats from phasing through mama rat sprite, complements stayBackWeight behavior
    [Min(0), Tooltip("Tendency for rats to stay behind the leader")]                        public float stayBackWeight;         //NOTE: Gives leader more control over swarm by preventing rats from overtaking her
    [Min(0), Tooltip("Tendency for rats in back of the line to catch up")]                  public float stragglerWeight;        //NOTE: Prevents rats from being left behind by swarm. May cause pileup at end of trail

    //Interpolation Metadata:
    private float currentInterpolant = -1;              //Interpolant last used to change this object's data (negative if data is not interpolated)
    private SwarmSettings lerpSettingsA, lerpSettingsB; //Stored references to latest settings used to interpolate values

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Interpolates this asset's values between two given settings based on given number of rats. NOTE: changes mode of this settings asset, only use on instantiated settings.
    /// </summary>
    public void Lerp(SwarmSettings settingsA, SwarmSettings settingsB, int ratNumber)
    {
        //Initialize:
        if (settingsA == null || settingsB == null) return;                                                                     //Ignore if either of given settings assets is null
        currentInterpolant = Mathf.Clamp01(Mathf.InverseLerp(settingsA.targetRatNumber, settingsB.targetRatNumber, ratNumber)); //Get interpolant value based on target settings and given number of rats
        lerpSettingsA = settingsA;                                                                                              //Save interpolant settings
        lerpSettingsB = settingsB;                                                                                              //Save interpolant settings

        //Interpolate float settings (NOTE: remember to update this whenever adding a variable to this object):
        separationRadius = Mathf.Lerp(settingsA.separationRadius, settingsB.separationRadius, currentInterpolant);
        neighborRadius = Mathf.Lerp(settingsA.neighborRadius, settingsB.neighborRadius, currentInterpolant);
        mamaRadius = Mathf.Lerp(settingsA.mamaRadius, settingsB.mamaRadius, currentInterpolant);
        leadRadius = Mathf.Lerp(settingsA.leadRadius, settingsB.leadRadius, currentInterpolant);
        targetRadius = Mathf.Lerp(settingsA.targetRadius, settingsB.targetRadius, currentInterpolant);

        trailDensity = Mathf.Lerp(settingsA.trailDensity, settingsB.trailDensity, currentInterpolant);
        compressionRange = Vector2.Lerp(settingsA.compressionRange, settingsB.compressionRange, currentInterpolant);
        trailBuffer = Mathf.Lerp(settingsA.trailBuffer, settingsB.trailBuffer, currentInterpolant);
        minTrailSegLength = Mathf.Lerp(settingsA.minTrailSegLength, settingsB.minTrailSegLength, currentInterpolant);
        velTrailLengthMultiplier = Mathf.Lerp(settingsA.velTrailLengthMultiplier, settingsB.velTrailLengthMultiplier, currentInterpolant);
        stillConformMultiplier = Mathf.Lerp(settingsA.stillConformMultiplier, settingsB.stillConformMultiplier, currentInterpolant);
        maxSegAngle = Mathf.Lerp(settingsA.maxSegAngle, settingsB.maxSegAngle, currentInterpolant);
        backtrackBuffer = Mathf.Lerp(settingsA.backtrackBuffer, settingsB.backtrackBuffer, currentInterpolant);
        maxTrailSkip = Mathf.Lerp(settingsA.maxTrailSkip, settingsB.maxTrailSkip, currentInterpolant);
        minTrailLength = Mathf.Lerp(settingsA.minTrailLength, settingsB.minTrailLength, currentInterpolant);
        maxOvertakeSpeed = Mathf.Lerp(settingsA.maxOvertakeSpeed, settingsB.maxOvertakeSpeed, currentInterpolant);
        followerJumpBoost = Mathf.Lerp(settingsA.followerJumpBoost, settingsB.followerJumpBoost, currentInterpolant);

        cohesionWeight = Mathf.Lerp(settingsA.cohesionWeight, settingsB.cohesionWeight, currentInterpolant);
        separationWeight = Mathf.Lerp(settingsA.separationWeight, settingsB.separationWeight, currentInterpolant);
        conformityWeight = Mathf.Lerp(settingsA.conformityWeight, settingsB.conformityWeight, currentInterpolant);
        targetWeight = Mathf.Lerp(settingsA.targetWeight, settingsB.targetWeight, currentInterpolant);
        dispersionWeight = Mathf.Lerp(settingsA.dispersionWeight, settingsB.dispersionWeight, currentInterpolant);
        leadWeight = Mathf.Lerp(settingsA.leadWeight, settingsB.leadWeight, currentInterpolant);
        leaderSeparationWeight = Mathf.Lerp(settingsA.leaderSeparationWeight, settingsB.leaderSeparationWeight, currentInterpolant);
        stayBackWeight = Mathf.Lerp(settingsA.stayBackWeight, settingsB.stayBackWeight, currentInterpolant);
        stragglerWeight = Mathf.Lerp(settingsA.stragglerWeight, settingsB.stragglerWeight, currentInterpolant);
    }
    /*
    public float EvaluateCompressionCurve(float time)
    {
        if (currentInterpolant == -1 || lerpSettingsA == null || lerpSettingsB == null) { return compressionCurve.Evaluate(time); } //Evaluate standard curve if settings are not interpolated
        else { return LerpCurves(lerpSettingsA.compressionCurve, lerpSettingsB.compressionCurve, time); }                           //Get interpolation between curve evaluations for interpolated settings
    }
    public float EvaluateConvectionCurve(float time)
    {
        if (currentInterpolant == -1 || lerpSettingsA == null || lerpSettingsB == null) { return convectionCurve.Evaluate(time); } //Evaluate standard curve if settings are not interpolated
        else { return LerpCurves(lerpSettingsA.convectionCurve, lerpSettingsB.convectionCurve, time); }                            //Get interpolation between curve evaluations for interpolated settings
    }*/

    //UTILITY METHODS:
    /// <summary>
    /// Evaluates both curves in parallel and then interpolates evaluations based on interpolation of settings object.
    /// </summary>
    /// <param name="time">Time used to evaluate each curve.</param>
    private float LerpCurves(AnimationCurve curveA, AnimationCurve curveB, float time)
    {
        float valueA = curveA.Evaluate(time);                  //Evaluate first curve
        float valueB = curveB.Evaluate(time);                  //Evaluate second curve
        return Mathf.Lerp(valueA, valueB, currentInterpolant); //Find final value by interpolating between given curves based on current system interpolation
    }
}
