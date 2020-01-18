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
    private bool serverMode = false;
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
   
    
    //Sends a packet
    public void Send(Atlas.PACKETTYPE type, byte[] data)
    {
        if (data.Length <= 0)
        {
            Debug.LogWarning("A null packet was called to send, ignoring...");
            return;
        }

        //Insert type infront of data
        byte[] header = Encoding.ASCII.GetBytes(":::" + ((int)type) + ":::");
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
            byte[] header = Encoding.ASCII.GetBytes(((int)type) + "::%%::");
            byte[] packet = new byte[header.Length + data.Length];
            header.CopyTo(packet, 0);
            data.CopyTo(packet, 0);

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

        ThreadPool.QueueUserWorkItem(ServerLoop);

    }

    public void StartClient()
    {
        Debug.LogError("Client Mode");

        ThreadPool.QueueUserWorkItem(ClientLoop);

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

    //Client Loop
    private void ClientLoop(object state)
    {

        network = new UdpClient();

        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);


        Debug.LogError("Connecting To: " + targetIP);
        network.Connect(ipEndPoint);
        Debug.LogError("Connected!");

        ClientHandShake();

        //const string safe = "Safe Send Test!\n";
        //while (true)
        //{
        //    Debug.LogError("Sending Messages...");
        //    SafeSend(Atlas.PACKETTYPE.UNASSIGNED, Encoding.ASCII.GetBytes(safe), safe.Length);
        //    Debug.LogError("Done.");
        //    Thread.Sleep(1000);
        //}




    }

    //Server Loop
    private void ServerLoop(object state)
    {

        network = new UdpClient(port);
        listenPoint = new IPEndPoint(IPAddress.Any, port);

        serverMode = true;

        while (true)
        {
            byte[] dataIn = network.Receive(ref listenPoint);
            Debug.LogError("Server Received: " + Encoding.ASCII.GetString(dataIn));
        }
    }
}
