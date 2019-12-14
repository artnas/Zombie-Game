using System;
using System.Collections.Generic;
using Mapbox.Directions;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace DefaultNamespace
{
    public static class NavigationUtilities
    {
        private static Directions Directions => MapboxAccess.Instance.Directions;

        public static void Query(Vector3 start, Vector3 end, AbstractMap map, Action<List<Vector3>> callback)
        {
            if (Directions == null)
            {
                callback(null);
                return;
            }
            
            var directionResource = new DirectionResource(new[]
            {
                GetGeoPosition(start, map.CenterMercator, map.WorldRelativeScale),
                GetGeoPosition(end, map.CenterMercator, map.WorldRelativeScale)
            }, RoutingProfile.Walking);
            
            directionResource.Steps = true;
            
            Directions.Query(directionResource, (DirectionsResponse response) =>
            {
                if (response?.Routes == null || response.Routes.Count < 1)
                {
                    Debug.Log("Nie znaleziono ścieżki");
                    // nie znaleziono ścieżki
                    return;
                }

                var dat = new List<Vector3>();
                foreach (var point in response.Routes[0].Geometry)
                {
                    dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, map.CenterMercator, map.WorldRelativeScale).ToVector3xz());
                }

                callback(dat);
            });
        }
        
        private static Vector2d GetGeoPosition(Vector3 position, Vector2d refPoint, float scale = 1)
        {
            var pos = refPoint + (position / scale).ToVector2d();
            return Conversions.MetersToLatLon(pos);
        }
    }
}