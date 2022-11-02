using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aligns object to facing direction of main camera (relative to the ground).
/// </summary>
public class Billboarder : MonoBehaviour
{
    //Objects & Components:
    private SpriteRenderer r; //Render component for this billboard's sprite

    //Settings:
    [Header("Settings:")]
    [SerializeField, Min(0), Tooltip("Maximum rotation speed of billboard (in degrees per second)")] private float maxZDelta;
    //Runtime Vars:
    /// <summary>
    /// Target rotation of billboard (relative to camera)
    /// </summary>
    internal float targetZRot = 0;
    private float currentZRot = 0; //Current rotation of billboard sprite (relative to camera forward axis)

    private void Awake()
    {
        //Get objects & components:
        r = GetComponent<SpriteRenderer>(); //Get renderer
    }
    void Update()
    {
        //Initialize:
        Vector3 camAngles = Camera.main.transform.eulerAngles;             //Get camera orientation
        Quaternion newRot = Quaternion.Euler(camAngles.x, camAngles.y, 0); //Get quaternion orientation from camera (keeping Z value locked)

        //Rotate Z axis:
        if (currentZRot != targetZRot) currentZRot = Mathf.MoveTowardsAngle(currentZRot, targetZRot * (MasterRatController.main.settings.flipAll ? -1 : 1), maxZDelta * Time.deltaTime); //Smoothly approach target Z rotation
        newRot = Quaternion.AngleAxis(currentZRot, Camera.main.transform.forward) * newRot;                                                                                              //Rotate billboard relative to forward direction of camera

        //Cleanup:
        transform.rotation = newRot;                                                                                     //Apply new orientation
        if (r.material.HasProperty("_LightAngle")) r.material.SetFloat("_LightAngle", currentZRot * (r.flipX ? -1 : 1)); //Set light angle to directly upwards if relevant (flip if sprite is flipped)
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Sets Z rotation of billboard, ignoring smooth approach.
    /// </summary>
    public void SetZRot(float rot)
    {
        targetZRot = rot;  //Set target rotation to given value
        currentZRot = rot; //Snap current rotation to given value
    }
}
