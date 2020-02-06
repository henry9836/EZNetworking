using System;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkIdentity : MonoBehaviour
{

    [Range(0.0f, 3.0f)]
    public float sendDelay = 0.2f;
    public Atlas.NETWORKOBJTYPE type;
    public bool safeSend = false;
    public bool serverOnlyObject = false;
    public bool localPlayerAuthority = false;
    public int ObjectID = -1;
    public int ObjectType = -1;
    public int ownerID = -1;

    private EZNetworking networkController;
    private float networkUpdateRate;
    private float currentTime = 0.0f;
    private int overrideID = -1;
    private bool isOriginal = false;
    private float sendRegDelay = 0.5f;

    public void updateID(int newID)
    {
        ObjectID = newID;
    }

    public void OverrideID(int newID)
    {
        overrideID = newID;
    }

    void BasicLogic()
    {

    }

    void TransformLogic()
    {
        //Pack infomation
        //SAFESEND+CLIENTID+OBJID+LOCALPLAYERAUTH+OBJTYPE+DATA+OWNERID
        string data = Atlas.packetSafeSendSeperator + (Convert.ToInt32(safeSend)).ToString() + Atlas.packetClientIDSeperator + Atlas.ID.ToString() + Atlas.packetObjectIDSeperator + ObjectID.ToString() + Atlas.packetObjectLocalAuthSeperator + (Convert.ToInt32(localPlayerAuthority)).ToString() + Atlas.packetObjectTypeSeperator + ObjectType.ToString() + Atlas.packetObjectDataSeperator + transform.position.ToString() + Atlas.packetOwnerSeperator + ownerID.ToString() + Atlas.packetTerminator;

        //Encode for network
        byte[] networkData = Encoding.ASCII.GetBytes(data);

        //Send infomation
        if (safeSend)
        {
            networkController.SafeSend(Atlas.PACKETTYPE.TRANSFORM, networkData, null, true);
        }
        else
        {
            networkController.Send(Atlas.PACKETTYPE.TRANSFORM, networkData, null, true);
        }
    }

    void RigidbodyLogic()
    {

    }

    public void IsOriginal()
    {
        isOriginal = true;
    }

    void Start()
    {
        //Give ourselves a temporary ID
        ObjectID = Atlas.AssignTempID(ObjectID);

        //Find the network interface
        networkController = FindObjectOfType<EZNetworking>();

        //Get our ID
        if (networkController != null)
        {
            ObjectID = networkController.AssignID(this.gameObject, ObjectID);
        }

        //Get the rate at which we should send updates to the network
        networkUpdateRate = sendDelay;

        //Send our info on spawn
        currentTime = networkUpdateRate;
    }

    //Update Loop Tied to Our Fixed Update Info
    void FixedUpdate()
    {
        //override ID
        if (overrideID >= 0)
        {
            ObjectID = overrideID;
        }

        //update timer
        currentTime += Time.deltaTime;

        //If we have reached our threshold to send our info
        if (currentTime >= networkUpdateRate)
        {
            //Sanity Checks
            if (networkController != null)
            {
                //If we are connected to a network
                if (Atlas.networkActive && Atlas.networkAuthed)
                {
                    //Less than 0 means no valid network ID
                    if (ObjectID > 0)
                    {
                        //Check if we know who owns us
                        if (ownerID >= 0) {
                            //Check for invalid or strange configs
                            if ((Atlas.isClient && localPlayerAuthority && isOriginal) || (Atlas.isServer && !serverOnlyObject))
                            {
                                //Carry out an action based on our type
                                switch (type)
                                {
                                    case Atlas.NETWORKOBJTYPE.BASIC:
                                        {
                                            BasicLogic();
                                            break;
                                        }
                                    case Atlas.NETWORKOBJTYPE.TRANSFORM:
                                        {
                                            TransformLogic();
                                            break;
                                        }
                                    case Atlas.NETWORKOBJTYPE.RIGIDBODY:
                                        {
                                            RigidbodyLogic();
                                            break;
                                        }
                                    default:
                                        {
                                            Debug.LogWarning("Could not determine type for Network Identity");
                                            break;
                                        }
                                }

                                //reset timer
                                currentTime = 0.0f;
                            }
                            else
                            {
                                Debug.LogWarning("Network Identity has incorrect bool config");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No Network Owner On Object: " + gameObject.name);
                        }
                    }
                    else
                    {
                        if (currentTime >= sendRegDelay)
                        {
                            Debug.LogError("Invalid Network ID {" + ObjectID + "}, retrying...");
                            if (Atlas.isClient)
                            {
                                networkController.AssignID(this.gameObject, ObjectID);
                            }
                            else
                            {
                                ObjectID = networkController.AssignID(this.gameObject, ObjectID);
                            }

                            //reset timer
                            currentTime = 0.0f;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("A Network Identity Is Active In The Scene But There Is No Network Currently Active Or We Have Not Completed A Handshake");
                }
            }
            else
            {
                Debug.LogError("Could not find network interface");
            }
        }
    } 
}
