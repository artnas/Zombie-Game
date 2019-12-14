using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using DefaultNamespace.Models;
using Mapbox.Json;
using Mapbox.Unity.Location;
using UnityEngine;

namespace DefaultNamespace.Multiplayer
{
    public class GameClient : MonoBehaviour
    {
        public GameObject MultiplayerGhostPrefab;
        
        private bool IsConnected = false;
        public string IpAddress;
        private ClientSocket _clientSocket;
        private string _clientIdentifier;
        private float _sendDataInterval = 1f / 15f;
        private WaitForSeconds _sendDataWait;
        private Coroutine _sendDataCoroutine;

        private Dictionary<string, MClientState> _clientStates;
        private Dictionary<string, Transform> _playerGhosts = new Dictionary<string, Transform>();
        
        private void Start()
        {
            _sendDataWait = new WaitForSeconds(_sendDataInterval);
            _clientIdentifier = Guid.NewGuid().ToString();
            _sendDataCoroutine = StartCoroutine(SendDataRoutine());
        }

        private void FixedUpdate()
        {
            UpdateVisualisation();

            if (_clientSocket != null && _clientSocket.ThreadLog.Count > 0)
            {
                // foreach (var log in _clientSocket.ThreadLog)
                // {
                //     Debug.Log(log);
                // }
                
                _clientSocket.ThreadLog.Clear();
            }
        }

        private IEnumerator SendDataRoutine()
        {
            while (true)
            {
                if (_clientSocket != null && _clientSocket.IsConnected())
                {
                    SendData();
                }
                yield return _sendDataWait;
            }
        }

        private void SendData()
        {
            // Debug.Log($"Client: Send data");
            _clientSocket.Send(GetSerializedClientState() + "\n");
        }

        private void ReceiveData(string message)
        {
            // Debug.Log($"Client: Receive data: {message}");
            
            try
            {
                _clientStates = ParseClientStatesDictionary(message);
                
                // Debug.Log($"Client: Received states {_clientStates.Count}");

                // foreach (var entry in _clientStates)
                // {
                //     Debug.Log($"{entry.Key}: {entry.Value.GeoPosition}");
                // }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Received message couldn't be parsed: {message}, {ex.Message}");
                _clientStates = null;
            }
        }
        
        private Dictionary<string, MClientState> ParseClientStatesDictionary(string message)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, MClientState>>(message);
        }
        
        public void Connect()
        {
            if (_clientSocket == null)
            {
                // Debug.Log("GameClient: Created client socket");
                _clientSocket = new ClientSocket(IPAddress.Parse(IpAddress), 10000);
            }
            
            // Debug.Log("GameClient: Starting client socket");
            _clientSocket.Start();
            _clientSocket.ReceiveMessageEvent += ReceiveData;
            // Debug.Log("GameClient: Started client socket");
        }

        public void Disconnect()
        {
            _clientSocket.Stop();
        }
        
        private string GetSerializedClientState()
        {
            // get geoposition
            var geoPosition =
                LocationProviderFactory.Instance.mapManager.WorldToGeoPosition(PlayerCharacter.Instance
                    .transform.position);

            var clientState = new MClientState
            {
                Identifier = _clientIdentifier,
                DateTime = DateTime.Now,
                GeoPosition = geoPosition
            };

            return JsonConvert.SerializeObject(clientState, Formatting.None);
        }

        private void OnDestroy()
        {
            _clientSocket.Stop();
        }

        private void UpdateVisualisation()
        {
            if (_clientStates == null || _clientStates.Count == 0)
            {
                if (_playerGhosts.Count != 0)
                {
                    foreach (var value in _playerGhosts.Values)
                    {
                        Destroy(value.gameObject);
                    }
                    
                    _playerGhosts.Clear();
                }
                
                return;
            }
            
            foreach (var clientStateEntry in _clientStates)
            {
                var identifier = clientStateEntry.Key;

                if (identifier == _clientIdentifier) continue;
                
                var hasFoundIdentifier = false;
                
                var worldPosition = LocationProviderFactory.Instance.mapManager.GeoToWorldPosition(clientStateEntry.Value.GeoPosition);
                
                foreach (var playerGhostEntry in _playerGhosts)
                {
                    if (playerGhostEntry.Key == identifier)
                    {
                        hasFoundIdentifier = true;
                        
                        // aktualizuj ducha

                        playerGhostEntry.Value.GetComponent<MultiplayerGhost>().DesiredPosition = worldPosition;
                    }
                }

                if (!hasFoundIdentifier)
                {
                    // stwórz nowego ducha

                    var newGhost = Instantiate(MultiplayerGhostPrefab, worldPosition, Quaternion.identity);
                    newGhost.name = identifier;
                    newGhost.GetComponent<MultiplayerGhost>().DesiredPosition = worldPosition;
                    _playerGhosts.Add(identifier, newGhost.transform);
                }
            }
        }
    }
}