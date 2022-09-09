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
    [SerializeField, Tooltip("Interchangeable data object describing movement settings of main rat")] private BigRatSettings settings;
    [SerializeField, Tooltip("Interchangeable data object describing rat horde behavior")]            private SwarmSettings swarmSettings;
    private SpriteRenderer sprite; //Sprite renderer component for big rat

    //Runtime Vars:
    private Vector2 velocity;  //Current speed and direction of movement
    private Vector2 moveInput; //Current input direction for movement

    private List<Vector2> trailPoints = new List<Vector2>(); //List of points in follower trail behind the big rat (lower indeces are newer)

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (settings == null) { Debug.LogError("Big Rat is missing ratSettings object"); Destroy(this); }        //Make sure big rat has ratSettings
        if (swarmSettings == null) { Debug.LogError("Big Rat is missing swarmSettings object"); Destroy(this); } //Make sure big rat has swarmSettings
        if (main == null) main = this; else Destroy(this);                                                       //Singleton-ize this script instance
        trailPoints.Add(PosAsVector2());                                                                         //Add current position of rat as first trail point
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
            Vector2 addVel = moveInput * settings.accel; //Get added velocity based on input this frame
            velocity += addVel * deltaTime;              //Add velocity as acceleration over time

            //Flip sprite:
            if (moveInput.x != 0) sprite.flipX = moveInput.x > 0 ? true : false; //Flip sprite to direction of horizontal move input
        }
        else if (velocity != Vector2.zero) //No input is given but rat is still moving
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, settings.decel * deltaTime); //Slow rat down based on deceleration over time
        }
        else return; //Return if velocity is zero and no input is given
        velocity = Vector2.ClampMagnitude(velocity, settings.speed); //Clamp velocity to speed

        //Get new position:
        Vector3 newPos = transform.position; //Get current position of rat
        newPos.x += velocity.x * deltaTime;  //Add X velocity over time to position
        newPos.z += velocity.y * deltaTime;  //Add Y velocity over time to position
        transform.position = newPos;         //Apply new position

        //Update trail characteristics:
        
    }

    //INPUT METHODS:
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); //Store input value
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns current position of rat as 2D vector (relative to world down).
    /// </summary>
    private Vector2 PosAsVector2()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
}
