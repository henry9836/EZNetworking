using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public EZNetworking NetworkInterface;
    public GameObject player;
    public List<Transform> spawns = new List<Transform>();

    private bool spawned;

    private void Start()
    {
        spawned = false;
    }

    private void Update()
    {
        if (Atlas.networkAuthed && !spawned)
        {
            NetworkInterface.Spawn(player, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity);
            spawned = true;
        }
    }


}
