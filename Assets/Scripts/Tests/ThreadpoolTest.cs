﻿using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class ThreadpoolTest : MonoBehaviour
{

    [Range (0.0f, 5.0f)]
    public float speedOfThreadSpawn = 1.0f;
    public bool spawnThreads = true;
    

    void Start()
    {
        Debug.LogError("TP Test Start");

        speedOfThreadSpawn = 5.0f - speedOfThreadSpawn;

        ThreadPool.QueueUserWorkItem(ThreadTest);

        StartCoroutine(Loop());

    }

    private void ThreadTest(object state)
    {
        Debug.LogError("Hello From The Threadpool!");

        Debug.LogError("I am thinking for a bit...");

        System.Random rand = new System.Random();

        Thread.Sleep(rand.Next(500, 10000));

        Debug.LogError("Done.");

    }

    IEnumerator Loop()
    {
        while (spawnThreads)
        {
            yield return new WaitForSeconds(speedOfThreadSpawn);
            ThreadPool.QueueUserWorkItem(ThreadTest);
            yield return null;
        }

        Debug.LogError("Stopped Adding Jobs To Pool");

        yield return null;
    }

}
