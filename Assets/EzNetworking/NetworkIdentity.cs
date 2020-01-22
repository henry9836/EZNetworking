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

    private EZNetworking networkController;
    private float networkUpdateRate;
    private float currentTime = 0.0f;
    private int ID = -1;

    void BasicLogic()
    {

    }

    void TransformLogic()
    {
        //Get infomation
        string data = transform.position.ToString();

        //Encode for network
        byte[] networkData = Encoding.ASCII.GetBytes(data);

        //Send infomation
        if (safeSend)
        {
            networkController.SafeSend(Atlas.PACKETTYPE.TRANSFORM, networkData);
        }
        else {
            networkController.Send(Atlas.PACKETTYPE.TRANSFORM, networkData);
        }
    }

    void RigidbodyLogic()
    {

    }

    void Start()
    {
        
        //Find the network interface
        networkController = FindObjectOfType<EZNetworking>();

        //Get our ID
        if (networkController != null)
        {
            ID = networkController.AssignID();
        }
        
        //Get the rate at which we should send updates to the network
        networkUpdateRate = sendDelay;

        //Send our info on spawn
        currentTime = networkUpdateRate;
    }

    //Update Loop Tied to Our Fixed Update Info
    void FixedUpdate()
    {
        //Sanity Checks
        if (networkController != null)
        {
            if (Atlas.networkActive)
            {
                if (ID != -1)
                {
                    //update timer
                    currentTime += Time.deltaTime;

                    //If we have reached our threshold to send our info
                    if (currentTime >= networkUpdateRate)
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
                }
                else
                {
                    Debug.LogError("Invalid Network ID: -1");
                }
            }
            else
            {
                Debug.LogWarning("A Network Identity Is Active In The Scene But There Is No Network Currently Active");
            }
        }
        else
        {
            Debug.LogError("Could not find network interface");
        }
    }
}
