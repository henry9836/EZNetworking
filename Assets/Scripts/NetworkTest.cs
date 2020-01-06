using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{

    const int port = 13371;

    public void StartServer()
    {
        Debug.Log("Server Mode");
    }

    public void StartClient()
    {
        Debug.Log("Client Mode");
    }

    private void Start()
    {
        
    }
}
