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
    //Pending ID Class to be used for queuing Obj IDs
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

    class PendingWorkItem
    {
        public enum WorkType
        {
            UNASSIGNED,
            TRANSFORMUPDATE
        }

        public PendingWorkItem()
        {

        }

        public PendingWorkItem(bool _safeSendFlag, int _senderID, int _objID, int _objType, string _objData, WorkType _workType)
        {
            safeSendFlag = _safeSendFlag;
            senderID = _senderID;
            objID = _objID;
            objType = _objType;
            objData = _objData;
            workType = _workType;
        }


        public bool safeSendFlag = false;
        public int senderID = -1;
        public int objID = -1;
        public int objType = -1;
        public string objData = "";
        public WorkType workType = WorkType.UNASSIGNED;


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
    private List<PendingWorkItem> pendingWorkItems = new List<PendingWorkItem>();
    private List<GameObject> localObjs = new List<GameObject>();
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
    private int NextObjID = 1;
    private int NextNetID = 1;

    
         

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
            int returnVal = NextObjID;
            NextObjID++;
            return returnVal;
        }
        //Request ID From Server
        else
        {
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
        byte[] header = Encoding.ASCII.GetBytes(((int)type) + Atlas.packetTypeSeperator);
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
            byte[] header = Encoding.ASCII.GetBytes(((int)type) + Atlas.packetTypeSeperator);
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

        Atlas.networkActive = false;
        Atlas.networkAuthed = false;
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
        Debug.Log("Server Received: " + dataStr);

        int cutPosition = dataStr.IndexOf(Atlas.packetTypeSeperator);

        if (cutPosition != -1)
        {
            int type = int.Parse(dataStr.Substring(0, cutPosition));
            string infoStr = dataStr.Substring(cutPosition+Atlas.packetTypeSeperator.Length);

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
                        //Send back a ACK with the client's ID
                        string ACK = "ACK:" + NextNetID.ToString();
                        NextNetID++;
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
                case (int)Atlas.PACKETTYPE.TRANSFORM:
                    {
                        //Decode Packet
                        //Debug.Log("DEBUG STR: " + Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator));

                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetObjectIDSeperator));
                        int objID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectIDSeperator, Atlas.packetObjectLocalAuthSeperator));
                        int objType = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectLocalAuthSeperator, Atlas.packetObjectTypeSeperator));
                        string objData = infoStr.Substring(infoStr.IndexOf(Atlas.packetObjectDataSeperator) + Atlas.packetObjectDataSeperator.Length);

                        Debug.Log("SENDER ID: " + senderID.ToString() + " SAFE SEND: " +  safeSendFlag + " OBJID: " + objID + " OBJTYPE: " + objType + " data["+objData+"]");

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        //If safesend tell client we got it

                        //Queue Work
                        pendingWorkItems.Add(new PendingWorkItem(safeSendFlag, senderID, objID, objType, objData, PendingWorkItem.WorkType.TRANSFORMUPDATE));

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
        Debug.Log("Client Received: " + dataStr);

        int cutPosition = dataStr.IndexOf(Atlas.packetTypeSeperator);

        if (cutPosition != -1)
        {
            int type = int.Parse(dataStr.Substring(0, cutPosition));
            string infoStr = dataStr.Substring(cutPosition + Atlas.packetTypeSeperator.Length);

            Debug.Log("Packet: type{" + type.ToString() + "} infomation: " + infoStr);

            switch (type)
            {
                case (int)Atlas.PACKETTYPE.ACK:
                    {
                        //Server has send a ACK
                        //Get Our NetID
                        Atlas.ID = int.Parse(infoStr.Substring(infoStr.IndexOf(":") + 1));
                        Atlas.networkAuthed = true;
                        break;
                    }
                case (int)Atlas.PACKETTYPE.NEWOBJID:
                    {
                        //Get The Old and New ID
                        int newPos = infoStr.IndexOf("$");
                        Debug.Log(infoStr.Substring(0, newPos));
                        int oldID = int.Parse(infoStr.Substring(0, newPos));
                        int newID = int.Parse(infoStr.Substring(newPos + 1));

                        //Find the Object with the tempID and replace tempID
                        Debug.Log("OLDID: " + oldID.ToString() + " NEWID: " + newID);
                        for (int i = 0; i < pendingIDObjects.Count; i++)
                        {
                            if (pendingIDObjects[i].oldID == oldID)
                            {
                                pendingIDObjects[i].newID = newID;
                                return;
                            }
                        }
                        Debug.Log("DEBUG END");
                        Debug.LogError("Cannot find the gameobject with the ID the server gave to us {" + oldID.ToString() + "}");

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
        Debug.Log("Connecting To: " + targetIP);
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

    // **************************************
    // | UNITY MAIN THREAD BELOW THIS POINT |
    // **************************************

    //Initializers
    public void StartServer()
    {
        Debug.Log("Server Mode");
        Thread mainServerThread = new Thread(ServerThread);
        mainServerThread.Start();
    }

    public void StartClient()
    {
        Debug.Log("Client Mode");
        Thread mainClientThread = new Thread(ClientLoop);
        mainClientThread.Start();
    }

    public void Spawn(GameObject prefab, Vector3 position, Quaternion quaternion)
    {
        //Check if prefab is in spawnable list
        for (int i = 0; i < spawnableObjects.Count; i++)
        {
            if (spawnableObjects[i] == prefab)
            {
                //Spawn object, let network identity handle network
                GameObject newObj = Instantiate(prefab, position, quaternion);
                newObj.GetComponent<NetworkIdentity>().ObjectType = i - 1;
                newObj.GetComponent<NetworkIdentity>().IsOriginal();
                //Add to our local objects since we instantiated it
                localObjs.Add(newObj);
                return;
            }
        }
        Debug.LogWarning("Attempted to spawn an object that isn't in the spawnable objects list, ignoring...");
    }

    //Assign ids that are in the pending list
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

    void WorkThroughQueue()
    {
        //Build a list of current gameobjects
        NetworkIdentity[] objs = GameObject.FindObjectsOfType<NetworkIdentity>();

        for (int i = 0; i < pendingWorkItems.Count; i++)
        {
            //Find object
            int indexOfObj = -1;

            for (int j = 0; j < objs.Length; j++)
            {
                //If we can find a obj with the same id, it already exists
                if (objs[j].ObjectID == pendingWorkItems[i].objID)
                {
                    indexOfObj = j;
                    break;
                }
            }

            //If Object Doesn't Exist
            if (indexOfObj < 0)
            {
                //Decode pos
                Vector3 pos = Atlas.StringToVector3(pendingWorkItems[i].objData);
                //Create the object locally
                GameObject objRef = Instantiate(spawnableObjects[pendingWorkItems[i].objType - 1], new Vector3(0,0,0), Quaternion.identity);
                objRef.GetComponent<NetworkIdentity>().OverrideID(pendingWorkItems[i].objID);
            }
            //If Object Exists
            else
            {
                Debug.LogWarning("OwO obj here");
                //Update object
                switch (pendingWorkItems[i].workType)
                {
                    case PendingWorkItem.WorkType.TRANSFORMUPDATE:
                    {
                            //Decode pos
                            Vector3 pos = Atlas.StringToVector3(pendingWorkItems[i].objData);

                            objs[indexOfObj].gameObject.transform.position = pos;

                            break;
                    }

                    default:
                        {
                            Debug.LogWarning("Got a work item that cannot be understood as type of update is unknown");
                            break;
                        }
                }
            }

            //Remove from list
            pendingWorkItems.RemoveAt(i);

        }

    }

    void RemoveDupes()
    {
        //Build a list of current gameobjects
        NetworkIdentity[] objs = GameObject.FindObjectsOfType<NetworkIdentity>();

        List<int> seenIDs = new List<int>();

        bool destroyFlag = false;

        for (int i = 0; i < objs.Length; i++)
        {
            //reset flag
            destroyFlag = false;
            //for all previous ids
            for (int j = 0; j < seenIDs.Count; j++)
            {
                //have we seen this ID?
                if (seenIDs[j] == objs[i].ObjectID)
                {
                    //Remove this dupe
                    destroyFlag = true;
                    break;
                }
            }
            //Add an objects ID to the List if flag is false otherwise remove the dupe
            if (destroyFlag)
            {
                Destroy(objs[i].gameObject);
            }
            else
            {
                seenIDs.Add(objs[i].ObjectID);
            }
        }
    }

    public void Start()
    {
        timeoutThreshold *= 1000; //convert from seconds to milliseconds
    }

    //Loop
    void FixedUpdate()
    {
        //Debug.Log("MY NETID IS: " + Atlas.ID);
        if (Atlas.isServer || (Atlas.isClient && Atlas.networkAuthed))
        {
            AssignPendingIDs();
            WorkThroughQueue();
            RemoveDupes();
        }
    }
}
