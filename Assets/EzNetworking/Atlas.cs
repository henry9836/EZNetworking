using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Atlas
{

    public static bool isServer = false;
    public static bool isClient = false;
    public static bool networkActive = false;

    public enum PACKETTYPE
    {
        UNASSIGNED,
        HANDSHAKEACKREQ,
        HANDSHAKEACK,
        ACK,
        REQALLGAMEINFO,
        REQOBJECTSTATE,
        WORLDINFO,
        OBJECTSTATE,
        FINALACK,
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

        ClientObject()
        {
            lastHeartbeat = 0.0f;
        }

        //Helps identify a timeout
        public void life(){
             lastHeartbeat += Time.deltaTime;
        }



        float lastHeartbeat = 0.0f;
    };

}
