using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Allows mama rat to change active camera by moving inside the ActivationVolume this script is on.
/// </summary>
public class CameraTrigger : MonoBehaviour
{
    //Static Stuff:
    /// <summary>
    /// Currently-active virtual camera.
    /// </summary>
    public static CameraTrigger current;
    /// <summary>
    /// Previously-active virtual camera.
    /// </summary>
    public static CameraTrigger previous;
    public static CameraTrigger menuCamera;
    /// <summary>
    /// Returns vector depending on currently-active camera which 
    /// </summary>
    public static Vector2 GetDirectionRef
    {
        get
        {
            //Validity checks:
            if (current == null) return Vector2.zero; //Return non-direction if there is no active camera

            //Get direction:
            if (brain != null && brain.IsBlending) //System is currently blending between two cameras
            {
                float blendTime = brain.ActiveBlend.TimeInBlend / brain.ActiveBlend.Duration;            //Get value representing progression through blend
                return Vector2.Lerp(previous.directionReference, current.directionReference, blendTime); //Use blendTime to interpolate between two potential directionReferences
            }
            else //System is not currently blending
            {
                return current.directionReference; //Use current direction reference value
            }
        }
    }
    /// <summary>
    /// Returns a rotation which may be applied to correct for current camera direction.
    /// </summary>
    public static float GetRotationOffset { get { return Vector2.SignedAngle(Vector2.up, GetDirectionRef); } }
    private static CinemachineBrain brain; //Single CinemachineBrain object in scene, used to govern blends

    //Objects & Components:
    private CinemachineVirtualCamera cam; //Camera object activated by this trigger
    internal Collider activator;          //Activation volume used to trigger this camera

    //Settings:
    [Header("Settings:")]
    [Tooltip("The direction in world space considered as forward when moving rats and rotating billboards while this camera is active")] public Vector2 directionReference;
    public float rotationSpeed;
    public bool firstCamera;
    public bool menuCam;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        if (brain == null) brain = Camera.main.GetComponent<CinemachineBrain>();   //Get cinemachine brain component from scene (for all triggers)
        if (!menuCam) cam = transform.parent.GetComponentInChildren<CinemachineVirtualCamera>(); //Get virtual camera component
        else
        {
            menuCamera = this;
            cam = GetComponent<CinemachineVirtualCamera>();
        }
            
        activator = GetComponent<Collider>(); //Get activation volume from this object

        //Setup first camera:
        if (current == null && firstCamera) { current = this; activator.enabled = false; } //Make first enabled camera current
    }
    private void Update()
    {
        if (menuCam && cam.enabled)
        {
            transform.parent.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent.TryGetComponent(out MasterRatController mrc)) ActivateCamera(); //Only activate camera if entering object is mama rat
        
    }

    //OPERATION METHODS:
    public void ActivateCamera()
    {
        cam.enabled = true;                                              //Enable camera
        if (current != null && current != this) current.DisableCamera(); //Disable old camera (if applicable)
        previous = current;                                              //Move current script to previous
        current = this;                                                  //Make this the current camera trigger
    }
    /// <summary>
    /// Disables this trigger's associated vcam.
    /// </summary>
    public void DisableCamera()
    {
        if (menuCam)
        {
            current.activator.enabled = true;
            current.ActivateCamera();
            MasterRatController.main.stasis = false;
            MasterRatController.main.Launch(Vector3.down);
        }
        cam.enabled = false; //Disable camera
    } 
}
