using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassFromInputToNetwork : MonoBehaviour
{

    public EZNetworking networkController;

    public void PassOn()
    {
        networkController.AssignIP(GetComponent<Text>().text);
    }
}
