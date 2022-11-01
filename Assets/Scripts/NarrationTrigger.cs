using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to trigger a specific narration using the player's MusicManager system.
/// </summary>
public class NarrationTrigger : MonoBehaviour
{
    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("The narration clip this object will play")]                   private AudioClip narration;
    [SerializeField, Tooltip("Whether or not this narration can be played multiple times")] private bool repeatable;

    //RUNTIME METHODS:
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MusicManager musicManager)) //Check if collision was from Rat's music manager
        {
            musicManager.PlayNarration(narration); //Activate this trigger's specific narration
            if (!repeatable) Destroy(gameObject);  //Destroy self if not repeatable
        }
    }
}
