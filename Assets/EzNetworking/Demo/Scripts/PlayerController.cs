using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed = 1000.0f;


    private Rigidbody rb;


    // Start is called before the first frame update
    void Start()
    {
        //Fix rot
        transform.rotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 forceDir = Vector3.zero;

        forceDir += transform.up * Input.GetAxisRaw("Vertical");
        forceDir += transform.right * Input.GetAxisRaw("Horizontal");

        rb.AddForce(forceDir * moveSpeed * Time.deltaTime);

    }
}
