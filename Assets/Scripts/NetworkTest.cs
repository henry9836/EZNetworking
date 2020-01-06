using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{

    const int port = 13371;
    public string targetIP = "127.0.0.1";

    public void StartServer()
    {
        Debug.Log("Server Mode");
        // Establish the local endpoint  
        // for the socket. Dns.GetHostName 
        // returns the name of the host  
        // running the application.
        IPAddress ipAddr = IPAddress.Parse(targetIP);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);

        // Creation TCP/IP Socket using  
        // Socket Class Costructor 
        Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        // Using Bind() method we associate a 
        // network address to the Server Socket 
        // All client that will connect to this  
        // Server Socket must know this network 
        // Address 
        listener.Bind(new IPEndPoint(IPAddress.Parse(targetIP), port));

        //listener.BeginReceiveFrom()
       
    }

    public void StartClient()
    {
        Debug.Log("Client Mode");

        var client = new UdpClient();

        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), port);


        Debug.Log("Connecting To: " + targetIP);
        client.Connect(ipEndPoint);
        Debug.Log("Connected!");
        Debug.Log("Sending Messages...");

        const string one = "Hello This is a UDP Connection Test!";
        const string two = "yay :D";
        const string three = "goodbye";

        client.Send(Encoding.ASCII.GetBytes(one), one.Length);
        client.Send(Encoding.ASCII.GetBytes(two), two.Length);
        client.Send(Encoding.ASCII.GetBytes(three), three.Length);

        Debug.Log("Done.");

    }

}
