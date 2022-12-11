using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{
    //Static Stuff:
    public static Respawner currentSpawnPoint; //Spawn point which player will go to when/if they die

    //Objects & Components:
    private EffectZone detectorZone; //Zone used to detect when mama rat switches to this spawnpoint
    private Transform spawnPosition; //Position from which spawned rats are flung
    private Animator anim;           //Animator for model doors

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Designates this spawnpoint as the default spawn area (ONLY CHECK THIS FOR ONE POINT)")] private bool startPoint;

    //Realtime Variables:
    private int savedRatCount; //Number of ratBoids player will spawn with if they respawn at this point

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        detectorZone = GetComponentInChildren<EffectZone>(); //Get detector zone component from children
        spawnPosition = transform.Find("SpawnPosition");     //Get spawn position transform by name

        //Set up events:
        detectorZone.OnBigRatEnter += SetAsSpawn; //Hook up spawn method to detector zone entry signal
    }
    private void OnDestroy()
    {
        //Unsubscriptions:
        if (detectorZone != null) detectorZone.OnBigRatEnter -= SetAsSpawn; //Unsubscribe on destruction if possible
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Respawns mama rat at this position.
    /// </summary>
    public void Respawn()
    {

    }
    /// <summary>
    /// Sets this point as active spawnpoint.
    /// </summary>
    public void SetAsSpawn()
    {
        //Store rat state:
        currentSpawnPoint = this;                                    //Set this point as active
        savedRatCount = MasterRatController.main.TotalFollowerCount; //Store current follower count of main rat
    }
}
