using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aligns object to facing direction of main camera (relative to the ground).
/// </summary>
public class Billboarder : MonoBehaviour
{
    void Update()
    {
        //Update object orientation:
        Vector3 newRot = Camera.main.transform.eulerAngles; //Get orientation from camera
        newRot.z = 0;                                       //Prevent twisting rotations
        transform.eulerAngles = newRot;                     //Apply new orientation
    }
}
