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
    public bool smoothMove = true;
    public bool smoothMoveMatchesSendRate = true;
    public float smoothMoveDuration = 0.2f;

    private EZNetworking networkController;
    private float networkUpdateRate;
    private float currentTime = 0.0f;
    private int overrideID = -1;
    private bool isOriginal = false;
    private float sendRegDelay = 0.5f;
    private Vector3 targetPos = Vector3.zero;
    private Vector3 oldPos = Vector3.zero;
    private Quaternion targetRot = Quaternion.identity;
    private Quaternion oldRot = Quaternion.identity;
    private Vector3 targetScale = Vector3.zero;
    private Vector3 oldScale = Vector3.zero;
    private Vector3 oldVelo = Vector3.zero;
    private Vector3 targetVelo = Vector3.zero;
    private Vector3 oldAVelo = Vector3.zero;
    private Vector3 targetAVelo = Vector3.zero;
    private Rigidbody rb = null;
    private float t = 0.0f;

    public void updateID(int newID)
    {
        ObjectID = newID;
    }

    public void OverrideID(int newID)
    {
        overrideID = newID;
    }

    public void UpdateTargets(Vector3 pos, Vector3 scale, Quaternion rot)
    {
        targetPos = pos;
        oldPos = transform.position;
        oldScale = transform.localScale;
        targetScale = scale;
        oldRot = transform.rotation;
        targetRot = rot;

        t = 0.0f;
    }

    public void UpdateTargets(Vector3 pos, Vector3 scale, Quaternion rot, Vector3 velo, Vector3 aVelo)
    {
        targetPos = pos;
        oldPos = transform.position;
        oldScale = transform.localScale;
        targetScale = scale;
        oldRot = transform.rotation;
        targetRot = rot;
        oldVelo = rb.velocity;
        targetVelo = velo;
        oldAVelo = rb.angularVelocity;
        targetAVelo = aVelo;

        t = 0.0f;
    }

    void BasicLogic()
    {

    }

    void TransformLogic()
    {
        //Pack infomation
        //SAFESEND+CLIENTID+OBJID+LOCALPLAYERAUTH+OBJTYPE+D_START+DATA+D_END+OWNERID+P_END
        string data = Atlas.packetSafeSendSeperator + (Convert.ToInt32(safeSend)).ToString() + Atlas.packetClientIDSeperator + Atlas.ID.ToString() + Atlas.packetObjectIDSeperator + ObjectID.ToString() + Atlas.packetObjectLocalAuthSeperator + (Convert.ToInt32(localPlayerAuthority)).ToString() + Atlas.packetObjectTypeSeperator + ObjectType.ToString() + Atlas.packetDataStartMark + transform.position.ToString() + Atlas.packetDataSeperator + transform.rotation.eulerAngles.ToString() + Atlas.packetDataSeperator + transform.localScale.ToString() + Atlas.packetDataTerminator + Atlas.packetOwnerSeperator + ownerID.ToString() + Atlas.packetTerminator;

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
        //Pack infomation
        //SAFESEND+CLIENTID+OBJID+LOCALPLAYERAUTH+OBJTYPE+D_START+DATA+D_END+OWNERID+P_END
        string data = Atlas.packetSafeSendSeperator + (Convert.ToInt32(safeSend)).ToString() + Atlas.packetClientIDSeperator + Atlas.ID.ToString() + Atlas.packetObjectIDSeperator + ObjectID.ToString() + Atlas.packetObjectLocalAuthSeperator + (Convert.ToInt32(localPlayerAuthority)).ToString() + Atlas.packetObjectTypeSeperator + ObjectType.ToString() + Atlas.packetDataStartMark + transform.position.ToString() + Atlas.packetDataSeperator + transform.rotation.eulerAngles.ToString() + Atlas.packetDataSeperator + transform.localScale.ToString() + Atlas.packetDataSeperator + rb.velocity.ToString() + Atlas.packetDataSeperator + rb.angularVelocity.ToString() + Atlas.packetDataTerminator + Atlas.packetOwnerSeperator + ownerID.ToString() + Atlas.packetTerminator;

        //Encode for network
        byte[] networkData = Encoding.ASCII.GetBytes(data);

        //Send infomation
        if (safeSend)
        {
            networkController.SafeSend(Atlas.PACKETTYPE.RIGIDBODY, networkData, null, true);
        }
        else
        {
            networkController.Send(Atlas.PACKETTYPE.RIGIDBODY, networkData, null, true);
        }
    }

    public void IsOriginal()
    {
        isOriginal = true;
    }

    void Start()
    {
        if (smoothMoveMatchesSendRate)
        {
            smoothMoveDuration = sendDelay;
        }

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

        //Get additional components that we need for our type
        switch (type)
        {
            case Atlas.NETWORKOBJTYPE.RIGIDBODY:
                {
                    rb = GetComponent<Rigidbody>();
                    break;
                }
            default:
                break;
        }
    }

    //Update Loop Tied to Our Fixed Update Info
    void FixedUpdate()
    {
        //Lerp Movement
        if (smoothMove && ((type == Atlas.NETWORKOBJTYPE.TRANSFORM) || (type == Atlas.NETWORKOBJTYPE.RIGIDBODY)) && ((Atlas.isClient && ((!localPlayerAuthority && ownerID == Atlas.ID) || (localPlayerAuthority && ownerID != Atlas.ID))) || (Atlas.isServer && ownerID != Atlas.ID)))
        {
            t += Time.deltaTime / smoothMoveDuration;
            transform.position = Vector3.Lerp(oldPos, targetPos, t);
            transform.localScale = Vector3.Lerp(oldScale, targetScale, t);
            transform.rotation = Quaternion.Lerp(oldRot, targetRot, t);

            switch (type)
            {
                case Atlas.NETWORKOBJTYPE.RIGIDBODY:
                    {
                        rb.velocity = Vector3.Lerp(oldVelo, targetVelo, t);
                        rb.angularVelocity = Vector3.Lerp(oldAVelo, targetAVelo, t);
                        break;
                    }
                default:
                    break;
            }

        }

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
                        if (ObjectType > -1)
                        {
                            //Check if we know who owns us
                            if (ownerID >= 0)
                            {
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
                            Debug.LogWarning("No Object Type ID On Object: " + gameObject.name);
                            ObjectID = networkController.GetComponent<EZNetworking>().FixObjectID(gameObject);
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
