using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterIdle : MonoBehaviour
{

    public Vector2 floatMinMax;
    public Vector2 randomTime = new Vector2(1.0f, 3.0f);

    private float t = 0.0f;
    private bool goingUp = true;
    private float overTimeThreshold = 1.0f;

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, floatMinMax.x, transform.position.z);
        goingUp = true;
        t = 0.0f;
        overTimeThreshold = Random.Range(randomTime.x, randomTime.y);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(transform.position.x, Mathf.Lerp(floatMinMax.x, floatMinMax.y, (t / overTimeThreshold)), transform.position.z);

        if (goingUp)
        {
            if (transform.position.y >= floatMinMax.y)
            {
                goingUp = false;
                overTimeThreshold = Random.Range(randomTime.x, randomTime.y);
            }
            else
            {
                t += Time.deltaTime;
            }
        }
        else
        {
            if (transform.position.y <= floatMinMax.x)
            {
                goingUp = true;
                overTimeThreshold = Random.Range(randomTime.x, randomTime.y);
            }
            else
            {
                t -= Time.deltaTime;
            }
        }

    }
}
