using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EZNetworking : MonoBehaviour
{
    //Publics
    [Range(1, 10)]
    public int reapeatSendCount = 3;
    [Range(1, 10)]
    public int maxRetryCount = 5;
    [Range(5, 15)]
    public int timeoutThreshold = 5;
    public int port = 13371;
    public string targetIP = "127.0.0.1";

    //Privates
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
    private int NextID = 1;
    private List<Atlas.ClientObject> clients;

    //Return A Valid Network ID
    public int AssignID()
    {
        int returnVal = NextID;
        NextID++;
        return returnVal;
    }

    //Sends a packet
    public void Send(Atlas.PACKETTYPE type, byte[] data)
    {
        if (data.Length <= 0)
        {
            Debug.LogWarning("A null packet was called to send, ignoring...");
            return;
        }

        //Insert type infront of data
        byte[] header = Encoding.ASCII.GetBytes(".:" + ((int)type) + ":.");
        byte[] packet = new byte[header.Length + data.Length];

        header.CopyTo(packet, 0);
        data.CopyTo(packet, header.Length);

        Debug.Log("Pack: " + Encoding.ASCII.GetString(packet));

        network.Send(packet, packet.Length);
    }

    //Sends a packet multiple times
    public void SafeSend(Atlas.PACKETTYPE type, byte[] data)
    {
        if (data.Length <= 0)
        {
            Debug.LogWarning("A null packet was called to send, ignoring...");
            return;
        }

        for (int i = 0; i < reapeatSendCount; i++)
        {
            //Insert type infront of data
            byte[] header = Encoding.ASCII.GetBytes(".:" + ((int)type) + ":.");
            byte[] packet = new byte[header.Length + data.Length];
            header.CopyTo(packet, 0);
            data.CopyTo(packet, header.Length);

            network.Send(packet, packet.Length);
        }
    }

    public void Start()
    {
        timeoutThreshold *= 1000; //convert from seconds to milliseconds
    }

    //Initializers
    public void StartServer()
    {
        Debug.LogError("Server Mode");

        Thread mainServerThread = new Thread(ServerLoop);

        mainServerThread.Start();

    }

    public void StartClient()
    {
        Debug.LogError("Client Mode");

        Thread mainClientThread = new Thread(ClientLoop);

        mainClientThread.Start();

    }

    //Timeout Thread
    private void TimeoutThread()
    {
        Debug.Log("Timeout Thread Started");
        Thread.Sleep(timeoutThreshold);
        timeOutHandShake = true;
        return;
    }

    //Handshake for Client
    void ClientHandShake()
    {
        for (int i = 0; i < maxRetryCount; i++)
        {
            Debug.Log("Attempting Handshake. Attempt: " + i.ToString());

            //Setup Timeout

            timeOutHandShake = false;
            Thread timeoutThread = new Thread(TimeoutThread);
            timeoutThread.Start();

            //Hello Server?
            Send(Atlas.PACKETTYPE.HANDSHAKEACKREQ, Encoding.ASCII.GetBytes("hello"));

            //Request Game Infomation From Server


            Thread.Sleep(timeoutThreshold + (3 - i));

            //Handshake has not reached timeout
            if (!timeOutHandShake)
            {
                Debug.Log("Handshake Completed");
                return;
            }

        }

        Debug.LogError("Could Not Complete Handshake");
    }

    //Recieve Threads

    private void serverReieveThread(object state)
    {
        while (Atlas.networkActive)
        {
            byte[] dataIn = network.Receive(ref listenPoint);
            Debug.LogError("Server Received: " + Encoding.ASCII.GetString(dataIn));
            network.Send(new byte[] { 1 }, 1, listenPoint); // if data is received reply letting the client know that we got his data          
        }
    }

    private void clientReieveThread(object state)
    {
        while (Atlas.networkActive)
        {
            byte[] dataIn = network.Receive(ref listenPoint);
            Debug.LogError("Client Received: " + Encoding.ASCII.GetString(dataIn));
        }
    }

        //Client Loop
    private void ClientLoop(object state)
    {

        Atlas.networkActive = true;

        //Connect to Server
        network = new UdpClient();
        listenPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);
        Debug.LogError("Connecting To: " + targetIP);
        network.Connect(listenPoint);
        Debug.LogError("Connected!");

        //Start Client Receive Thread
        Thread clientR = new Thread(clientReieveThread);
        clientR.Start();

        //Attempt Handshake
        ClientHandShake();

        Atlas.isServer = false;
        Atlas.isClient = true;

        const string safe = "Client Send Test!\n";
        while (Atlas.networkActive)
        {
            Send(Atlas.PACKETTYPE.UNASSIGNED, Encoding.ASCII.GetBytes(safe));
            Thread.Sleep(1000);
        }

    }

    //Server Loop
    private void ServerLoop(object state)
    {

        Atlas.networkActive = true;

        //Attempt to listen on port on all addresses
        network = new UdpClient(port);
        listenPoint = new IPEndPoint(IPAddress.Any, port);

        //Update Atlas State
        Atlas.isServer = true;
        Atlas.isClient = false;

        //Start Client Receive Thread
        Thread serverR = new Thread(serverReieveThread);
        serverR.Start();

        //const string safe = "Server Send Test!\n";
        //while (Atlas.networkActive)
        //{
        //    Send(Atlas.PACKETTYPE.UNASSIGNED, Encoding.ASCII.GetBytes(safe));
        //    Thread.Sleep(1000);
        //}
    }
}
