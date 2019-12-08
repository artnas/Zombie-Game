using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public List<Zombie> Zombies = new List<Zombie>();
        public AbstractMap Map;
        public List<Path> Roads = new List<Path>();

        private void Start()
        {
            Instance = this;
            
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
            };
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
                    var distance = Vector3.Distance(road.GetRandomPoint(), PlayerCharacter.Instance.transform.position);
                    if (distance >= rangeFromPlayer.x && distance <= rangeFromPlayer.y)
                    {
                        return road;
                    }
                }
            }

            return null;
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