using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpawnCube : MonoBehaviour
{
    public GameObject Cube;
    public GameObject Ball;
    public EZNetworking network;
    public bool spawned = false;
    // Start is called before the first frame update
    void FixedUpdate()
    {
        if (Atlas.networkActive && Atlas.isServer && Atlas.networkAuthed)
        //if (Atlas.networkActive && Atlas.isClient && Atlas.networkAuthed)
        {
            if (!spawned)
            {
                network.Spawn(Cube, new Vector3(0, 0, 0), Quaternion.identity);
                spawned = true;
            }
        }
        //else if (Atlas.isServer && Atlas.networkAuthed && Atlas.networkActive)
        else if (Atlas.networkActive && Atlas.isClient && Atlas.networkAuthed)
        {
            if (!spawned)
            {
                network.Spawn(Ball, new Vector3(0, 0, 0), Quaternion.identity);
                spawned = true;
            }
        }
    }
}
