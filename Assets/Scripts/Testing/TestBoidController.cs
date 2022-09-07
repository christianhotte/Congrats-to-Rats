using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBoidController : MonoBehaviour
{
    private Transform billboard;
    internal Vector2 velocity;
    internal SpriteRenderer r;
    internal float timeUntilFlip;
    [SerializeField, Min(0)] private float scaleRandomness;
    [SerializeField] private AnimationCurve bobCurve;
    [SerializeField] private float maxBobHeight;
    [SerializeField] private float bobTime;
    private float currentBobTime;

    private void Start()
    {
        //Get objects & components:
        r = GetComponentInChildren<SpriteRenderer>();
        billboard = transform.GetChild(0);                   //Get billboard child object
        billboard.rotation = Camera.main.transform.rotation; //Rotate billboard

        //Randomize initial settings:
        if (scaleRandomness > 0)
        {
            float s = Random.Range(1 - scaleRandomness, 1 + scaleRandomness); //Randomize scale
            transform.localScale *= s;
        }
        currentBobTime = Random.Range(0, bobTime); //Randomize point in bob cycle
        DoBob();
    }
    private void Update()
    {
        //Update flip timer:
        if (timeUntilFlip > 0)
        {
            timeUntilFlip = Mathf.Max(0, timeUntilFlip - Time.deltaTime); //Update flip timer if necessary
            if (timeUntilFlip == 0)
            {
                bool prevFlip = r.flipX;
                if (velocity.x < 0) r.flipX = false;
                else if (velocity.x > 0) r.flipX = true;
                if (prevFlip != r.flipX) timeUntilFlip = 0.4f;
            }
        }

        //Adjust bob height:
        currentBobTime += Time.deltaTime;
        if (currentBobTime > bobTime) currentBobTime -= bobTime;
        DoBob();
    }

    //UTILITY METHODS:
    private void DoBob()
    {
        float t = bobCurve.Evaluate(currentBobTime / bobTime);
        float scaleMultiplier = (1 - transform.localScale.x) + 1;
        float currentMaxHeight = maxBobHeight * scaleMultiplier;
        float height = Mathf.Lerp(BoidManager.main.spawnHeight, currentMaxHeight, t);
        Vector3 newPos = transform.position; newPos.y = height;
        transform.position = newPos;
    }
}
