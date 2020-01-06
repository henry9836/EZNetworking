using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

/*
// A C# program for Client 
namespace Client
{

    class Program
    {

        // Main Method 
        static void Main(string[] args)
        {
            ExecuteClient();
        }

        // ExecuteClient() Method 
        static void ExecuteClient()
        {

            try
            {

                // Establish the remote endpoint 
                // for the socket. This example 
                // uses port 11111 on the local 
                // computer. 
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

                // Creation TCP/IP Socket using 
                // Socket Class Costructor 
                Socket sender = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                try
                {

                    // Connect Socket to the remote 
                    // endpoint using method Connect() 
                    sender.Connect(localEndPoint);

                    // We print EndPoint information 
                    // that we are connected 
                    Console.WriteLine("Socket connected to -> {0} ",
                                sender.RemoteEndPoint.ToString());

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

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }
    }
}


 */

public class NetworkTest : MonoBehaviour
{

    const int port = 13371;

    public void StartServer()
    {
        Debug.Log("Server Mode");
    }

    public void StartClient()
    {
        Debug.Log("Client Mode");

        // Establish the remote endpoint 
        // for the socket.
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddr = host.AddressList[0];
        ipAddr = IPAddress.Parse("127.0.0.1");
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

    private void Start()
    {
        
    }
}
