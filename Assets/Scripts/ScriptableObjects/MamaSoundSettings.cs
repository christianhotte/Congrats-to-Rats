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
    [Header("Sound Groups:")]
    [Tooltip("Repeating sounds played while walking")]                      public AudioClip[] footsteps;
    [Tooltip("Single noise played when rat lands on ground")]               public AudioClip[] landings;
    [Tooltip("Single exertion sound made when rat is winding up to throw")] public AudioClip[] throwWindups;
    [Tooltip("Single noise made when rat throws baby rats")]                public AudioClip[] throwReleases;
    [Tooltip("Noise made when big rat falls")]                              public AudioClip[] fallSounds;
    [Tooltip("Noise made when big rat jumps")]                              public AudioClip[] jumpSounds;
    [Tooltip("Sound made when a little rat is spawned")]                    public AudioClip[] spawnSounds;
    [Tooltip("Sound made when rat bounces off of a sponge or toaster")]     public AudioClip[] bounceSounds;
    [Header("Single Noises:")]
    [Tooltip("Sound rat makes when it dies (not suddenly)")] public AudioClip deathSound;
    [Tooltip("Sound made when rat is commanding")]           public AudioClip deploySound;
    [Tooltip("Sound made when rat picks up an item")]        public AudioClip pickupSound;

    //OPERATION METHODS:
    /// <summary>
    /// Returns a single random clip from given array
    /// </summary>
    public AudioClip RandomClip(AudioClip[] noises) { return noises[Random.Range(0, noises.Length)]; }
}
