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
        IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddr = ipHost.AddressList[0];
        ipAddr = IPAddress.Parse(targetIP);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);

        // Creation TCP/IP Socket using  
        // Socket Class Costructor 
        Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // Using Bind() method we associate a 
        // network address to the Server Socket 
        // All client that will connect to this  
        // Server Socket must know this network 
        // Address 
        listener.Bind(localEndPoint);

        // Using Listen() method we create  
        // the Client list that will want 
        // to connect to Server 
        listener.Listen(10);

        while (true)
        {

            Debug.Log("Waiting connection ... ");

            // Suspend while waiting for 
            // incoming connection Using  
            // Accept() method the server  
            // will accept connection of client 
            Socket clientSocket = listener.Accept();

            // Data buffer 
            byte[] bytes = new Byte[1024];
            string data = null;

            while (true)
            {

                int numByte = clientSocket.Receive(bytes);

                data += Encoding.ASCII.GetString(bytes,
                                           0, numByte);

                if (data.IndexOf("<EOF>") > -1)
                    break;
            }

            Console.WriteLine("Text received -> {0} ", data);
            byte[] message = Encoding.ASCII.GetBytes("Test Server");

            // Send a message to Client  
            // using Send() method 
            clientSocket.Send(message);

            // Close client Socket using the 
            // Close() method. After closing, 
            // we can use the closed Socket  
            // for a new Client Connection 
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }

    public void StartClient()
    {
        Debug.Log("Client Mode");

        // Establish the remote endpoint 
        // for the socket.
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddr = host.AddressList[0];
        ipAddr = IPAddress.Parse(targetIP);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);


        // Creation TCP/IP Socket using 
        // Socket Class Costructor 
        Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // Connect Socket to the remote 
        // endpoint using method Connect() 
        Debug.Log("Attempting To Connect To: " + ipAddr);
        sender.Connect(localEndPoint);
        Debug.Log("Connection Successful!");
        // We print EndPoint information 
        // that we are connected 
        Console.WriteLine("Socket connected to -> {0} ", sender.RemoteEndPoint.ToString());

        // Creation of messagge that 
        // we will send to Server 
        byte[] messageSent = Encoding.ASCII.GetBytes("Test Client<EOF>");
        int byteSent = sender.Send(messageSent);

        // Data buffer 
        byte[] messageReceived = new byte[1024];

        // We receive the messagge using 
        // the method Receive(). This 
        // method returns number of bytes 
        // received, that we'll use to 
        // convert them to string 
        int byteRecv = sender.Receive(messageReceived);
        Console.WriteLine("Message from Server -> {0}",
            Encoding.ASCII.GetString(messageReceived,
                                        0, byteRecv));

        // Close Socket using 
        // the method Close() 
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();

    }

}
