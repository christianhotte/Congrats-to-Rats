using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to make an object (without a rigidbody) hover/float in the air, designed for pickups & collectibles.
/// </summary>
public class SpinFloat : MonoBehaviour
{
    //Settings:


    //Runtime Vars:


    //RUNTIME METHODS:
    private void Awake()
    {
        //Validity checks:
        if (TryGetComponent(out Rigidbody rb)) { Debug.LogError("SpinFloat component cannot be placed on " + name + " because it has an attached Rigidbody!"); Destroy(this); } //Make sure no rigidbody is present
    }
    private void Update()
    {
        
    }
}
