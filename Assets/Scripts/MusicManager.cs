using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    //Objects & Components:
    private AudioSource mainSource;      //Main audiosource component used to play sound with this system
    private AudioSource fadeSource;      //Secondary audiosource component used to fade in tracks
    private AudioSource narrationSource; //Tertiary audiosource used to play verbal narration
    [SerializeField, Tooltip("Main music loops which play throughout the game")]    private AudioClip[] mainLoops;
    [SerializeField, Tooltip("Volume outside which intro music will stop playing")] private BoxCollider introMusicVolume;

    //Settings:
    [Header("Settings:")]
    [SerializeField, Range(0, 1), Tooltip("Default and master volume setting for music player, also functions as max volume")] private float baseVolume = 1;
    [SerializeField, Min(0), Tooltip("Time taken for system to fade between clips")]                                           private float fadeTime;
    [Header("Narration Settings:")]
    [SerializeField, Range(0, 1), Tooltip("Level music volume drops to when narration is playing")] private float narrationMusicVolume;

    //Runtime Variables:
    private float currentVolume = 1;   //Current system volume setting
    private int currentPhase = 0;      //Current phase music is in
    private bool fadingClip = false;   //Indicates whether or not music is fading into new clip
    private bool fadingVolume = false; //Indicates whether or not volume is being faded

    //Events & Coroutines:
    /// <summary>
    /// Fades into given clip in given number of seconds.
    /// </summary>
    IEnumerator FadeToClip(AudioClip clip, float fadeTime)
    {
        //Validity checks:
        if (fadingClip) { Debug.LogError("Music Manager tried to fade twice at the same time!"); yield return null; } //Cancel fade operation if fade is already occurring

        //Initialize:
        fadeSource.clip = clip; //Set new clip
        fadeSource.Play();      //Play new clip on fade source
        fadingClip = true;      //Indicate that system is currently fading into new clip

        //Crossfade:
        for (float totalTime = 0; totalTime < fadeTime; totalTime += Time.fixedDeltaTime) //Iterate every fixed update for given number of seconds
        {
            float volValue = (totalTime / fadeTime) * currentVolume; //Get volume value for fade source based on progression of fade and current volume setting
            fadeSource.volume = volValue;                            //Set rising volume for fade source
            mainSource.volume = 1 - volValue;                        //Set falling volume for main source
            yield return new WaitForFixedUpdate();                   //Wait for next fixed update
        }

        //Switch sources:
        fadeSource.volume = currentVolume;     //Set fade source to maximum volume
        mainSource.volume = 0;                 //Fully mute main source
        AudioSource switchSource = fadeSource; //Temporarily store reference to fade source
        fadeSource = mainSource;               //Move main source to secondary position
        mainSource = switchSource;             //Move fade source to primary position
        fadeSource.Stop();                     //Halt muted main (now fade) source

        //Cleanup:
        fadingClip = false; //Indicate that clips are no longer fading
        yield return null;  //End coroutine
    }
    /// <summary>
    /// Fades system volume to new volume within given time frame.
    /// </summary>
    /// <param name="newVolume">Target volume to fade into (note: will be automatically made relative to Base Volume).</param>
    /// <param name="fadeTime">Time taken to fade to target volume.</param>
    /// <returns></returns>
    IEnumerator FadeToVolume(float newVolume, float fadeTime)
    {
        //Validity checks:
        if (fadingVolume) { Debug.LogError("Music Manager tried to fade volume twice at the same time"); yield return null; } //Cancel fade operation if volume fade is already occurring

        //Initialize:
        float prevVolume = currentVolume;            //Store volume before fade
        float targetVolume = newVolume * baseVolume; //Get target volume based off of master volume
        fadingVolume = true;                         //Indicate that volume is now being faded

        //Fade:
        for (float totalTime = 0; totalTime < fadeTime; totalTime += Time.fixedDeltaTime) //Iterate every fixed update for given number of seconds
        {
            float timeValue = totalTime / fadeTime;                        //Get value representing chronological progression through curve
            ChangeVolume(Mathf.Lerp(prevVolume, targetVolume, timeValue)); //Move volume toward target
            yield return new WaitForFixedUpdate();                         //Wait for next fixed update
        }

        //Cleanup:
        ChangeVolume(targetVolume); //Snap volume to target
        fadingVolume = false;       //Indicate that volume is no longer being faded
    }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialize:
        currentVolume = baseVolume; //Set current volume based on initial setting

        //Generate audio sources:
        mainSource = gameObject.AddComponent<AudioSource>(); //Create new audiosource component on this object and get reference to it
        mainSource.loop = true;                              //Set source to loop
        mainSource.volume = currentVolume;                   //Set source volume to default max
        mainSource.name = "MainSource";

        fadeSource = gameObject.AddComponent<AudioSource>(); //Create new audiosource component on this object and get reference to it
        fadeSource.loop = true;                              //Set source to loop
        fadeSource.volume = 0;                               //Set fade source volume to zero by default

        narrationSource = gameObject.AddComponent<AudioSource>(); //Create new audiosource component on this object and get reference to it
        narrationSource.volume = currentVolume;                   //Set source volume to default max
    }
    private void Start()
    {
        //Initialize:
        mainSource.clip = mainLoops[0]; //Set up intro clip in main audio source
        mainSource.Play();              //Begin playing clip
    }
    private void Update()
    {
        if (currentPhase == 0 && !introMusicVolume.bounds.Contains(transform.parent.position)) //Intro phase is active and rat has left intro area
        {
            if (!fadingClip) //Make sure system is not currently fading for some reason
            {
                currentPhase++;                                     //Increment phase tracker
                StartCoroutine(FadeToClip(mainLoops[1], fadeTime)); //Fade to first main loop
            }
        }
    }

    //OPERATION METHODS:
    /// <summary>
    /// Sets new music volume.
    /// </summary>
    /// <param name="newVolume">New volume setting (between 0 and 1).</param>
    public void ChangeVolume(float newVolume)
    {
        newVolume = Mathf.Clamp01(newVolume * baseVolume); //Make sure volume is between 0 and 1 (and make relative to base volume)
        if (!fadingClip) mainSource.volume = newVolume;    //Set new volume for main source, but only if it is not currently being controlled by an active fade coroutine
        currentVolume = newVolume;                         //Record current volume
    }
    /// <summary>
    /// Plays given clip as a narration (fades out music while playing).
    /// </summary>
    public void PlayNarration(AudioClip narrationClip)
    {

    }
}
