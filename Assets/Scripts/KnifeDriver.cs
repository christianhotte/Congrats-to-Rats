using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeDriver : MonoBehaviour
{
    private EffectZone triggerZone;
    private AudioSource source;
    private Renderer r;
    private Rigidbody rb;

    [Header("Sounds")]
    public AudioClip fallSound;
    public AudioClip thunkSound;
    public float fallTime;

    IEnumerator ScheduleThunk()
    {
        yield return new WaitForSeconds(fallTime);
        source.PlayOneShot(thunkSound);
    }

    private bool activated = false;

    private void Awake()
    {
        triggerZone = transform.Find("Trigger").GetComponent<EffectZone>(); //Get trigger zone from children
        r = GetComponentInChildren<Renderer>();                             //Get renderer
        rb = GetComponentInChildren<Rigidbody>();                           //Get rigidbody
        source = GetComponent<AudioSource>();                               //Get audioSource component
        triggerZone.OnBigRatEnter += Activate;                              //Set up event subscription
        r.enabled = false;                                                  //Disable renderer
    }
    private void OnDestroy()
    {
        if (triggerZone != null) triggerZone.OnBigRatEnter -= Activate;
    }

    public void Activate()
    {
        if (activated) return; else activated = true; //Make sure system can only activate once
        StartCoroutine(ScheduleThunk());
        r.enabled = true;
        rb.useGravity = true;
        rb.isKinematic = false;
        source.PlayOneShot(fallSound);
    }
}
