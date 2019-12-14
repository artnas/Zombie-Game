using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace DefaultNamespace.Multiplayer
{
    public class ClientSocket
    {
        private IPAddress _serverIpAddress;
        private int _serverPort;
        private Thread _thread;
        private Socket _sender;
        private bool _isConnecting = false;

        public delegate void ReceiveMessage(string text);

        public ReceiveMessage ReceiveMessageEvent;
        
        public Queue<string> ThreadLog = new Queue<string>();
        
        public ClientSocket(IPAddress serverIpAddress, int serverPort)
        {
            _serverIpAddress = serverIpAddress;
            _serverPort = serverPort;
        }

        public void Start()
        {
            _thread = new Thread(StartClient);
            _thread.Start();
        }

        public void Stop()
        {
            if (_sender != null)
            {
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
                _sender = null;
            }

            if (_thread != null)
            {
                _thread.Abort();
                _thread = null;
            }
        }

        private void StartClient()
        {
            _isConnecting = true;
            
            // Data buffer for incoming data.  
            byte[] bytes = new byte[4096];
            
            ThreadLog.Enqueue("ClientSocket: Starting client");

            // Connect to a remote device.  
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(_serverIpAddress, _serverPort);

                // Create a TCP/IP  socket.  
                _sender = new Socket(_serverIpAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                ThreadLog.Enqueue("ClientSocket: Created socket");
                // Debug.Log("ClientSocket: Created socket");
                
                ThreadLog.Enqueue($"ClientSocket: Connecting to {_serverIpAddress}:{_serverPort}");

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    _sender.Connect(remoteEP);
                    // Debug.Log($"Socket connected to {_sender.RemoteEndPoint}");
                    ThreadLog.Enqueue($"Socket connected to {_sender.RemoteEndPoint}");

                    _isConnecting = false;

                    while (true)
                    {
                        try
                        {
                            int bytesRec = _sender.Receive(bytes);
                            var messageString = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            // Debug.Log($"Received message: {messageString}");
                            ReceiveMessageEvent?.Invoke(messageString);
                        }
                        catch (ArgumentNullException ane)
                        {
                            // Debug.Log($"ArgumentNullException : {ane}");
                            ThreadLog.Enqueue($"ArgumentNullException : {ane}");
                        }
                        catch (SocketException se)
                        {
                            // Debug.Log($"SocketException : {se}");
                            ThreadLog.Enqueue($"SocketException : {se}");
                        }
                        catch (Exception e)
                        {
                            // Debug.Log($"Unexpected exception : {e}");
                            ThreadLog.Enqueue($"Unexpected exception : {e}");
                        }
                    }
                }
                catch (ArgumentNullException ane)
                {
                    // Debug.Log($"ArgumentNullException : {ane}");
                    ThreadLog.Enqueue($"ArgumentNullException : {ane}");
                }
                catch (SocketException se)
                {
                    // Debug.Log($"SocketException : {se}");
                    ThreadLog.Enqueue($"SocketException : {se}");
                }
                catch (Exception e)
                {
                    // Debug.Log($"Unexpected exception : {e}");
                    ThreadLog.Enqueue($"Unexpected exception : {e}");
                    _isConnecting = false;
                }
            }
            catch (Exception e)
            {
                // Debug.Log($"Unexpected exception : {e}");
                ThreadLog.Enqueue($"Unexpected exception : {e}");
                _isConnecting = false;
            }
        }

        public void Send(string messageString)
        {
            byte[] msg = Encoding.ASCII.GetBytes(messageString);
            int bytesSent = _sender.Send(msg);
            // Debug.Log($"Client: Sent message: {messageString}");
            ThreadLog.Enqueue($"Client: Sent message: {messageString}");
        }
        
        public bool IsConnected()
        {
            return (_sender != null && _sender.Connected);
        }

        public bool IsConnecting()
        {
            return _isConnecting;
        }

    }
}