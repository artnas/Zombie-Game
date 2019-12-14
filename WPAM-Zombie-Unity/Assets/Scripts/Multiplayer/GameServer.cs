using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Mapbox.Json;
using UnityEngine;

namespace DefaultNamespace.Multiplayer
{
    public class GameServer : MonoBehaviour
    {
        private ServerSocket _serverSocket;
        private float _sendDataInterval = 1f / 2f;
        private WaitForSeconds _sendDataWait;
        private Coroutine _sendDataCoroutine;
        private Dictionary<string, MClientState> _clientStates = new Dictionary<string, MClientState>();
        
        private void Start()
        {
            #if UNITY_EDITOR
                StartServer();

                _sendDataWait = new WaitForSeconds(_sendDataInterval);
                _sendDataCoroutine = StartCoroutine(SendDataRoutine());
            #endif
        }
        
        private IEnumerator SendDataRoutine()
        {
            while (true)
            {
                RemoveOldClientStates();
                SendData();
                yield return _sendDataWait;
            }
        }

        private void SendData()
        {
            var message = GetSerializedClientStatesDictionary();
            
            // Debug.Log($"Server: Sending message to all clients ({_clientStates.Count}): {message}");
            _serverSocket.SendToAllConnectedClients(message);
        }

        private string GetSerializedClientStatesDictionary()
        {
            var statesCopy = new Dictionary<string, MClientState>(_clientStates);
            return JsonConvert.SerializeObject(statesCopy, Formatting.None) + "\n";
        }

        private void RemoveOldClientStates()
        {
            if (_clientStates != null)
            {
                List<string> statesToRemove = null;

                foreach (var state in _clientStates)
                {
                    if ((DateTime.Now - state.Value.DateTime).TotalSeconds > 3)
                    {
                        if (statesToRemove == null)
                        {
                            statesToRemove = new List<string>();
                        }

                        statesToRemove.Add(state.Key);
                    }
                }

                if (statesToRemove != null)
                {
                    foreach (var stateToRemove in statesToRemove)
                    {
                        _clientStates.Remove(stateToRemove);
                    }
                }
            }
        }

        private void ReceiveData(string message)
        {
            // Debug.Log($"Server: Receive message {message}");
            
            try
            {
                var clientState = ParseClientState(message);
                _clientStates[clientState.Identifier] = clientState;
                
                // Debug.Log($"Server: Received update from {clientState.Identifier}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Couldnt parse message: {message}");
            }
        }

        private MClientState ParseClientState(string message)
        {
            return JsonConvert.DeserializeObject<MClientState>(message);
        }

        private void OnDestroy()
        {
            _serverSocket.Stop();
        }

        private void StartServer()
        {
            // _serverSocket = new ServerSocket(IPAddress.Parse("127.0.0.1"), 10000);
            _serverSocket = new ServerSocket(IPAddress.Parse("192.168.0.103"), 10000);
            _serverSocket.Start();
            _serverSocket.ReceiveMessageEvent += ReceiveData;
        }
    }
}