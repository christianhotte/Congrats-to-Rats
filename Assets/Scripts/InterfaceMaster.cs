using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InterfaceMaster : MonoBehaviour
{
    //Objects & Components:
    /// <summary>
    /// Global master controller for UI.
    /// </summary>
    public static InterfaceMaster main;
    [SerializeField, Tooltip("Text used to display current quantity of rats.")] internal TextMeshProUGUI ratCounter;

    //Settings:

    //Runtime Vars:


    //RUNTIME METHODS:
    private void Awake()
    {
        if (main == null) main = this; else Destroy(this); //Singleton-ize this script
    }

    //EXTERNAL METHODS:
    /// <summary>
    /// Updates rat counter to given amount.
    /// </summary>
    /// <param name="ratNumber"></param>
    public static void SetCounter(float amount)
    {
        main.ratCounter.text = amount.ToString(); //Set text to given number
    }
}
