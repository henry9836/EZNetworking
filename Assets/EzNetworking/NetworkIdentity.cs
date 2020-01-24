using System;
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

    private EZNetworking networkController;
    private float networkUpdateRate;
    private float currentTime = 0.0f;
    

    public void updateID(int newID)
    {
        ObjectID = newID;
    }

    void BasicLogic()
    {

    }

    void TransformLogic()
    {
        //Get infomation
        string data = ObjectID.ToString() + "$" + transform.position.ToString();

        //Encode for network
        byte[] networkData = Encoding.ASCII.GetBytes(data);

        //Send infomation
        if (safeSend)
        {
            //networkController.SafeSend(Atlas.PACKETTYPE.TRANSFORM, networkData);
        }
        else {
            //networkController.Send(Atlas.PACKETTYPE.TRANSFORM, networkData);
        }
    }

    void RigidbodyLogic()
    {

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
        //update timer
        currentTime += Time.deltaTime;

        //If we have reached our threshold to send our info
        if (currentTime >= networkUpdateRate)
        {
            //Sanity Checks
            if (networkController != null)
            {
                if (Atlas.networkActive && Atlas.networkAuthed)
                {
                    //Less than 0 means no valid network ID
                    if (ObjectID > 0)
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
                        Debug.LogError("Invalid Network ID {" + ObjectID + "}, retrying...");
                        if (Atlas.isClient)
                        {
                            networkController.AssignID(this.gameObject, ObjectID);
                        }
                        else
                        {
                            ObjectID = networkController.AssignID(this.gameObject, ObjectID);
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
