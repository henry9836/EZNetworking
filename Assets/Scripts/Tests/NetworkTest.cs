using System.Collections;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{

    public int port = 13371;
    public string targetIP = "127.0.0.1";

    private bool serverMode = false;
    private IPEndPoint listenPoint;
    private UdpClient server;

    public void StartServer()
    {
        Debug.Log("Server Mode");

        ThreadPool.QueueUserWorkItem(ServerLoop);

    }

    public void StartClient()
    {
        Debug.Log("Client Mode");

        var client = new UdpClient();

        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);


        Debug.Log("Connecting To: " + targetIP);
        client.Connect(ipEndPoint);
        Debug.Log("Connected!");
        Debug.Log("Sending Messages...");

        const string one = "Hello This is a UDP Connection Test!\n";
        const string two = "yay :D\n";
        const string three = "goodbye\n";

        client.Send(Encoding.ASCII.GetBytes(one), one.Length);
        client.Send(Encoding.ASCII.GetBytes(two), two.Length);
        client.Send(Encoding.ASCII.GetBytes(three), three.Length);

        Debug.Log("Done.");

    }

    private void ServerLoop(object state)
    {

        server = new UdpClient(port);
        listenPoint = new IPEndPoint(IPAddress.Any, port);

        serverMode = true;

        while (true)
        {
            byte[] dataIn = server.Receive(ref listenPoint);
            Debug.Log("Server Received: " + Encoding.ASCII.GetString(dataIn));
        }
    }
}
