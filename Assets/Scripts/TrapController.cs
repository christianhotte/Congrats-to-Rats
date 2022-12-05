using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    //Objects & Components:
    private Rigidbody rb;         //Rigidbody component for this trap assembly
    private Collider coll;        //Box collider for trap body
    private Animator anim;        //Animator controller for trap
    private Transform exploPoint; //Point used to determine where explosion force is generated from
    [Header("Zones:")]
    [SerializeField, Tooltip("Zone which, when a rat enters, will cause trap to trip")]       private EffectZone triggerZone;
    [SerializeField, Tooltip("Zone containing rats which will be destroyed when trap snaps")] private EffectZone killZone;

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Force by which trap launches rats when triggered")] private float launchForce;
    [SerializeField, Tooltip("Causes rats to be launched away from center")]      private float launchHorizForce;
    [SerializeField, Tooltip("Potential random deviation in launch force")]       private float launchForceRandomness;
    [SerializeField, Tooltip("Potential random deviation in launch angle")]       private float launchHorizRandomness;
    [Space()]
    [SerializeField, Tooltip("Force by which trap is physically moved when it snaps")] private float explosionPower;
    [SerializeField, Tooltip("Potential random deviation for explosion power")]        private float explosionPowRandomness;
    [SerializeField, Tooltip("Potential random deviation for explosion point")]        private float explosionPosRandomness;
    [Header("Debug:")]
    [SerializeField, Tooltip("Use this to manually spring trap")] private bool debugSpring;

    //Runtime Variables:
    private bool sprung = false; //Becomes true after trap is activated

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        rb = GetComponent<Rigidbody>();                   //Get rigidbody component on object
        coll = GetComponent<Collider>();                  //Get collider component on object
        anim = GetComponent<Animator>();                  //Get animator component on object
        exploPoint = transform.Find("ExplosionPosition"); //Get explosion position marker
    }
    private void Update()
    {
        //Check trip status:
        if (!sprung) if (triggerZone.zoneRats.Count > 0) Trip(); //Activate trap when a rat enters trigger zone

        //Debug:
        if (debugSpring) { debugSpring = false; Trip(); } //Check for manual trap activation
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Triggers spring trap.
    /// </summary>
    private void Trip()
    {
        anim.SetTrigger("Activate"); //Begin spring animation
        sprung = true;               //Indicate that trap has been sprung
    }
    /// <summary>
    /// Causes trap to launch into the air
    /// </summary>
    public void Launch()
    {
        //Kill rats:
        foreach (RatBoid rat in killZone.zoneRats) //Iterate through each rat in killzone
        {
            //Get launch values:
            Vector3 force = Vector3.up * (launchForce + Random.Range(-launchForceRandomness, launchForceRandomness));                                                                                 //Get base upward launch force with random deviation within set range
            force += Vector3.ProjectOnPlane((rat.transform.position - transform.position).normalized, Vector3.up) * (launchHorizForce + Random.Range(-launchHorizRandomness, launchHorizRandomness)); //Add horizontal force which pushes rats away from trap

            //Cleanup:
            rat.Launch(force);      //Launch rat using generated force value
            rat.dieOnImpact = true; //Set rats to die when they hit another object
        }

        //Launch trap:
        float power = explosionPower + Random.Range(-explosionPowRandomness, explosionPowRandomness); //Randomly modify explosion power within set range
        Vector3 pos = exploPoint.position + (Random.insideUnitSphere * explosionPosRandomness);       //Randomly modify explosion position within set range
        rb.isKinematic = false; coll.enabled = false;                                                 //Activate rigidbody & disable collider
        rb.AddExplosionForce(power, pos, 100);                                                        //Apply explosion force to trap using modified values
    }
}
