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
    [Min(0), Tooltip("Maximum speed rats may travel at")]                                                                             public float maxSpeed;
    [Min(0), Tooltip("Distance with rats will try to keep between themselves and other rats")]                                        public float separationRadius;
    [Min(0), Tooltip("Distance at which rats will influence each other and clump together (should be larger than separationRadius)")] public float neighborRadius;
    [Min(0), Tooltip("Rough radius around target path which rat swarm tries to congregate in")]                                       public float targetRadius;
    [Space()]
    [SerializeField, Tooltip("Use this to tweak followWeight according to where rats are along the trail")] private AnimationCurve followStrengthCurve;
    [SerializeField, Tooltip("Use modify target radius along length of path")]                              private AnimationCurve targetSizeCurve;
    [Header("Trail:")]
    [Min(0), Tooltip("Number of rats per unit of trail")]                                                  public float trailDensity;
    [Min(0), Tooltip("Minimum distance between two trail points (higher values will make trail simpler)")] public float minTrailSegLength;
    [Min(1), Tooltip("Determines how much trail stretches when big rat is moving")]                        public float velTrailLengthMultiplier = 1;
    [Header("Rules:")]
    [Min(0), Tooltip("Tendency for rats to move toward other nearby rats")]              public float cohesionWeight;
    [Min(0), Tooltip("Tendency for rats to maintain a small distance from nearby rats")] public float separationWeight;
    [Min(0), Tooltip("Tendency for rats to match speed with other nearby rats")]         public float conformityWeight;
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

        //Interpolate float settings (NOTE: kinda gross):
        maxSpeed = Mathf.Lerp(settingsA.maxSpeed, settingsB.maxSpeed, currentInterpolant);
        separationRadius = Mathf.Lerp(settingsA.separationRadius, settingsB.separationRadius, currentInterpolant);
        neighborRadius = Mathf.Lerp(settingsA.neighborRadius, settingsB.neighborRadius, currentInterpolant);
        targetRadius = Mathf.Lerp(settingsA.targetRadius, settingsB.targetRadius, currentInterpolant);

        trailDensity = Mathf.Lerp(settingsA.trailDensity, settingsB.trailDensity, currentInterpolant);
        minTrailSegLength = Mathf.Lerp(settingsA.minTrailSegLength, settingsB.minTrailSegLength, currentInterpolant);
        velTrailLengthMultiplier = Mathf.Lerp(settingsA.velTrailLengthMultiplier, settingsB.velTrailLengthMultiplier, currentInterpolant);

        cohesionWeight = Mathf.Lerp(settingsA.cohesionWeight, settingsB.cohesionWeight, currentInterpolant);
        separationWeight = Mathf.Lerp(settingsA.separationWeight, settingsB.separationWeight, currentInterpolant);
        conformityWeight = Mathf.Lerp(settingsA.conformityWeight, settingsB.conformityWeight, currentInterpolant);
        targetWeight = Mathf.Lerp(settingsA.targetWeight, settingsB.targetWeight, currentInterpolant);
        followWeight = Mathf.Lerp(settingsA.followWeight, settingsB.followWeight, currentInterpolant);
        leadWeight = Mathf.Lerp(settingsA.leadWeight, settingsB.leadWeight, currentInterpolant);
        stayBackWeight = Mathf.Lerp(settingsA.stayBackWeight, settingsB.stayBackWeight, currentInterpolant);
        stragglerWeight = Mathf.Lerp(settingsA.stragglerWeight, settingsB.stragglerWeight, currentInterpolant);
    }
    public float EvaluateFollowStrength(float time)
    {
        if (currentInterpolant == -1 || lerpSettingsA == null || lerpSettingsB == null) { return followStrengthCurve.Evaluate(time); } //Evaluate standard curve if settings are not interpolated
        else { return LerpCurves(lerpSettingsA.followStrengthCurve, lerpSettingsB.followStrengthCurve, time); }                        //Get interpolation between curve evaluations for interpolated settings
    }
    public float EvaluateTargetSize(float time)
    {
        if (currentInterpolant == -1 || lerpSettingsA == null || lerpSettingsB == null) { return targetSizeCurve.Evaluate(time); } //Evaluate standard curve if settings are not interpolated
        else { return LerpCurves(lerpSettingsA.targetSizeCurve, lerpSettingsB.targetSizeCurve, time); }                            //Get interpolation between curve evaluations for interpolated settings
    }

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
