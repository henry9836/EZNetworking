using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpawnCube : MonoBehaviour
{
    public GameObject Cube;
    public GameObject Ball;
    public EZNetworking network;
    public bool spawned = false;

    private float cmdThreshold = 1.0f;
    private float cmdTimer = 0.0f;
    // Start is called before the first frame update
    void FixedUpdate()
    {
        //Random Commands
        if (Atlas.isServer)
        {
            cmdTimer += Time.deltaTime;
            if (cmdTimer > cmdThreshold)
            {
                GetComponent<EZNetworking>().sendCommand(CommandHandler.COMMANDTYPE.TESTING, "Hello Networking", false, false);
                GetComponent<EZNetworking>().sendCommand(CommandHandler.COMMANDTYPE.TESTING, "Hello Mr. Special", 1, false, false);

                cmdThreshold = Random.Range(1.0f, 3.0f);
                cmdTimer = 0.0f;
            }
        }


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
