using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Atlas
{

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
}
