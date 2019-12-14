using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace DefaultNamespace.Multiplayer
{
    // State object for reading client data asynchronously  
    public class StateObject {  
        // Client  socket.  
        public Socket workSocket = null;  
        // Size of receive buffer.  
        public const int BufferSize = 4096;  
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];  
        // Received data string.  
        public StringBuilder sb = new StringBuilder();    
    }  
    
    public class ServerSocket
    {
        // Thread signal.  
        private ManualResetEvent allDone = new ManualResetEvent(false);

        private IPAddress _ipAddress;
        private int _port;
        private Thread _thread;
        private Socket _listener;
        
        public delegate void ReceiveMessage(string text);
        public ReceiveMessage ReceiveMessageEvent;

        private List<Socket> _connectedClients = new List<Socket>();

        public ServerSocket(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public void Start()
        {
            _thread = new Thread(StartListening);
            _thread.Start();
        }
        
        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Shutdown(SocketShutdown.Both);  
                _listener.Close();
                _listener = null;
            }
            
            if (_thread == null) return;
            _thread.Abort();
            _thread = null;
        }

        private void StartListening()
        {
            IPEndPoint localEndPoint = new IPEndPoint(_ipAddress, _port);

            // Create a TCP/IP socket.  
            _listener = new Socket(_ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                _listener.Bind(localEndPoint);
                _listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Debug.Log("Waiting for a connection...");
                    _listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        _listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            
            _connectedClients.Add(handler);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            
            // handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //     new AsyncCallback(ReadCallback), state);
            RecieveHandler(handler, state);
        }

        private void RecieveHandler(Socket handler, StateObject state)
        {
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                ReadCallback, state);
            RecieveHandler(handler, new StateObject
            {
                workSocket = handler
            });
        }

        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject) ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString();

                // if (content.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                // if (content.IndexOf("</MClientState>", StringComparison.Ordinal) > -1)
                // if (true)
                if (content.IndexOf("\n", StringComparison.Ordinal) > -1)
                {
                    // Debug.Log($"ALL DATA");
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    // Debug.Log($"Read {content.Length} bytes from socket. \n Data : {content}");
                    // Echo the data back to the client.  
                    // Send(handler, content);
                    var contents = content.Split(new[]{"\n"}, StringSplitOptions.None);
                
                    foreach (var c in contents)
                    {
                        if (c.Length > 2)
                        {
                            ReceiveMessageEvent?.Invoke(c);
                        }
                    }
                    // ReceiveMessageEvent?.Invoke(content);
                }
                else
                {
                    Debug.Log($"MORE DATA");
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
        }

        public void SendToAllConnectedClients(string message)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            
            for (var i = 0; i < _connectedClients.Count; i++)
            {
                var client = _connectedClients[i];
                if (client.Connected)
                {
                    // Debug.Log($"Sending to {client.LocalEndPoint}");
                    client.BeginSend(byteData, 0, byteData.Length, 0,
                        OnMessageSentToClient, client);
                }
                else
                {
                    _connectedClients.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnMessageSentToClient(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                // Debug.Log($"Sent {bytesSent} bytes to client.");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}