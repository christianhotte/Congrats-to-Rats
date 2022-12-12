using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodPickup : EffectZone
{
    //Objects & Components:
    [SerializeField, Tooltip("Hidden animated sparkle sprite which becomes visible when food is consumed")] private SpriteRenderer sparkle;

    //Settings:
    [Header("Food Settings:")]
    [SerializeField, Min(1), Tooltip("Total number of rats which will be spawned by this food")] private int ratNumber;
    [SerializeField, Min(0.01f), Tooltip("Time taken for food to lerp to held position")]        private float holdLerpTime;
    [SerializeField, Tooltip("Scale food lerps to when being held")]                             private float holdScale;
    [SerializeField, Tooltip("Curve describing speed of food as it lerps to held position")]     private AnimationCurve holdLerpCurve;
    [SerializeField, Tooltip("Time to wait with food held before beginning rat spawn")]          private float holdWait;
    [SerializeField, Tooltip("Time to wait after spawning before returning control to player")]  private float postSpawnWait;

    //Runtime Variables:

    //Coroutines:
    /// <summary>
    /// Procedure which executes when food is eaten.
    /// </summary>
    IEnumerator FoodProcedure()
    {
        //Move food to position:
        Vector3 originPos = transform.localPosition;                                                                            //Get current position of this food item
        Vector3 targetPos = Vector3.zero;                                                                                       //Initialize target position at center of big rat
        targetPos += MasterRatController.main.billboarder.transform.right * MasterRatController.main.settings.heldFoodOffset.x; //Offset target position horizontally by set amount (relative to facing position)
        targetPos += MasterRatController.main.billboarder.transform.up * MasterRatController.main.settings.heldFoodOffset.y;    //Offset target position vertically by set amount (relative to facing position)
        float originScl = transform.localScale.x;                                                                               //Get initial scale of object
        float targetScl = originScl * holdScale;                                                                                //Calculate final target scale based on settings
        for (float timePassed = 0; timePassed <= holdLerpTime; timePassed += Time.fixedDeltaTime) //Iterate every fixed update for set number of seconds
        {
            float timeValue = Mathf.Clamp01(timePassed / holdLerpTime);                       //Get value representing current percentage of total time
            timeValue = holdLerpCurve.Evaluate(timeValue);                                    //Adjust time value based on evaluation of lerp curve
            transform.localPosition = Vector3.Lerp(originPos, targetPos, timeValue);          //Smoothly move food to designated position over time
            transform.localScale = Vector3.one * Mathf.Lerp(originScl, targetScl, timeValue); //Smoothly scale food to designated size over time
            yield return new WaitForFixedUpdate();                                            //Wait for next fixed update
        }
        transform.localPosition = targetPos;       //Lock sprite to target position
        sparkle.enabled = true;                    //Make sparkle visible
        yield return new WaitForSeconds(holdWait); //Wait for designated number of seconds while holding food

        //Spawn rats:
        Destroy(transform.GetChild(0).gameObject);                                                                         //Destroy visual elements, leaving only this script
        MasterRatController.main.anim.SetTrigger("Proceed");                                                               //Begin spawning animation
        yield return new WaitForSeconds(0.2f);                                                                             //Wait for animation to reach its crescendo
        float spawnTime = ratNumber * 0.01f;                                                                               //Get total time taken to spawn all rats
        MasterRatController.main.StartCoroutine(MasterRatController.main.SpawnRatsOverTime(ratNumber, ratNumber * 0.01f)); //Trigger rat spawn routine
        yield return new WaitForSeconds(spawnTime + postSpawnWait);                                                        //Wait until rats have finished spawning

        //Cleanup:
        MasterRatController.main.anim.SetBool("HoldingFood", false); //End food animation
        yield return new WaitForSeconds(0.15f);                      //Briefly wait to allow animation to begin
        MasterRatController.main.noControl = false;                  //Pass control back to player
        Destroy(gameObject);                                         //Destroy cheese after spawn sequence has been completed
    }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Event subscriptions:
        OnBigRatEnter += EatFood; //Subscribe food-eating method to big rat entry event
    }
    private void OnDestroy()
    {
        //Event unsubscriptions:
        OnBigRatEnter -= EatFood; //Unsubscribe from event
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Begins food consumption procedure.
    /// </summary>
    private void EatFood()
    {
        //Initialize micro-cutscene:
        transform.parent = MasterRatController.main.transform;      //Child food to big rat transform
        MasterRatController.main.noControl = true;                  //Take control away from player
        MasterRatController.main.anim.SetBool("HoldingFood", true); //Begin food animation

        //Cleanup:
        Clear();                         //Deactivate all zone functionality
        StartCoroutine(FoodProcedure()); //Begin food procedure
    }
}
