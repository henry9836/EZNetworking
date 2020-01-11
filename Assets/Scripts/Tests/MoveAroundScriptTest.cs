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
    }

    private void Update()
    {
        if (transform.position.x > (startPos + (new Vector3(1,0,0) * moveDistance)).x)
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
