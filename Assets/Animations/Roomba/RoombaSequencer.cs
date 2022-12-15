using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoombaSequencer : MonoBehaviour
{
    //Objects & Components:
    private Animator anim;

    //Settings:
    [SerializeField, Tooltip("Zones which, when entered, will progress roomba animation state")] private EffectZone[] zones;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        anim = GetComponent<Animator>(); //Get animator component
    }
    private void Start()
    {
        foreach (EffectZone zone in zones)
        {
            zone.OnBigRatEnter += ProgressState;
        }
    }
    private void OnDestroy()
    {
        foreach (EffectZone zone in zones)
        {
            if (zone != null)
            {
                zone.OnBigRatEnter -= ProgressState;
            }
        }
    }

    //FUNCTIONALITY METHODS:
    private void ProgressState()
    {
        anim.SetTrigger("Proceed");
    }
}
