using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnBall : MonoBehaviour
{
    public float respawnDistance = 10.0f;
    public float speed = 1000.0f;

    private Vector3 respawnPoint = Vector3.zero;
    private Rigidbody rb = null;

    private void Start()
    {
        respawnPoint = GameObject.FindGameObjectWithTag("Respawn").transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Atlas.isClient)
        {
            if (Vector3.Distance(respawnPoint, transform.position) > respawnDistance)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                transform.position = respawnPoint;
            }
            rb.AddForce(Vector3.right * speed * Time.deltaTime);
        }
    }
}
