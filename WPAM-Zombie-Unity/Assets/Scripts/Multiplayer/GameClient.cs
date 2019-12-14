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
        public ClientSocket ClientSocket;
        private string _clientIdentifier;
        private float _sendDataInterval = 1f / 2f;
        private WaitForSeconds _sendDataWait;
        private Coroutine _sendDataCoroutine;

        private Dictionary<string, MClientState> _clientStates;
        private Dictionary<string, MultiplayerGhost> _playerGhosts = new Dictionary<string, MultiplayerGhost>();
        
        private void Start()
        {
            _sendDataWait = new WaitForSeconds(_sendDataInterval);
            _clientIdentifier = Guid.NewGuid().ToString();
            _sendDataCoroutine = StartCoroutine(SendDataRoutine());
        }

        private void FixedUpdate()
        {
            UpdateVisualisation();

            if (ClientSocket != null && ClientSocket.ThreadLog.Count > 0)
            {
                // foreach (var log in _clientSocket.ThreadLog)
                // {
                //     Debug.Log(log);
                // }
                
                ClientSocket.ThreadLog.Clear();
            }
        }

        private IEnumerator SendDataRoutine()
        {
            while (true)
            {
                if (ClientSocket != null && ClientSocket.IsConnected())
                {
                    SendData();
                }
                yield return _sendDataWait;
            }
        }

        private void SendData()
        {
            // Debug.Log($"Client: Send data");
            ClientSocket.Send(GetSerializedClientState() + "\n");
        }

        private void ReceiveData(string message)
        {
            // Debug.Log($"Client: Receive data: {message}");
            
            try
            {
                _clientStates = ParseClientStatesDictionary(message);
                
                // Debug.Log($"Client: Received states {_clientStates.Count}");

                foreach (var entry in _clientStates)
                {
                    Debug.Log($"{entry.Key}: {entry.Value.GeoPosition}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Received message couldn't be parsed: {message}, {ex.Message}");
                // _clientStates = null;
            }
        }
        
        private Dictionary<string, MClientState> ParseClientStatesDictionary(string message)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, MClientState>>(message);
        }
        
        public void Connect()
        {
            if (ClientSocket == null)
            {
                // Debug.Log("GameClient: Created client socket");
                ClientSocket = new ClientSocket(IPAddress.Parse(IpAddress), 10000);
            }
            
            // Debug.Log("GameClient: Starting client socket");
            ClientSocket.Start();
            ClientSocket.ReceiveMessageEvent += ReceiveData;
            // Debug.Log("GameClient: Started client socket");
        }

        public void Disconnect()
        {
            ClientSocket.Stop();
            ClientSocket = null;
            _clientStates.Clear();
            UpdateVisualisation();
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
            ClientSocket.Stop();
        }

        private void UpdateVisualisation()
        {
            if (ClientSocket == null || !ClientSocket.IsConnected() || _clientStates == null || _clientStates.Count == 0)
            {
                if (_playerGhosts.Count != 0)
                {
                    foreach (var value in _playerGhosts.Values)
                    {
                        Destroy(value.gameObject);
                    }
                    
                    _playerGhosts.Clear();
                }
            }
            else
            {
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

                            playerGhostEntry.Value.DesiredPosition = worldPosition;
                        }
                    }

                    if (!hasFoundIdentifier)
                    {
                        // stwórz nowego ducha

                        var newGhost = Instantiate(MultiplayerGhostPrefab, worldPosition, Quaternion.identity);
                        newGhost.name = identifier;
                        
                        var newMultiplayerGhostComponent = newGhost.GetComponent<MultiplayerGhost>();
                        
                        newMultiplayerGhostComponent.DesiredPosition = worldPosition;
                        _playerGhosts.Add(identifier, newMultiplayerGhostComponent);
                    }
                }

                List<KeyValuePair<string, MultiplayerGhost>> ghostsToRemove = null;
                foreach (var entry in _playerGhosts)
                {
                    if (!_clientStates.ContainsKey(entry.Key))
                    {
                        if (ghostsToRemove == null)
                        {
                            ghostsToRemove = new List<KeyValuePair<string, MultiplayerGhost>>();
                        }
                        
                        ghostsToRemove.Add(entry);
                    }
                }

                if (ghostsToRemove != null)
                {
                    foreach (var entry in ghostsToRemove)
                    {
                        Destroy(entry.Value.gameObject);
                        _playerGhosts.Remove(entry.Key);
                    }
                }
            }
        }
    }
}