using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandHandler : MonoBehaviour
{

    public enum COMMANDTYPE
    {
        UNASSIGNED,
        TESTING
    };

    public void runCommand(COMMANDTYPE cmdType, string[] data)
    {
        switch (cmdType)
        {
            case COMMANDTYPE.TESTING:
                {
                    for (int i = 0; i < data.Length - 1; i++)
                    {
                        Debug.Log("data["+i.ToString()+"] " + data[i]);
                    }
                    break;
                }
            default:
                {
                    Debug.LogWarning("runCommand Called but there is not case set up for [" + cmdType.ToString() + "]");
                    break;
                }
        }
    }

}
