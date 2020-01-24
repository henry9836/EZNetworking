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
    //Structs
    class PendingID
    {
        public PendingID(int _oldID, int _newID)
        {
            oldID = _oldID;
            newID = _newID;
        }

        public GameObject refgameobject;
        public int oldID;
        public int newID;
    }

    //Publics
    [Range(1, 10)]
    public int SafeSendRepeatCount = 3;
    [Range(1, 10)]
    public int maxRetryCount = 5;
    [Range(5, 15)]
    public int timeoutThreshold = 5;
    public int port = 13371;
    public string targetIP = "127.0.0.1";
    public List<GameObject> spawnableObjects = new List<GameObject>();

    //Privates
    private List<Atlas.ClientObject> clients = new List<Atlas.ClientObject>();
    private List<PendingID> pendingIDObjects = new List<PendingID>();
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
    private int NextID = 1;

    //Const
    private const string idSeperator = "::";
    private const string ACK = "ACK";
    
    public void pleaseWorkAssHole(int oldID, int newID) //Doesn't work because of gameobjects and threading
    {
        Debug.LogError("OLDID: " + oldID.ToString() + " NEWID: " + newID);
        Debug.Log("DEBUG START");
        for (int i = 0; i < pendingIDObjects.Count; i++)
        {
            Debug.Log("DEBUG LOOP");
            if (pendingIDObjects[i].oldID == oldID)
            {
                Debug.Log("DEBUG END");
                Debug.LogError("I FOUND IT YAY {" + newID.ToString() + "}");
                pendingIDObjects[i].newID = newID;
                return;
            }
        }
        Debug.Log("DEBUG END");
        Debug.LogError("Cannot find the gameobject with the ID the server gave to us {" + oldID.ToString() + "}");

    }

    //Assign Target IP
    public void AssignIP(string newIP)
    {
        targetIP = newIP;
    }

    //Return A Valid Network ID
    public int AssignID(GameObject obj, int currentID)
    {
        //If we are the server we can assign the ID
        if (Atlas.isServer)
        {
            Debug.Log("I AM SERVER FOR THING");
            int returnVal = NextID;
            NextID++;
            return returnVal;
        }
        //Request ID From Server
        else
        {
            Debug.Log("REQ SERVER FOR THING");
            pendingIDObjects.Add(new PendingID(currentID, -1));
            pendingIDObjects[pendingIDObjects.Count - 1].refgameobject = obj;
            Send(Atlas.PACKETTYPE.REQISTERNEWOBJID, Encoding.ASCII.GetBytes(obj.GetComponent<NetworkIdentity>().ObjectID.ToString()), listenPoint);

            return -1;
        }
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

        for (int i = 0; i < SafeSendRepeatCount; i++)
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

    public void Spawn(GameObject prefab, Vector3 position, Quaternion quaternion)
    {
        //Check if prefab is in spawnable list
        for (int i = 0; i < spawnableObjects.Count; i++)
        {
            if (spawnableObjects[i] == prefab)
            {
                //Spawn object, let network identity handle network
                Instantiate(prefab, position, quaternion);
                return;
            }
        }
        Debug.LogWarning("Attempted to spawn an object that isn't in the spawnable objects list, ignoring...");
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
            while (!Atlas.networkAuthed && !timeOutHandShake)
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
                //replace with known client
                client = clients[i];
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

            //Check Client For Weird Behaviour
            if (client.authState != Atlas.ClientObject.AUTHTYPE.HANDSHAKE_SUCCEED && type != (int)Atlas.PACKETTYPE.HANDSHAKEACKREQ)
            {
                //client is attempting do things without handshake ignore
                Debug.LogWarning("Client has not completed handshake but is trying to request other actions, ignoring...");
                return;
            }

            switch (type)
            {
                case (int)Atlas.PACKETTYPE.HANDSHAKEACKREQ:
                    {
                        //Send back a ACK
                        SafeSend(Atlas.PACKETTYPE.ACK, Encoding.ASCII.GetBytes(ACK), client.clientEP);
                        client.authState = Atlas.ClientObject.AUTHTYPE.HANDSHAKE_SUCCEED;
                        break;
                    }
                case (int)Atlas.PACKETTYPE.REQISTERNEWOBJID:
                    {
                        //Get Temporary ID
                        int oldID = int.Parse(infoStr);
                        int newID = AssignID(null, -1);
                        Debug.Log("OLD ID: " + oldID.ToString() + " NEW ID: " + newID.ToString());
                        SafeSend(Atlas.PACKETTYPE.NEWOBJID, Encoding.ASCII.GetBytes(oldID.ToString() +"$" + newID.ToString()), client.clientEP);
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
                        Atlas.networkAuthed = true;
                        break;
                    }
                case (int)Atlas.PACKETTYPE.NEWOBJID:
                    {
                        //Get The Old and New ID
                        int newPos = infoStr.IndexOf("$");
                        Debug.LogError(infoStr.Substring(0, newPos));
                        int oldID = int.Parse(infoStr.Substring(0, newPos));
                        int newID = int.Parse(infoStr.Substring(newPos + 1));

                        //Find the Object with the tempID and replace tempID
                        pleaseWorkAssHole(oldID, newID);
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
        Atlas.networkAuthed = false;
        Atlas.ID = -1;

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

    }

    //Server Boot Thread
    private void ServerThread(object state)
    {
        //Update Atlas State
        Atlas.networkActive = true;
        Atlas.isServer = true;
        Atlas.isClient = false;
        Atlas.networkAuthed = true;
        Atlas.ID = 0;

        //Attempt to listen on port on all addresses
        network = new UdpClient(port);

        //Start Client Receive Thread
        Thread serverR = new Thread(serverMainReceiveThread);
        serverR.Start();
    }

    //Assign ids that are in the list
    void AssignPendingIDs()
    {
        for (int i = 0; i < pendingIDObjects.Count; i++)
        {
            if (pendingIDObjects[i].newID > 0)
            {
                //Assign new Network ID
                pendingIDObjects[i].refgameobject.GetComponent<NetworkIdentity>().updateID(pendingIDObjects[i].newID);
                //Remove Pending ID
                pendingIDObjects.RemoveAt(i); 
            }
        }
    }


    //Loop
    void FixedUpdate()
    {
        AssignPendingIDs();
    }
}
