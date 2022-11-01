using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object containing preset data regarding mama rat sounds.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MamaSoundSettings", order = 1)]
public class MamaSoundSettings : ScriptableObject
{
    //DATA:
    [Header("Sound Clips:")]
    [Tooltip("Repeating sounds played while walking")]                      public AudioClip[] footsteps;
    [Tooltip("Single noise played when rat lands on ground")]               public AudioClip[] landings;
    [Tooltip("Single exertion sound made when rat is winding up to throw")] public AudioClip[] throwWindups;
    [Tooltip("Single noise made when rat throws baby rats")]                public AudioClip[] throwReleases;

    //OPERATION METHODS:
    /// <summary>
    /// Returns a single random clip from given array
    /// </summary>
    public AudioClip RandomClip(AudioClip[] noises) { return noises[Random.Range(0, noises.Length)]; }
}
