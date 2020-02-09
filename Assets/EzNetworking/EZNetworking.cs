using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CommandHandler))]
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
            TRANSFORMUPDATE,
            RIGIDBODYUPDATE,
        }

        public PendingWorkItem()
        {

        }

        public PendingWorkItem(bool _safeSendFlag, bool _localAuth, int _senderID, int _objID, int _objType, string _objData, int _ownerID, WorkType _workType)
        {
            safeSendFlag = _safeSendFlag;
            senderID = _senderID;
            objID = _objID;
            objType = _objType;
            objData = _objData;
            workType = _workType;
            localAuth = _localAuth;
            ownerID = _ownerID;
        }


        public bool safeSendFlag = false;
        public bool localAuth = false;
        public int senderID = -1;
        public int objID = -1;
        public int objType = -1;
        public string objData = "";
        public int ownerID = -1;
        public WorkType workType = WorkType.UNASSIGNED;


    }

    class PendingCommand
    {
        public PendingCommand(string _data, bool _safeSend, bool srv)
        {
            data = _data;
            safeSend = _safeSend;
            forServerOnly = srv;
        }

        public PendingCommand(string _data, bool _safeSend, int _target, bool srv)
        {
            data = _data;
            target = _target;
            safeSend = _safeSend;
            forServerOnly = srv;
        }


        public CommandHandler.COMMANDTYPE type = CommandHandler.COMMANDTYPE.UNASSIGNED;
        public int target = -1;
        public string data = "";
        public bool safeSend = false;
        public bool forServerOnly = false;
    };

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
    public bool debugPackets = false;

    //Privates
    private List<Atlas.ClientObject> clients = new List<Atlas.ClientObject>();
    private List<PendingID> pendingIDObjects = new List<PendingID>();
    private List<PendingWorkItem> pendingWorkItems = new List<PendingWorkItem>();
    private List<PendingCommand> pendingCommands = new List<PendingCommand>();
    private List<GameObject> localObjs = new List<GameObject>();
    private IPEndPoint listenPoint;
    private UdpClient network;
    private bool timeOutHandShake = false;
    private int NextObjID = 1;
    private int NextNetID = 1;
    private Thread mainNetworkThread;

    public void Disconnect()
    {

        if (Atlas.isServer)
        {
            //Build a list of current gameobjects
            NetworkIdentity[] objs = GameObject.FindObjectsOfType<NetworkIdentity>();

            Atlas.isServer = false;
            Atlas.networkActive = false;
            Atlas.networkAuthed = false;

            mainNetworkThread.Abort();

            for (int i = 0; i < objs.Length; i++)
            {
                Destroy(objs[i]);
            }

        }

        if (Atlas.isClient)
        {

            Atlas.isClient = false;
            Atlas.networkActive = false;
            Atlas.networkAuthed = false;

            mainNetworkThread.Abort();
        }
    }

    public void sendCommand(CommandHandler.COMMANDTYPE cmdType, string data, bool safeSend, bool targetIsServer)
    {
        if (Atlas.isClient && Atlas.networkAuthed && Atlas.networkActive)
        {
            //Build Packet
            //SAFESEND+CLIENTID+D_START+TYPE+DATA+D_END+FORSERVER+P_END
            data = Atlas.packetSafeSendSeperator + (Convert.ToInt32(safeSend)).ToString() + Atlas.packetClientIDSeperator + Atlas.ID.ToString() + Atlas.packetDataStartMark + ((int)cmdType).ToString() + Atlas.packetDataSeperator + data + Atlas.packetDataTerminator + Atlas.packetForSrvSeperator + (Convert.ToInt32(targetIsServer)).ToString() + Atlas.packetTerminator;

            Debug.LogWarning("Sending: " + data);

            //Send Packet
            if (safeSend)
            {
                SafeSend(Atlas.PACKETTYPE.COMMAND, Encoding.ASCII.GetBytes(data), null, true);
            }
            else
            {
                Send(Atlas.PACKETTYPE.COMMAND, Encoding.ASCII.GetBytes(data), null, true);
            }
        }
        else if (Atlas.isServer)
        {
            //Build Pending Command
            //TYPE+DATA
            data = ((int)cmdType).ToString() + Atlas.packetDataSeperator + data;
            pendingCommands.Add(new PendingCommand(data, safeSend, targetIsServer));
        }
    }

    public void sendCommand(CommandHandler.COMMANDTYPE cmdType, string data, int targetID, bool safeSend, bool targetIsServer)
    {
        
        if (Atlas.isClient && Atlas.networkAuthed && Atlas.networkActive)
        {
            //Build Packet
            //SAFESEND+CLIENTID+TARGETID+D_START+TYPE+DATA+D_END+FORSERVER+P_END
            data = Atlas.packetSafeSendSeperator + (Convert.ToInt32(safeSend)).ToString() + Atlas.packetClientIDSeperator + Atlas.ID.ToString() + Atlas.packetTargetIDSeperator + targetID.ToString() + Atlas.packetDataStartMark + ((int)cmdType).ToString() + Atlas.packetDataSeperator + data + Atlas.packetDataTerminator + Atlas.packetForSrvSeperator + (Convert.ToInt32(targetIsServer)).ToString() + Atlas.packetTerminator;

            Debug.LogWarning("Sending: " + data);

            //Send Packet
            if (safeSend)
            {
                SafeSend(Atlas.PACKETTYPE.TARCOMMAND, Encoding.ASCII.GetBytes(data), null, true);
            }
            else
            {
                Send(Atlas.PACKETTYPE.TARCOMMAND, Encoding.ASCII.GetBytes(data), null, true);
            }
        }
        else if (Atlas.isServer)
        {
            //Build Pending Command
            //TYPE+DATA
            data = ((int)cmdType).ToString() + Atlas.packetDataSeperator + data;
            pendingCommands.Add(new PendingCommand(data, safeSend, targetID, targetIsServer));
        }
    }

    //Fix Missing Object Type
    public int FixObjectID(GameObject obj)
    {
        for (int i = 0; i < spawnableObjects.Count; i++)
        {
            if (spawnableObjects[i] == obj)
            {
                return i;
            }
        }

        Debug.LogWarning("Cannot Fix Object Type ID for gameobject: " + obj.name + " try assigning the gameobject as a spawnable prefab in the Network Interface");
        return -1;
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
            int returnVal = NextObjID;
            NextObjID++;
            return returnVal;
        }
        //Request ID From Server
        else
        {
            pendingIDObjects.Add(new PendingID(currentID, -1));
            pendingIDObjects[pendingIDObjects.Count - 1].refgameobject = obj;
            Send(Atlas.PACKETTYPE.REQISTERNEWOBJID, Encoding.ASCII.GetBytes(obj.GetComponent<NetworkIdentity>().ObjectID.ToString()), listenPoint, false);

            return -1;
        }
    }

    //Sends a packet
    public void Send(Atlas.PACKETTYPE type, byte[] data, IPEndPoint ep, bool sendToAll)
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
            //Send to one client
            if (!sendToAll)
            {
                network.Send(packet, packet.Length, ep);
            }
            else
            {
                //Send to everyone
                for (int i = 0; i < clients.Count; i++)
                {
                    network.Send(packet, packet.Length, clients[i].clientEP);
                }
            }
        }
        else
        {
            Debug.LogWarning("Atlas doesn't have a mode selected");
        }
    }

    //Sends a packet multiple times
    public void SafeSend(Atlas.PACKETTYPE type, byte[] data, IPEndPoint ep, bool sendToAll)
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
            Send(Atlas.PACKETTYPE.HANDSHAKEACKREQ, Encoding.ASCII.GetBytes("Hello Server!"), listenPoint, false);

            //Server Reply
            while (!Atlas.networkAuthed && !timeOutHandShake)
            {
                //Wait for either the timeout or a ACK packet
            }

            //Handshake has not reached timeout
            if (!timeOutHandShake)
            {
                timeoutThread.Abort();
                Debug.Log("Handshake Completed");
                
                //Request Game Infomation From Server
                SafeSend(Atlas.PACKETTYPE.REQALLGAMEINFO, Encoding.ASCII.GetBytes("SENDGAMEINFO"), listenPoint, false);

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
        if (!Atlas.networkActive) {
            Debug.LogWarning("Attempted to process data while network was not active, ignoring");
        }

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

        int cutPosition = dataStr.IndexOf(Atlas.packetTypeSeperator);

        if (cutPosition != -1)
        {
            int type = int.Parse(dataStr.Substring(0, cutPosition));
            string infoStr = dataStr.Substring(cutPosition+Atlas.packetTypeSeperator.Length);
            if (debugPackets)
            {
                Debug.Log("Infomation: type{" + type.ToString() + "} infomation: " + infoStr);
            }

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
                        client.ID = NextNetID;
                        NextNetID++;
                        SafeSend(Atlas.PACKETTYPE.ACK, Encoding.ASCII.GetBytes(ACK), client.clientEP, false);
                        client.authState = Atlas.ClientObject.AUTHTYPE.HANDSHAKE_SUCCEED;
                        break;
                    }
                case (int)Atlas.PACKETTYPE.REQISTERNEWOBJID:
                    {
                        //Get Temporary ID
                        int oldID = int.Parse(infoStr);
                        int newID = AssignID(null, -1);
                        Debug.Log("OLD ID: " + oldID.ToString() + " NEW ID: " + newID.ToString());
                        SafeSend(Atlas.PACKETTYPE.NEWOBJID, Encoding.ASCII.GetBytes(oldID.ToString() +"$" + newID.ToString()), client.clientEP, false);
                        break;
                    }
                case (int)Atlas.PACKETTYPE.TRANSFORM:
                    {
                        //Decode Packet
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetObjectIDSeperator));
                        int objID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectIDSeperator, Atlas.packetObjectLocalAuthSeperator));
                        bool localAuth = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectLocalAuthSeperator, Atlas.packetObjectTypeSeperator)) != 0;
                        int objType = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectTypeSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);
                        int objOwnerID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetOwnerSeperator, Atlas.packetTerminator));

                        //Debugging
                        Debug.Log("SENDER ID: " + senderID.ToString() + " OWNER ID: " + objOwnerID.ToString() + " SAFE SEND: " +  safeSendFlag + " AUTH: " + localAuth + " OBJID: " + objID + " OBJTYPE: " + objType + " data["+objData+"]");

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }
                        //If safesend tell client we got it

                        //Queue Work
                        pendingWorkItems.Add(new PendingWorkItem(safeSendFlag, localAuth, senderID, objID, objType, objData, objOwnerID, PendingWorkItem.WorkType.TRANSFORMUPDATE));

                        break;
                    }
                case (int)Atlas.PACKETTYPE.RIGIDBODY:
                    {
                        //Decode Packet
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetObjectIDSeperator));
                        int objID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectIDSeperator, Atlas.packetObjectLocalAuthSeperator));
                        bool localAuth = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectLocalAuthSeperator, Atlas.packetObjectTypeSeperator)) != 0;
                        int objType = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectTypeSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);
                        int objOwnerID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetOwnerSeperator, Atlas.packetTerminator));

                        //Debugging
                        Debug.Log("SENDER ID: " + senderID.ToString() + " OWNER ID: " + objOwnerID.ToString() + " SAFE SEND: " + safeSendFlag + " AUTH: " + localAuth + " OBJID: " + objID + " OBJTYPE: " + objType + " data[" + objData + "]");

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        //If safesend tell client we got it

                        //Queue Work
                        pendingWorkItems.Add(new PendingWorkItem(safeSendFlag, localAuth, senderID, objID, objType, objData, objOwnerID, PendingWorkItem.WorkType.RIGIDBODYUPDATE));


                        break;
                    }
                case (int)Atlas.PACKETTYPE.COMMAND:
                    {
                        //Decode Packet
                        //SAFESEND+CLIENTID+D_START+TYPE+DATA+D_END+P_END
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);
                        bool srvFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetForSrvSeperator, Atlas.packetTerminator)) != 0;

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        //Queue Work
                        pendingCommands.Add(new PendingCommand(objData, safeSendFlag, srvFlag));

                        break;
                    }
                case (int)Atlas.PACKETTYPE.TARCOMMAND:
                    {
                        //Decode Packet
                        //SAFESEND+CLIENTID+TARGETID+D_START+TYPE+DATA+D_END+P_END
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetTargetIDSeperator));
                        int targetID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetTargetIDSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);
                        bool srvFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetForSrvSeperator, Atlas.packetTerminator)) != 0;

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        //Queue Work
                        pendingCommands.Add(new PendingCommand(objData, safeSendFlag, targetID, srvFlag));

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

        if (!Atlas.networkActive)
        {
            Debug.LogWarning("Attempted to process data while network was not active, ignoring");
        }

        byte[] data = state as byte[];

        //Reset timeout
        Atlas.clientHeartbeatTimer = 0.0f;

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

                        if (Atlas.ID > 0)
                        {
                            Atlas.networkAuthed = true;
                            Debug.Log("Client ID: " + Atlas.ID.ToString());
                        }

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
                        for (int i = 0; i < pendingIDObjects.Count; i++)
                        {
                            if (pendingIDObjects[i].oldID == oldID)
                            {
                                pendingIDObjects[i].newID = newID;
                                return;
                            }
                        }
                        break;
                    }
                case (int)Atlas.PACKETTYPE.TRANSFORM:
                    {
                        //Decode Packet
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetObjectIDSeperator));
                        int objID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectIDSeperator, Atlas.packetObjectLocalAuthSeperator));
                        bool localAuth = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectLocalAuthSeperator, Atlas.packetObjectTypeSeperator)) != 0;
                        int objType = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectTypeSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator); 
                        int objOwnerID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetOwnerSeperator, Atlas.packetTerminator));

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        Debug.Log("SENDER ID: " + senderID.ToString() + " OWNER ID: " + objOwnerID.ToString() + " SAFE SEND: " + safeSendFlag + " AUTH: " + localAuth + " OBJID: " + objID + " OBJTYPE: " + objType + " data[" + objData + "]");


                        //If safesend tell server we got it

                        //Queue Work
                        pendingWorkItems.Add(new PendingWorkItem(safeSendFlag, localAuth, senderID, objID, objType, objData, objOwnerID, PendingWorkItem.WorkType.TRANSFORMUPDATE));


                        break;
                    }
                case (int)Atlas.PACKETTYPE.RIGIDBODY:
                    {
                        //Decode Packet
                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetClientIDSeperator)) != 0;
                        int senderID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetClientIDSeperator, Atlas.packetObjectIDSeperator));
                        int objID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectIDSeperator, Atlas.packetObjectLocalAuthSeperator));
                        bool localAuth = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectLocalAuthSeperator, Atlas.packetObjectTypeSeperator)) != 0;
                        int objType = int.Parse(Atlas.extractStr(infoStr, Atlas.packetObjectTypeSeperator, Atlas.packetDataStartMark));
                        string objData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);
                        int objOwnerID = int.Parse(Atlas.extractStr(infoStr, Atlas.packetOwnerSeperator, Atlas.packetTerminator));

                        //If it is our ID ignore
                        if (Atlas.ID == senderID)
                        {
                            return;
                        }

                        //If safesend tell server we got it
                        Debug.Log("SENDER ID: " + senderID.ToString() + " OWNER ID: " + objOwnerID.ToString() + " SAFE SEND: " + safeSendFlag + " AUTH: " + localAuth + " OBJID: " + objID + " OBJTYPE: " + objType + " data[" + objData + "]");

                        //Queue Work
                        pendingWorkItems.Add(new PendingWorkItem(safeSendFlag, localAuth, senderID, objID, objType, objData, objOwnerID, PendingWorkItem.WorkType.RIGIDBODYUPDATE));


                        break;
                    }
                case (int)Atlas.PACKETTYPE.COMMAND:
                case (int)Atlas.PACKETTYPE.TARCOMMAND:
                    {

                        //Decode Packet
                        string cmdData = Atlas.extractStr(infoStr, Atlas.packetDataStartMark, Atlas.packetDataTerminator);

                        bool safeSendFlag = int.Parse(Atlas.extractStr(infoStr, Atlas.packetSafeSendSeperator, Atlas.packetDataStartMark)) != 0;

                        pendingCommands.Add(new PendingCommand(cmdData, safeSendFlag, false));


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
        //Thread clientR = new Thread(clientMainReieveThread);
        mainNetworkThread = new Thread(clientMainReieveThread);
        mainNetworkThread.Start();

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
        //Thread serverR = new Thread(serverMainReceiveThread);
        mainNetworkThread = new Thread(serverMainReceiveThread);
        mainNetworkThread.Start();
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

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion quaternion)
    {
        //Check if prefab is in spawnable list
        for (int i = 0; i < spawnableObjects.Count; i++)
        {
            if (spawnableObjects[i] == prefab)
            {
                //Spawn object, let network identity handle network
                GameObject newObj = Instantiate(prefab, position, quaternion);
                newObj.GetComponent<NetworkIdentity>().ObjectType = i;
                newObj.GetComponent<NetworkIdentity>().IsOriginal();
                newObj.GetComponent<NetworkIdentity>().ownerID = Atlas.ID;
                //Add to our local objects since we instantiated it
                localObjs.Add(newObj);
                return newObj;
            }
        }
        Debug.LogWarning("Attempted to spawn an object that isn't in the spawnable objects list, ignoring...");
        return null;
    }

    //Assign ids that are in the pending list
    void AssignPendingIDs()
    {
        for (int i = 0; i < pendingIDObjects.Count; i++)
        {
            if (pendingIDObjects[i].newID > 0)
            {
                //Assign new Network ID
                if (pendingIDObjects[i].refgameobject != null)
                {
                    pendingIDObjects[i].refgameobject.GetComponent<NetworkIdentity>().updateID(pendingIDObjects[i].newID);
                }
                //Remove Pending ID
                pendingIDObjects.RemoveAt(i);
            }
        }
    }

    void WorkThroughObjQueue(NetworkIdentity[] objs)
    {
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

                switch (pendingWorkItems[i].workType)
                {
                    case PendingWorkItem.WorkType.TRANSFORMUPDATE:
                    {
                            //Extract Raw Strings
                            string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);
                            //Decode pos
                            Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                            Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                            Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));
                            //Create the object locally
                            GameObject objRef = Instantiate(spawnableObjects[pendingWorkItems[i].objType], pos, rot);
                            objRef.GetComponent<NetworkIdentity>().OverrideID(pendingWorkItems[i].objID);
                            objRef.GetComponent<NetworkIdentity>().ObjectType = pendingWorkItems[i].objType;
                            objRef.GetComponent<NetworkIdentity>().ownerID = pendingWorkItems[i].ownerID;
                            break;
                    }
                    case PendingWorkItem.WorkType.RIGIDBODYUPDATE:
                        {
                            //Extract Raw Strings
                            string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);
                            //Decode pos
                            Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                            Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                            Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));
                            Vector3 velo = Atlas.StringToVector3(dataStrArray[3]);
                            Vector3 aVelo = Atlas.StringToVector3(dataStrArray[4]);
                            //Create the object locally and assign vars
                            GameObject objRef = Instantiate(spawnableObjects[pendingWorkItems[i].objType], pos, rot);
                            objRef.GetComponent<NetworkIdentity>().OverrideID(pendingWorkItems[i].objID);
                            objRef.GetComponent<Rigidbody>().velocity = velo;
                            objRef.GetComponent<Rigidbody>().angularVelocity = aVelo;
                            objRef.GetComponent<NetworkIdentity>().ObjectType = pendingWorkItems[i].objType;
                            objRef.GetComponent<NetworkIdentity>().ownerID = pendingWorkItems[i].ownerID;
                            break;
                        }
                    default:
                        {
                            Debug.LogWarning("Unknown Work Type {"+ pendingWorkItems[i].workType.ToString()+"}");
                            break;
                        }
                }
                
            }
            //If Object Exists
            else
            {
                //Server logic
                if (Atlas.isServer)
                {
                    //Check if packet is from owner if local is on
                    if ((pendingWorkItems[i].senderID == objs[indexOfObj].ownerID) && objs[indexOfObj].localPlayerAuthority)
                    {
                        //Update object
                        switch (pendingWorkItems[i].workType)
                        {
                            case PendingWorkItem.WorkType.TRANSFORMUPDATE:
                                {
                                    //Extract Raw Strings
                                    string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);

                                    Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                                    Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                                    Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));

                                    if (objs[indexOfObj].smoothMove)
                                    {
                                        objs[indexOfObj].UpdateTargets(pos, scale, rot);
                                    }
                                    else
                                    {
                                        objs[indexOfObj].gameObject.transform.position = pos;
                                        objs[indexOfObj].gameObject.transform.rotation = rot;
                                        objs[indexOfObj].gameObject.transform.localScale = scale;
                                    }

                                    break;
                                }
                            case PendingWorkItem.WorkType.RIGIDBODYUPDATE:
                                {
                                    //Extract Raw Strings
                                    string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);

                                    Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                                    Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                                    Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));
                                    Vector3 velo = Atlas.StringToVector3(dataStrArray[3]);
                                    Vector3 aVelo = Atlas.StringToVector3(dataStrArray[4]);

                                    if (objs[indexOfObj].smoothMove)
                                    {
                                        objs[indexOfObj].UpdateTargets(pos, scale, rot, velo, aVelo);
                                    }
                                    else
                                    {
                                        objs[indexOfObj].gameObject.transform.position = pos;
                                        objs[indexOfObj].gameObject.transform.rotation = rot;
                                        objs[indexOfObj].gameObject.transform.localScale = scale;
                                        objs[indexOfObj].GetComponent<Rigidbody>().velocity = velo;
                                        objs[indexOfObj].GetComponent<Rigidbody>().angularVelocity = aVelo;
                                    }

                                    break;
                                }
                            default:
                                {
                                    Debug.LogWarning("Got a work item that cannot be understood as type of update is unknown");
                                    break;
                                }
                        }
                    }
                    //Someone is trying to change stuff they shouldn't or the obj doesn't accept changes from clients
                    else
                    {
                        Debug.LogWarning("[" + pendingWorkItems[i].ownerID.ToString() + "] is trying to change stuff they shouldn't or the obj doesn't accept changes from clients on object: " + objs[indexOfObj].name);
                    }
                }
                //Client Logic
                else
                {
                    //Update object
                    switch (pendingWorkItems[i].workType)
                    {
                        case PendingWorkItem.WorkType.TRANSFORMUPDATE:
                            {
                                //Move we don't have auth over obj
                                if (!objs[i].localPlayerAuthority || (objs[i].localPlayerAuthority && (objs[i].ownerID != Atlas.ID)))
                                {
                                    //Extract Raw Strings
                                    string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);

                                    Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                                    Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                                    Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));

                                    if (objs[indexOfObj].smoothMove)
                                    {
                                        objs[indexOfObj].UpdateTargets(pos, scale, rot);
                                    }
                                    else
                                    {
                                        objs[indexOfObj].gameObject.transform.position = pos;
                                        objs[indexOfObj].gameObject.transform.rotation = rot;
                                        objs[indexOfObj].gameObject.transform.localScale = scale;
                                    }
                                }
                                break;
                            }
                        case PendingWorkItem.WorkType.RIGIDBODYUPDATE:
                            {
                                //Extract Raw Strings
                                string[] dataStrArray = pendingWorkItems[i].objData.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);

                                Vector3 pos = Atlas.StringToVector3(dataStrArray[0]);
                                Vector3 scale = Atlas.StringToVector3(dataStrArray[2]);
                                Quaternion rot = Quaternion.Euler(Atlas.StringToVector3(dataStrArray[1]));
                                Vector3 velo = Atlas.StringToVector3(dataStrArray[3]);
                                Vector3 aVelo = Atlas.StringToVector3(dataStrArray[4]);

                                if (objs[indexOfObj].smoothMove)
                                {
                                    objs[indexOfObj].UpdateTargets(pos, scale, rot, velo, aVelo);
                                }
                                else
                                {
                                    objs[indexOfObj].gameObject.transform.position = pos;
                                    objs[indexOfObj].gameObject.transform.rotation = rot;
                                    objs[indexOfObj].gameObject.transform.localScale = scale;
                                    objs[indexOfObj].GetComponent<Rigidbody>().velocity = velo;
                                    objs[indexOfObj].GetComponent<Rigidbody>().angularVelocity = aVelo;
                                }

                                break;
                            }
                        default:
                            {
                                Debug.LogWarning("Got a work item that cannot be understood as type of update is unknown");
                                break;
                            }
                    }
                }

            }

            //Remove from list
            pendingWorkItems.RemoveAt(i);

        }

    }

    void WorkThroughCmdQueue()
    {
        for (int i = 0; i < pendingCommands.Count; i++)
        {
            //Get Type
            //Extract Raw Strings
            string[] dataStrArray = pendingCommands[i].data.Split(new string[] { Atlas.packetDataSeperator }, StringSplitOptions.None);

            CommandHandler.COMMANDTYPE type = (CommandHandler.COMMANDTYPE)int.Parse(dataStrArray[0]);

            //Server Logic
            if (Atlas.isServer)
            {
                //If this is only meant for the server execute locally on server
                if (pendingCommands[i].forServerOnly)
                {
                    if (Atlas.isServer)
                    {
                        GetComponent<CommandHandler>().runCommand(type, dataStrArray);
                    }
                }
                //pass it onto target (and not zero as that is the server)
                else if (pendingCommands[i].target > 0)
                {
                    //Find and send to client
                    for (int j = 0; j < clients.Count; j++)
                    {
                        //Found match
                        if (clients[j].ID == pendingCommands[i].target)
                        {
                            //Build packet
                            string packet = Atlas.packetSafeSendSeperator + (Convert.ToInt32(pendingCommands[i].safeSend)).ToString() + Atlas.packetDataStartMark + pendingCommands[i].data + Atlas.packetDataTerminator + Atlas.packetTerminator;


                            if (pendingCommands[i].safeSend)
                            {
                                SafeSend(Atlas.PACKETTYPE.TARCOMMAND, Encoding.ASCII.GetBytes(packet), clients[j].clientEP, false);
                            }
                            else
                            {
                                Send(Atlas.PACKETTYPE.TARCOMMAND, Encoding.ASCII.GetBytes(packet), clients[j].clientEP, false);
                            }
                            break;
                        }
                    }
                }
                //execute command and pass it onto all other clients
                else
                {
                    GetComponent<CommandHandler>().runCommand(type, dataStrArray);

                    //Build packet
                    string packet = Atlas.packetSafeSendSeperator + (Convert.ToInt32(pendingCommands[i].safeSend)).ToString() + Atlas.packetDataStartMark + pendingCommands[i].data + Atlas.packetDataTerminator + Atlas.packetTerminator;

                    if (pendingCommands[i].safeSend)
                    {
                        SafeSend(Atlas.PACKETTYPE.COMMAND, Encoding.ASCII.GetBytes(packet), null, true);
                    }
                    else
                    {
                        Send(Atlas.PACKETTYPE.COMMAND, Encoding.ASCII.GetBytes(packet), null, true);
                    }
                }
            }
            //Client Logic
            else
            {
                Debug.LogError("Running command: " + dataStrArray[0]);
                GetComponent<CommandHandler>().runCommand(type, dataStrArray);
            }

            //Remove from list
            pendingCommands.RemoveAt(i);
        }
    }

    void RemoveDupes(NetworkIdentity[] objs)
    {

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

    void Heartbeat(NetworkIdentity[] objs)
    {
        if (Atlas.isServer)
        {
            //Server Timeout Logic
            for (int i = 0; i < clients.Count; i++)
            {
                Debug.LogWarning("HB LP");
                //Only timeout other people
                if (clients[i].ID != Atlas.ID)
                {
                    clients[i].life();

                    Debug.LogWarning("HB[" + clients[i].ID + "] = " + clients[i].heartbeatTimer + " Threshold is: " + timeoutThreshold);

                    //If the client has timedout
                    if (clients[i].heartbeatTimer > (timeoutThreshold/1000))
                    {
                        Debug.LogWarning("WE HAVE HIT THRESHOLD!");

                        //Destory all objs owned by player
                        for (int j = 0; j < objs.Length; j++)
                        {
                            if (objs[j].ownerID == clients[i].ID)
                            {
                                Destroy(objs[j].gameObject);
                            }
                        }
                        //Remove Client Data
                        clients.Remove(clients[i]);
                        Debug.LogWarning("ALL DONE!");
                    }
                }
            }
        }
        else if (Atlas.isClient){
            //Client Timeout Logic
            Atlas.clientHeartbeatTimer += Time.unscaledDeltaTime;

            if (Atlas.clientHeartbeatTimer > (timeoutThreshold / 1000))
            {
                //destory all objs and disconnect

                for (int i = 0; i < objs.Length; i++)
                {
                    Destroy(objs[i].gameObject);
                }

                Disconnect();
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
        //Valid Network and Setup
        if (Atlas.isServer || (Atlas.isClient && Atlas.networkAuthed))
        {
            //Build a list of current gameobjects
            NetworkIdentity[] objs = GameObject.FindObjectsOfType<NetworkIdentity>();

            AssignPendingIDs();
            WorkThroughObjQueue(objs);
            RemoveDupes(objs);
            WorkThroughCmdQueue();
            Heartbeat(objs);
        }
    }
}
