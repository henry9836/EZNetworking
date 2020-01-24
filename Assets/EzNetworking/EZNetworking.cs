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

    private List<Atlas.ClientObject> clients = new List<Atlas.ClientObject>();

    //Privates
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
    private int NextID = 1;
    private bool clientHasGotACK = false;

    //Const
    private const string idSeperator = "::";
    private const string ACK = "ACK";
    


    //Assign Target IP
    public void AssignIP(string newIP)
    {
        targetIP = newIP;
    }

    //Return A Valid Network ID
    public int AssignID()
    {
        int returnVal = NextID;
        NextID++;
        return returnVal;
    }

    //Sends a packet
    public void Send(Atlas.PACKETTYPE type, byte[] data, IPEndPoint ep)
    {
        if (data.Length <= 0)
        {
            Debug.LogWarning("A null packet was called to send, ignoring...");
            return;
        }

        //Insert type infront of data
        byte[] header = Encoding.ASCII.GetBytes(((int)type) + idSeperator);
        byte[] packet = new byte[header.Length + data.Length];

        header.CopyTo(packet, 0);
        data.CopyTo(packet, header.Length);

        if (Atlas.isServer && Atlas.isClient)
        {
            Debug.LogWarning("Cannot determine if Atlas mode is Server or Client as both are True");
        }
        else if (Atlas.isClient)
        {
            network.Send(packet, packet.Length);
        }
        else if (Atlas.isServer)
        {
            network.Send(packet, packet.Length, ep);
        }
        else
        {
            Debug.LogWarning("Atlas doesn't have a mode selected");
        }
    }

    //Sends a packet multiple times
    public void SafeSend(Atlas.PACKETTYPE type, byte[] data, IPEndPoint ep)
    {
        if (data.Length <= 0)
        {
            Debug.LogWarning("A null packet was called to send, ignoring...");
            return;
        }

        for (int i = 0; i < reapeatSendCount; i++)
        {
            //Insert type infront of data
            byte[] header = Encoding.ASCII.GetBytes(((int)type) + idSeperator);
            byte[] packet = new byte[header.Length + data.Length];
            header.CopyTo(packet, 0);
            data.CopyTo(packet, header.Length);

            if (Atlas.isServer && Atlas.isClient)
            {
                Debug.LogWarning("Cannot determine if Atlas mode is Server or Client as both are True");
            }
            else if (Atlas.isClient)
            {
                network.Send(packet, packet.Length);
            }
            else if (Atlas.isServer)
            {
                network.Send(packet, packet.Length, ep);
            }
            else
            {
                Debug.LogWarning("Atlas doesn't have a mode selected");
            }
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

        Thread mainServerThread = new Thread(ServerThread);

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
            Send(Atlas.PACKETTYPE.HANDSHAKEACKREQ, Encoding.ASCII.GetBytes("Hello Server!"), listenPoint);

            //Server Reply
            while (!clientHasGotACK && !timeOutHandShake)
            {
                //Wait for either the timeout or a ACK packet
            }

            //Handshake has not reached timeout
            if (!timeOutHandShake)
            {
                Debug.Log("Handshake Completed");

                //Request Game Infomation From Server
                SafeSend(Atlas.PACKETTYPE.REQALLGAMEINFO, Encoding.ASCII.GetBytes("SENDGAMEINFO"), listenPoint);

                return;
            }
            else
            {
                Debug.Log("Handshake Failed Retrying...");
            }

        }

        Debug.LogError("Could Not Complete Handshake");
    }

    //Server Process Incoming Data
    private void serverReceiveThread(object state)
    {
        Atlas.ClientObject client = state as Atlas.ClientObject;
        byte[] data = client.lastMessage;
        //Have we seen this client before?
        bool unknownClient = true;
        for (int i = 0; i < clients.Count; i++)
        {
            Debug.Log("Comparing: " + clients[i].clientEP.Address.ToString() + " to: " + client.clientEP.Address.ToString());
            //We know this client ip
            if (clients[i].clientEP.Address.ToString() == client.clientEP.Address.ToString())
            {
                unknownClient = false;
            }
        }
        //Add client if we do not know it
        if (unknownClient)
        {
            client.authState = Atlas.ClientObject.AUTHTYPE.NOHANDSHAKE;
            clients.Add(client);
            Debug.Log("New Client Found!");
        }

        //Heartbeat
        client.resetHeart();

        //Decode Data Type
        string dataStr = Encoding.ASCII.GetString(data);
        Debug.LogError("Server Received: " + dataStr);

        int cutPosition = dataStr.IndexOf(idSeperator);

        if (cutPosition != -1)
        {
            int type = int.Parse(dataStr.Substring(0, cutPosition));
            string infoStr = dataStr.Substring(cutPosition+idSeperator.Length);

            Debug.Log("Infomation: type{" + type.ToString() +"} infomation: " + infoStr);

            switch (type)
            {
                case (int)Atlas.PACKETTYPE.HANDSHAKEACKREQ:
                    {
                        //Send back a ACK
                        SafeSend(Atlas.PACKETTYPE.ACK, Encoding.ASCII.GetBytes(ACK), client.clientEP);
                        break;
                    }

                default:
                    {
                        Debug.LogWarning("Packet has unknown type {" + type.ToString() + "}, ignoring...");
                        break;
                    }
            }

        }
        else
        {
            Debug.LogWarning("Received a packet without a type, ignoring...");
        }
               
    }

    //Client Process Incoming Data
    private void clientReceiveThread(object state)
    {
        byte[] data = state as byte[];

        //Decode Data Type
        string dataStr = Encoding.ASCII.GetString(data);
        Debug.LogError("Client Received: " + dataStr);

        int cutPosition = dataStr.IndexOf(idSeperator);

        if (cutPosition != -1)
        {
            int type = int.Parse(dataStr.Substring(0, cutPosition));
            string infoStr = dataStr.Substring(cutPosition + idSeperator.Length);

            Debug.Log("Packet: type{" + type.ToString() + "} infomation: " + infoStr);

            switch (type)
            {
                case (int)Atlas.PACKETTYPE.ACK:
                    {
                        //Server has send a ACK
                        clientHasGotACK = true;
                        break;
                    }

                default:
                    {
                        Debug.LogWarning("Packet has unknown type {" + type.ToString() + "}, ignoring...");
                        break;
                    }
            }

        }
        else
        {
            Debug.LogWarning("Received a packet without a type, ignoring...");
        }
    }

        //High Priority Server Loop
        private void serverMainReceiveThread(object state)
    {
        while (Atlas.networkActive)
        {
            //Recieve data
            listenPoint = new IPEndPoint(IPAddress.Any, port);
            byte[] data = network.Receive(ref listenPoint);

            //Build a temporary client object and send it to the threadpool for processing
            Atlas.ClientObject tmp = new Atlas.ClientObject(listenPoint, data);
            ThreadPool.QueueUserWorkItem(serverReceiveThread, tmp);

        }
    }

    private void clientMainReieveThread(object state)
    {
        while (Atlas.networkActive)
        {
            byte[] dataIn = network.Receive(ref listenPoint);

            //Process Data
            ThreadPool.QueueUserWorkItem(clientReceiveThread, dataIn);
        }
    }

    //Client Loop
    private void ClientLoop(object state)
    {
        //Update Atlas State
        Atlas.networkActive = true;
        Atlas.isServer = false;
        Atlas.isClient = true;
        clientHasGotACK = false;

        //Connect to Server
        network = new UdpClient();
        listenPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);
        Debug.LogError("Connecting To: " + targetIP);
        network.Connect(listenPoint);

        //Start Client Receive Thread
        Thread clientR = new Thread(clientMainReieveThread);
        clientR.Start();

        //Attempt Handshake
        ClientHandShake();

        //const string safe = "Client Send Test!\n";
        //while (Atlas.networkActive)
        //{
        //    Send(Atlas.PACKETTYPE.UNASSIGNED, Encoding.ASCII.GetBytes(safe), listenPoint);
        //    Thread.Sleep(1000);
        //}

    }

    //Server Boot Thread
    private void ServerThread(object state)
    {
        //Update Atlas State
        Atlas.networkActive = true;
        Atlas.isServer = true;
        Atlas.isClient = false;

        //Attempt to listen on port on all addresses
        network = new UdpClient(port);
        



        //Start Client Receive Thread
        Thread serverR = new Thread(serverMainReceiveThread);
        serverR.Start();
    }
}
