using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAroundScriptTest : MonoBehaviour
{

    public float moveDistance = 10.0f;
    public float moveSpeed = 1.0f;

    private Vector3 startPos;
    private bool moveAway;

    private void Start()
    {
        startPos = transform.position;
        if (Atlas.isClient)
        {
            StartCoroutine(RandomStuff());
        }
    }

    private void Update()
    {
        if (Atlas.isServer)
        {
            if (GetComponent<NetworkIdentity>().ownerID == Atlas.ID)
            {
                //Move
                if (transform.position.x > (startPos + (new Vector3(1, 0, 0) * moveDistance)).x)
                {
                    moveAway = false;
                }
                if (transform.position.x < (startPos - (new Vector3(1, 0, 0) * moveDistance)).x)
                {
                    moveAway = true;
                }
                if (moveAway)
                {
                    transform.position += (new Vector3(1, 0, 0) * moveSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position -= (new Vector3(1, 0, 0) * moveSpeed * Time.deltaTime);
                }
            }
        }
    }

    IEnumerator RandomStuff()
    {
        while (true)
        {
            if (GetComponent<NetworkIdentity>().ownerID == Atlas.ID)
            {
                //Scale
                transform.localScale = new Vector3(Random.Range(0.5f, 3.0f), Random.Range(0.5f, 3.0f), Random.Range(0.5f, 3.0f));
                //Rotate
                transform.rotation = Quaternion.Euler(Random.Range(-360.0f, 360.0f), Random.Range(-360.0f, 360.0f), Random.Range(-360.0f, 360.0f));
            }

            yield return new WaitForSecondsRealtime(Random.Range(0.5f, 3.0f));
        }
        yield return null;
    }

}
