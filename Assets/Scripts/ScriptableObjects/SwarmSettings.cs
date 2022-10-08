using UnityEngine;

/// <summary>
/// Object containing preset data regarding follower rat behavior. NOTE: when evaluating curves, use designated evaluation method.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SwarmSettings", order = 1)]
public class SwarmSettings : ScriptableObject
{
    //Data:
    [Header("General:")]
    [Min(1), Tooltip("The quantity of rats these settings work best for")]                                                            public int targetRatNumber;
    [Min(0), Tooltip("Distance with rats will try to keep between themselves and other rats")]                                        public float separationRadius;
    [Min(0), Tooltip("Distance at which rats will influence each other and clump together (should be larger than separationRadius)")] public float neighborRadius;
    [Min(0), Tooltip("Rough radius around target path which rat swarm tries to congregate in")]                                       public float targetRadius;
    [Header("Trail:")]
    [Min(0), Tooltip("Number of rats per unit of trail")]                                                      public float trailDensity;
    [Min(0), Tooltip("Target density of trail")]                                                               public float targetTrailCompression;
    [Range(0, 0.5f), Tooltip("Distance rats will try to keep from each end of trail")]                         public float trailBuffer;
    [Min(0), Tooltip("Minimum distance between two trail points (higher values will make trail simpler)")]     public float minTrailSegLength;
    [Min(1), Tooltip("Determines how much trail stretches when big rat is moving")]                            public float velTrailLengthMultiplier = 1;
    [Min(0), Tooltip("Minimum allowed angle between segments (prevents kinks/sharp turns)")]                   public float maxSegAngle;
    [Range(0, 1), Tooltip("Length of buffer segment (proportional to overall size of trail) when backing up")] public float backtrackBuffer;
    [Min(0), Tooltip("Decrease this to prevent rats from skipping backward along trail")]                      public float maxTrailSkip;
    [Min(0), Tooltip("Trail length below which rats will swarm in a blob instead of a trail")]                 public float minTrailLength;
    [Header("Rules:")]
    [Min(0), Tooltip("Tendency for rats to move toward other nearby rats")]              public float cohesionWeight;
    [Min(0), Tooltip("Tendency for rats to maintain a small distance from nearby rats")] public float separationWeight; //NOTE: Enlarges rat swarm
    [Min(0), Tooltip("Tendency for rats to match speed with other nearby rats")]         public float conformityWeight; //NOTE: Improves smoothness of rat swarm at the expense of instability while stationary
    [Min(0), Tooltip("Tendency for rats to move toward desired position")]               public float targetWeight;
    [Min(0), Tooltip("Tendency for rats to move along path toward leader")]              public float followWeight;
    [Min(0), Tooltip("Tendency for rats to match leader velocity while on path")]        public float leadWeight;
    [Min(0), Tooltip("Tendency for rats to stay behind the leader")]                     public float stayBackWeight;
    [Min(0), Tooltip("Tendency for rats in back of the line to catch up")]               public float stragglerWeight;

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
        targetRadius = Mathf.Lerp(settingsA.targetRadius, settingsB.targetRadius, currentInterpolant);

        trailDensity = Mathf.Lerp(settingsA.trailDensity, settingsB.trailDensity, currentInterpolant);
        targetTrailCompression = Mathf.Lerp(settingsA.targetTrailCompression, settingsB.targetTrailCompression, currentInterpolant);
        trailBuffer = Mathf.Lerp(settingsA.trailBuffer, settingsB.trailBuffer, currentInterpolant);
        minTrailSegLength = Mathf.Lerp(settingsA.minTrailSegLength, settingsB.minTrailSegLength, currentInterpolant);
        velTrailLengthMultiplier = Mathf.Lerp(settingsA.velTrailLengthMultiplier, settingsB.velTrailLengthMultiplier, currentInterpolant);
        maxSegAngle = Mathf.Lerp(settingsA.maxSegAngle, settingsB.maxSegAngle, currentInterpolant);
        backtrackBuffer = Mathf.Lerp(settingsA.backtrackBuffer, settingsB.backtrackBuffer, currentInterpolant);
        maxTrailSkip = Mathf.Lerp(settingsA.maxTrailSkip, settingsB.maxTrailSkip, currentInterpolant);
        minTrailLength = Mathf.Lerp(settingsA.minTrailLength, settingsB.minTrailLength, currentInterpolant);

        cohesionWeight = Mathf.Lerp(settingsA.cohesionWeight, settingsB.cohesionWeight, currentInterpolant);
        separationWeight = Mathf.Lerp(settingsA.separationWeight, settingsB.separationWeight, currentInterpolant);
        conformityWeight = Mathf.Lerp(settingsA.conformityWeight, settingsB.conformityWeight, currentInterpolant);
        targetWeight = Mathf.Lerp(settingsA.targetWeight, settingsB.targetWeight, currentInterpolant);
        followWeight = Mathf.Lerp(settingsA.followWeight, settingsB.followWeight, currentInterpolant);
        leadWeight = Mathf.Lerp(settingsA.leadWeight, settingsB.leadWeight, currentInterpolant);
        stayBackWeight = Mathf.Lerp(settingsA.stayBackWeight, settingsB.stayBackWeight, currentInterpolant);
        stragglerWeight = Mathf.Lerp(settingsA.stragglerWeight, settingsB.stragglerWeight, currentInterpolant);
    }
    /*
    public float EvaluateFollowStrength(float time)
    {
        if (currentInterpolant == -1 || lerpSettingsA == null || lerpSettingsB == null) { return followStrengthCurve.Evaluate(time); } //Evaluate standard curve if settings are not interpolated
        else { return LerpCurves(lerpSettingsA.followStrengthCurve, lerpSettingsB.followStrengthCurve, time); }                        //Get interpolation between curve evaluations for interpolated settings
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
