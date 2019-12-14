using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace.Models;
using DefaultNamespace.Multiplayer;
using Enemy;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.VectorTile.Geometry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public List<Zombie> Zombies = new List<Zombie>();
        public List<Item> Items = new List<Item>();
        public AbstractMap Map;
        public List<Path> Roads = new List<Path>();
        public MGameState GameState;
        public BaseBuilder BaseBuilder;
        public GameClient GameClient;
        
        public GameObject[] ItemPrefabs = new GameObject[0];
        
        private string _defaultSavePath;

        private void Start()
        {
            Instance = this;
         
            _defaultSavePath = Application.persistentDataPath + "/game-save.xml";
            LoadGameState();

            SaveGameState();
            
            Map.OnTileFinished += (UnityTile tile) =>
            {
                var _rectSizex = tile.Rect.Size.x;
                var _rectSizey = tile.Rect.Size.y;
                var layerExtent = 4096;

                foreach (var layerName in tile.VectorData.LayerNames())
                {
                    if (layerName == "road")
                    {
                        var layer = tile.VectorData.GetLayer(layerName);
                        var featureCount = layer.FeatureCount();

                        for (var i = 0; i < featureCount; i++)
                        {
                            var feature = layer.GetFeature(i);

                            if (feature.GeometryType == GeomType.LINESTRING)
                            {
                                List<List<Point2d<float>>> geom = feature.Geometry<float>(0);

                                foreach (var subGeometry in geom)
                                {
                                    var pointsList = new List<Vector3>(subGeometry.Count);

                                    foreach (var point in subGeometry)
                                    {
                                        var v = tile.transform.TransformPoint(new Vector3(
                                            (float) (point.X / layerExtent * _rectSizex - (_rectSizex / 2)) *
                                            tile.TileScale, 0,
                                            (float) ((layerExtent - point.Y) / layerExtent * _rectSizey -
                                                     (_rectSizey / 2)) * tile.TileScale));
                                        
                                        pointsList.Add(v);
                                    }

                                    if (pointsList.Count > 0)
                                    {
                                        var road = new Path (pointsList, tile);
                                        Roads.Add(road);
                                    }
                                }
                            }
                        }
                    }
                }
                
                BaseBuilder.UpdateBuildingStates();
            };
        }

        private void FixedUpdate()
        {
            UseSupplies();
            CheckLoseCondition();
        }

        public void LoadGameState()
        {
            try
            {
                var loadedGameState = MGameState.DeserializeFromFile(_defaultSavePath);
                GameState = loadedGameState;
                Debug.Log($"Loaded game state ({GameState.DateTime})");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load game state ({exception.Message}). Creating new game state");
                
                GameState = new MGameState
                {
                    Player = new MPlayer
                    {
                        Ammo = MPlayer.AmmoMax,
                        Health = MPlayer.HealthMax,
                        BuildingMaterials = 30,
                        FoodSupplies = 3,
                        WaterSupplies = 3,
                    }
                };

                SaveGameState();
            }

            var timeSpanSinceSavedGame = DateTime.Now - GameState.DateTime;

            TickSupplies((float)timeSpanSinceSavedGame.TotalDays);
        }

        public void SaveGameState()
        {
            GameState.SerializeToFile(_defaultSavePath);
            Debug.Log("Saved game state");
        }

        public void DeleteGameState()
        {
            try
            {
                File.Delete(_defaultSavePath);
                Debug.Log("Deleted game state");
            }
            catch (Exception ex)
            {
                Debug.Log($"Could not delete game state: {ex.Message}");
            }
        }

        public void UseSupplies()
        {
            var secondsInDay = 86400;
            var useAmount = 1f / secondsInDay * Time.deltaTime;
            
            TickSupplies(useAmount);
        }

        private void TickSupplies(float amount)
        {
            if (GameState.Hideout != null)
            {
                GameState.Hideout.StoredFoodSupplies -= amount;
                GameState.Hideout.StoredWaterSupplies -= amount;
            } else if (GameState.Player != null)
            {
                GameState.Player.FoodSupplies -= amount;
                GameState.Player.WaterSupplies -= amount;
            }
        }

        public void CheckLoseCondition()
        {
            if (GameState.Hideout != null)
            {
                if (GameState.Hideout.StoredFoodSupplies < 0)
                {
                    GameUtilities.EndGame("W bazie skończyło się pożywienie.");        
                } else if (GameState.Hideout.StoredWaterSupplies < 0)
                {
                    GameUtilities.EndGame("W bazie skończyła się woda.");  
                }
            } else if (GameState.Player != null)
            {
                if (GameState.Player.FoodSupplies < 0)
                {
                    GameUtilities.EndGame("Skończyło się pożywienie.");        
                } else if (GameState.Player.WaterSupplies < 0)
                {
                    GameUtilities.EndGame("Skończyła się woda.");  
                }
            }
        }

        private List<Path> _cachedRoads = null;
        private List<Path> GetShuffledRoads()
        {
            if (_cachedRoads == null || _cachedRoads.Count != Roads.Count)
            {
                _cachedRoads = new List<Path>(Roads);
            }

            for (var i = 0; i < _cachedRoads.Count; i++)
            {
                var randomIndex = Random.Range(0, _cachedRoads.Count);
                var c = _cachedRoads[randomIndex];

                _cachedRoads[randomIndex] = _cachedRoads[i];
                _cachedRoads[i] = c;
            }

            return _cachedRoads;
        }
        
        public Path GetRoadSuitableForNewZombie(float minLength, Vector2 rangeFromPlayer)
        {
            var roads = GetShuffledRoads();

            foreach (var road in roads)
            {
                if (road.Points.Count > 1 && road.TotalLength >= minLength && !IsPathUsedByZombie(road))
                {
                    var roadIsInRange = true;
                    
                    for (var i = 0; i < 5; i++)
                    {
                        var interpolatedPoint = road.GetInterpolatedPoint(i / 4f);

                        var distance = GetMinDistanceFromPlayerAndBuildings(interpolatedPoint);
                        if (distance < rangeFromPlayer.x || distance > rangeFromPlayer.y)
                        {
                            roadIsInRange = false;
                            break;
                        }
                    }
                    
                    if (!roadIsInRange) continue;
                    var isRoadFarFromOtherRoads = true;
                    
                    for (var i = 0; i < 5; i++)
                    {
                        var interpolatedPoint = road.GetInterpolatedPoint(i / 4f);
                        
                        foreach (var zombie in Zombies)
                        {
                            for (var j = 0; j < 5; j++)
                            {
                                var interpolatedZombieRoadPoint = zombie.OriginRoad.GetInterpolatedPoint(i / 4f);

                                var distance = Vector3.Distance(interpolatedPoint, interpolatedZombieRoadPoint);
                                if (distance < 5)
                                {
                                    isRoadFarFromOtherRoads = false;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (isRoadFarFromOtherRoads)
                    {
                        return road;
                    }
                }
            }

            return null;
        }
        
        public Vector3? GetRandomPositionForItemSpawn(Vector2 rangeFromPlayer)
        {
            var roads = GetShuffledRoads();

            foreach (var road in roads)
            {
                var roadIsInRange = true;
                    
                for (var i = 0; i < 5; i++)
                {
                    var interpolatedPoint = road.GetInterpolatedPoint(i / 5f);

                    var distance = Vector3.Distance(interpolatedPoint, PlayerCharacter.Instance.transform.position);
                    if (distance < rangeFromPlayer.x || distance > rangeFromPlayer.y)
                    {
                        roadIsInRange = false;
                        break;
                    }
                }

                if (roadIsInRange)
                {
                    return road.GetRandomPoint();
                }
            }

            return null;
        }

        public GameObject SpawnRandomItem(Vector3 position)
        {
            var spawnedGameObject = (GameObject)Instantiate(ItemPrefabs[Random.Range(0, ItemPrefabs.Length)], position, Quaternion.identity);
            return spawnedGameObject;
        }

        private float GetMinDistanceFromPlayerAndBuildings(Vector3 pos)
        {
            var minDistance = Vector3.Distance(pos, PlayerCharacter.Instance.transform.position);

            if (BaseBuilder.HideoutInstance)
            {
                var distanceFromHideout = Vector3.Distance(pos, BaseBuilder.HideoutInstance.transform.position);

                if (distanceFromHideout < minDistance)
                {
                    minDistance = distanceFromHideout;
                }
            }

            if (BaseBuilder.OutpostInstances.Count > 0)
            {
                foreach (var entry in BaseBuilder.OutpostInstances)
                {
                    var distanceFromOutpost = Vector3.Distance(pos, entry.Value.transform.position);

                    if (distanceFromOutpost < minDistance)
                    {
                        minDistance = distanceFromOutpost;
                    }
                }
            }

            return minDistance;
        }

        public float GetMinDistanceFromItems(Vector3 pos)
        {
            var minDistance = float.MaxValue;

            foreach (var item in Items)
            {
                var distance = Vector3.Distance(item.transform.position, pos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        private bool IsPathUsedByZombie(Path path)
        {
            foreach (var zombie in Zombies)
            {
                if (zombie.OriginRoad == path)
                {
                    return true;
                }
            }

            return false;
        }
    }
}