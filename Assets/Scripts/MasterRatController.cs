using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the big rat and governs behavior of all the little rats.
/// </summary>
public class MasterRatController : MonoBehaviour
{
    //Static Stuff:
    /// <summary>
    /// Singleton instance of Big Rat in scene.
    /// </summary>
    public static MasterRatController main;

    //Objects & Components:
    [Header("Settings:")]
    [SerializeField, Tooltip("Interchangeable data object describing movement settings of main rat")] private BigRatSettings ratSettings;
    [SerializeField, Tooltip("Interchangeable data object describing rat horde behavior")]            private SwarmSettings swarmSettings;
    private SpriteRenderer sprite; //Sprite renderer component for big rat

    //Runtime Vars:
    private Vector2 velocity;  //Current speed and direction of movement
    private Vector2 moveInput; //Current input direction for movement

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (ratSettings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); }     //Make sure big rat has ratSettings
        if (swarmSettings == null) { Debug.LogError("Big Rat is missing swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
        if (main == null) main = this; else Destroy(this);                                                       //Singleton-ize this script instance
    }
    private void Start()
    {
        //Get objects & components:
        sprite = GetComponentInChildren<SpriteRenderer>(); //Get spriteRenderer component
    }
    private void Update()
    {
        MoveRat(Time.deltaTime); //Apply velocity to rat position
    }
    
    //UPDATE METHODS:
    /// <summary>
    /// Moves the big rat according to current velocity.
    /// </summary>
    private void MoveRat(float deltaTime)
    {
        //Modify velocity:
        if (moveInput != Vector2.zero) //Player is moving rat in a direction
        {
            //Add velocity:
            Vector2 addVel = moveInput * ratSettings.accel; //Get added velocity based on input this frame
            velocity += addVel * deltaTime;                 //Add velocity as acceleration over time

            //Flip sprite:
            if (moveInput.x != 0) sprite.flipX = moveInput.x > 0 ? true : false; //Flip sprite to direction of horizontal move input
        }
        else if (velocity != Vector2.zero) //No input is given but rat is still moving
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, ratSettings.decel * deltaTime); //Slow rat down based on deceleration over time
        }
        else return; //Return if velocity is zero and no input is given
        velocity = Vector2.ClampMagnitude(velocity, ratSettings.speed); //Clamp velocity to speed

        //Set new position:
        Vector3 newPos = transform.position; //Get current position of rat
        newPos.x += velocity.x * deltaTime;  //Add X velocity over time
        newPos.z += velocity.y * deltaTime;  //Add Y velocity over time
        transform.position = newPos;         //Apply new velocity
    }

    //INPUT METHODS:
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); //Store input value
    }
}
