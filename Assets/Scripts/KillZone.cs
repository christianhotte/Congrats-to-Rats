using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZone : EffectZone
{
    private AudioSource source;
    public AudioClip[] killSounds;

    private void Awake()
    {
        OnBigRatEnter += KillBigRat;
        source = GetComponent<AudioSource>();
    }
    private void OnDestroy()
    {
        OnBigRatEnter -= KillBigRat;
    }
    public void KillBigRat()
    {
        if (source != null && killSounds.Length > 0)
        {
            source.PlayOneShot(killSounds[Random.Range(0, killSounds.Length)]); //Play random kill sound
        }
        if (MasterRatController.main.falling) MasterRatController.main.anim.SetTrigger("Land");
        MasterRatController.main.Kill();
    }
    public override void AffectRat(RatBoid rat, float deltaTime)
    {
        Destroy(rat.gameObject);
    }
}
