using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{
    //Static Stuff:
    public static Respawner currentSpawnPoint; //Spawn point which player will go to when/if they die

    //Objects & Components:
    private EffectZone detectorZone;  //Zone used to detect when mama rat switches to this spawnpoint
    internal Transform spawnPosition; //Position from which spawned rats are flung
    private Animator anim;            //Animator for model doors

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Designates this spawnpoint as the default spawn area (ONLY CHECK THIS FOR ONE POINT)")]       private bool startPoint;
    [SerializeField, Tooltip("Area around spawnPosition within which new ratboids may be spawned")]                         private Vector2 boidSpawnArea;
    [SerializeField, Tooltip("Power with which RatBoids are launched from door (X is horizontal component, Y is vertical")] private Vector2 boidLaunchPower;
    [SerializeField, Tooltip("How many seconds each ratboid should take to spawn")]                                         private float secondsPerRat;
    [SerializeField, Tooltip("Default speed of door opening/closing animation")]                                            private float baseDoorSpeed = 1;

    //Realtime Variables:
    private int savedRatCount; //Number of ratBoids player will spawn with if they respawn at this point

    //Coroutines:
    IEnumerator RespawnSequence()
    {
        //Initialize:
        MasterRatController bigRat = MasterRatController.main; //Get shortened reference to big rat

        //Begin opening door:
        anim.SetFloat("Speed", 5f);            //Set door to open in given amount of time (hardcoded so I don't have to add all of these times as settings)
        anim.SetBool("Open", true);            //Start door opening animation
        yield return new WaitForSeconds(0.1f); //Wait until door is about halfway open

        //Launch rats out of door:
        bigRat.billboarder.SetVisibility(1);                                            //Make big rat visible again
        bigRat.stasis = false;                                                          //Take big rat out of stasis
        bigRat.noControl = true;                                                        //Briefly take away player control over rat
        Vector3 launchPower = transform.forward * bigRat.settings.respawnLaunchPower.x; //Initialize container for launch power using forward direction and horizontal launch component
        launchPower.y += bigRat.settings.respawnLaunchPower.y;                          //Add vertical component to launch power
        bigRat.Launch(launchPower, false);                                              //Launch rat using designated velocity
        StartCoroutine(SpawnRatBoids(savedRatCount, 0.2f));                             //Queue up ratboid spawn sequence
        yield return new WaitForSeconds(0.2f);                                          //Wait until rat is a good distance from door

        //Final cleanup:
        bigRat.noControl = false; //Return control back to player
    }
    /// <summary>
    /// Spawns given number of ratboids from door within given amount of time.
    /// </summary>
    /// <param name="wait">Optional amount of time to wait before spawning rats.</param>
    /// <returns></returns>
    IEnumerator SpawnRatBoids(int count, float wait = 0)
    {
        //Initialization:
        Vector3 launchPower = transform.forward * boidLaunchPower.x; //Calculate base launch power based off of direction door is facing and horizontal setting
        launchPower.y += boidLaunchPower.y;                          //Add vertical component to launch power
        yield return new WaitForSeconds(wait);                       //Optionally wait a brief period before beginning rat deluge (meant to give doors time to open)

        //Spawn rats:
        for (int i = 0; i < count; i++) //Iterate for given number of cycles
        {
            //Initialization:
            yield return new WaitForSeconds(secondsPerRat);                                      //Wait for given delay before spawning each rat
            Vector3 spawnPos = spawnPosition.position;                                           //Initialize position at set spawnpoint
            spawnPos += transform.right * (Random.Range(-boidSpawnArea.x, boidSpawnArea.x) / 2); //Apply random horizontal deviation to spawnpoint
            spawnPos.y += Random.Range(-boidSpawnArea.y, boidSpawnArea.y) / 2;                   //Apply random vertical deviation to spawnpoint

            //Spawn & launch:
            RatBoid newRat = Instantiate(MasterRatController.main.settings.basicRatPrefab).GetComponent<RatBoid>(); //Instantiate a new rat and get its controller
            newRat.transform.position = spawnPos;                                                                   //Move rat to its generated spawn position
            newRat.flatPos = RatBoid.FlattenVector(spawnPos);                                                       //Mark flat position of rat (this should probably be deprecated at some point)
            newRat.Launch(launchPower);                                                                             //Launch rat using set launch power
        }
    }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        detectorZone = GetComponentInChildren<EffectZone>(); //Get detector zone component from children
        spawnPosition = transform.Find("SpawnPosition");     //Get spawn position transform by name
        anim = GetComponent<Animator>();                     //Get animator component from object

        //Set up events:
        detectorZone.OnBigRatEnter += EnteredZone; //Hook up spawn method to detector zone entry signal
        detectorZone.OnBigRatLeave += LeftZone;    //Hook up secondary method to detector zone leave signal

        //Initialization:
        if (startPoint) currentSpawnPoint = this; //Set this point as current spawnpoint
        anim.SetFloat("Speed", baseDoorSpeed);    //Set doors to default speed
    }
    private void OnDestroy()
    {
        //Unsubscriptions:
        if (detectorZone != null) detectorZone.OnBigRatEnter -= EnteredZone; //Unsubscribe on destruction if possible
        if (detectorZone != null) detectorZone.OnBigRatLeave -= LeftZone;    //Unsubscribe on destruction if possible
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Respawns mama rat at position of current spawnpoint.
    /// </summary>
    public static void Respawn()
    {
        if (currentSpawnPoint != null) currentSpawnPoint.StartCoroutine(currentSpawnPoint.RespawnSequence()); //Begin respawn sequence at current spawnpoint
    }
    /// <summary>
    /// Method called whenever big rat enters door zone.
    /// </summary>
    private void EnteredZone()
    {
        //Spawnpoint upkeep:
        if (currentSpawnPoint == this) //Zone is already set as spawnpoint
        {
            int currentRatCount = MasterRatController.main.TotalFollowerCount; //Store current follower count of master rat controller
            if (currentRatCount > savedRatCount) //Rat has re-entered this spawnpoint with more rats than it had before
            {
                savedRatCount = currentRatCount; //Update current rat count
            }
            else if (currentRatCount < savedRatCount) //Rat has re-entered this spawnpoint with fewer rats than it had before
            {
                StartCoroutine(SpawnRatBoids(savedRatCount - currentRatCount, 0.4f)); //Spawn rats to make up for difference
            }
        }
        else //This zone is being set as new spawnpoint
        {
            //Store rat state:
            currentSpawnPoint = this;                                    //Set this point as active
            savedRatCount = MasterRatController.main.TotalFollowerCount; //Store current follower count of main rat
        }

        //Open doors:
        if (!MasterRatController.main.stasis) anim.SetBool("Open", true); //Trigger door opening animation (but make sure it doesn't interrupt spawn sequence)
    }
    private void LeftZone()
    {
        //Close doors:
        anim.SetFloat("Speed", baseDoorSpeed); //Set doors to default speed
        anim.SetBool("Open", false);           //Trigger door closing animation
    }
}
