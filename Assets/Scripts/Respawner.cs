using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{
    //Static Stuff:
    public static Respawner currentSpawnPoint; //Spawn point which player will go to when/if they die

    //Objects & Components:
    private EffectZone detectorZone; //Zone used to detect when mama rat switches to this spawnpoint

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Designates this spawnpoint as the default spawn area (ONLY CHECK THIS FOR ONE POINT)")] private float startPoint;

    //Realtime Variables:
    private int savedRatCount; //Number of ratBoids player will spawn with if they respawn at this point

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Respawns mama rat at this position.
    /// </summary>
    public void Respawn()
    {

    }
}
