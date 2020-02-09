using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBEGONE : MonoBehaviour
{

    public List<GameObject> ui = new List<GameObject>();

    bool showing = true;

    void FixedUpdate()
    {
        if (Atlas.networkAuthed)
        {
            if (showing)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    ui[i].SetActive(false);
                }
                showing = false;
            }
        }
        else
        {
            if (!showing)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    ui[i].SetActive(true);
                }
                showing = true;
            }
        }
    }
}
