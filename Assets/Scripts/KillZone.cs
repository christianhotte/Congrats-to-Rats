using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZone : EffectZone
{
    private void Awake()
    {
        OnBigRatEnter += KillBigRat;
    }
    private void OnDestroy()
    {
        OnBigRatEnter -= KillBigRat;
    }
    public void KillBigRat()
    {
        if (MasterRatController.main.falling) MasterRatController.main.anim.SetTrigger("Land");
        MasterRatController.main.Kill();
    }
    public override void AffectRat(RatBoid rat, float deltaTime)
    {
        Destroy(rat.gameObject);
    }
}
