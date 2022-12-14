using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to trigger a shift in music phase
/// </summary>
public class MusicPhaseAdvancer : MonoBehaviour
{
    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("Phase which this trigger will advance the music to")] private int targetPhase;

    //RUNTIME METHODS:
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MusicManager musicManager)) //Check if collision was from Rat's music manager
        {
            musicManager.AdvanceToPhase(targetPhase); //Advance music to target phase
            Destroy(gameObject);                      //Destroy self
        }
    }
}
