﻿using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Atlas
{

    public static bool isServer = false;
    public static bool isClient = false;
    public static bool networkActive = false;
    public static bool networkAuthed = false;
    public static int ID = -1;

    private static int lastObjID = -5;

    public static int AssignTempID(int ObjID)
    {
        //But you already have a network ID
        if (ObjID > 0)
        {
            return ObjID;
        }
        //What we already have an ID
        else if (ObjID < -1)
        {
            lastObjID--;
            return lastObjID;
        }
        //Error
        return -1;
    }

    public enum PACKETTYPE
    {
        UNASSIGNED,
        HANDSHAKEACKREQ,
        ACK,
        REQALLGAMEINFO,
        REQOBJECTSTATE,
        REQISTERNEWOBJID,
        NEWOBJID,
        WORLDINFO,
        OBJECTSTATE,
        SPAWNOBJ,
        DISCONNECT,
        BASIC,
        TRANSFORM,
        RIGIDBODY,
    };

    public enum NETWORKOBJTYPE
    {
        BASIC,
        TRANSFORM,
        RIGIDBODY,
    };

    public class ClientObject
    {
        
        public enum AUTHTYPE
        {
            NOHANDSHAKE,
            HANDSHAKE_SUCCEED,
            HANDSHAKE_FAIL,
        };

        public ClientObject(IPEndPoint _clientEP, byte[] _lastMessage)
        {
            lastMessage = _lastMessage;
            clientEP = _clientEP;
            lastHeartbeat = 0.0f;
        }

        //Helps identify a timeout
        public void life(){
             lastHeartbeat += Time.deltaTime;
        }

        public void resetHeart()
        {
            lastHeartbeat = 0.0f;
        }

        public int ID;
        public AUTHTYPE authState;
        public byte[] lastMessage;
        public IPEndPoint clientEP;
        public float lastHeartbeat = 0.0f;
    };

}
