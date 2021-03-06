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
    public static float clientHeartbeatTimer = 0.0f;

    private static int lastObjID = -5;

    //Const
    public const string packetTypeSeperator = "::";
    public const string packetSafeSendSeperator = "!!";
    public const string packetClientIDSeperator = "@@";
    public const string packetObjectIDSeperator = "##";
    public const string packetObjectTypeSeperator = "$$";
    public const string packetObjectLocalAuthSeperator = "**";
    public const string packetDataStartMark = "[D_START]";
    public const string packetDataSeperator = "%%";
    public const string packetDataTerminator = "[D_END]";
    public const string packetOwnerSeperator = "&&";
    public const string packetTerminator = "[P_END]";
    public const string packetTargetIDSeperator = "++";
    public const string packetForSrvSeperator = "SRVFLAG";
    public const string commandDataSeperator = "\\\\";

    //Cut and extract a substring using two other substrings as markers
    public static string extractStr(string src, string start, string end)
    {
        string result = "";
        if (start == null)
        {
            result = src.Substring(0, (src.IndexOf(end)));
        }
        else
        {
            result = src.Substring(src.IndexOf(start) + start.Length, (src.IndexOf(end) - (src.IndexOf(start) + start.Length)));
        }
        return result;
    }

    //Convert String To Vector3
    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

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
        COMMAND,
        TARCOMMAND,
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
            heartbeatTimer = 0.0f;
        }

        //Helps identify a timeout
        public void life(){
            heartbeatTimer += Time.unscaledDeltaTime;
        }

        public void resetHeart()
        {
            heartbeatTimer = 0.0f;
        }

        public int ID = -1;
        public AUTHTYPE authState;
        public byte[] lastMessage;
        public IPEndPoint clientEP;
        public float heartbeatTimer = 0.0f;
    };

}
