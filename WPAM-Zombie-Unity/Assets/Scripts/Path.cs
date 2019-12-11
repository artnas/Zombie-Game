using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace DefaultNamespace
{
    [System.Serializable]
    public class Path
    {
        public List<Vector3> Points;
        public readonly UnityTile Tile;

        public float TotalLength = 0;

        public Path(List<Vector3> points, UnityTile tile)
        {
            Points = points;
            Tile = tile;

            TotalLength = CalculateTotalLength();
        }

        private float CalculateTotalLength()
        {
            var length = 0f;
            var lastPoint = Points[0];

            for (int i = 1; i < Points.Count; i++)
            {
                var currentPoint = Points[i];

                length += Vector3.Distance(currentPoint, lastPoint);
                
                lastPoint = currentPoint;
            }

            return length;
        }

        public Vector3 GetInterpolatedPoint(float p)
        {
            var f = (Points.Count - 1) * p;

            var fMod = f % 1.0f;

            var floor = Mathf.FloorToInt(f);
            var ceil = Mathf.CeilToInt(f);

            if (floor == Points.Count)
            {
                floor--;
            }

            if (ceil == Points.Count)
            {
                ceil--;
            }

            var a = Points[floor];
            var b = Points[ceil];
            
            if (ceil == floor)
            {
                return Points[ceil];
            }

            return Vector3.Lerp(a, b, fMod);
        }

        public Vector3 GetRandomPoint()
        {
            return Points[Random.Range(0, Points.Count)];
        }

        public (int, Vector3) GetClosestNode(Vector3 position)
        {
            var resultIndex = -1;
            var resultPosition = Vector3.zero;
            
            var minDistance = float.MaxValue;
            for (var i = 0; i < Points.Count; i++)
            {
                var distanceToPoint = Vector3.Distance(position, Points[i]);

                if (distanceToPoint < minDistance)
                {
                    minDistance = distanceToPoint;
                    resultIndex = i;
                    resultPosition = Points[i];
                }
            }

            return (resultIndex, resultPosition);
        }

        public void Simplify()
        {
            var newPath = new List<Vector3>(Points.Count);
            var distanceThreshold = 2f;

            for (var i = 0; i < Points.Count; i++)
            {
                var currentPoint = Points[i];
                var mergedPoint = currentPoint;

                for (var j = i; j < Points.Count; j++)
                {
                    var pointToCheck = Points[j];

                    var distance = Vector3.Distance(mergedPoint, pointToCheck);

                    if (distance < distanceThreshold)
                    {
                        mergedPoint = (mergedPoint + pointToCheck) / 2;
                        Points.RemoveAt(j);
                    }
                }

                newPath.Add(mergedPoint);
            }

            Points = newPath;
            TotalLength = CalculateTotalLength();
        }

        public void DebugDraw()
        {
            if (Points == null || Points.Count <= 0) return;
            
            var lastPoint = Points[0];

            for (var i = 1; i < Points.Count; i++)
            {
                var currentPoint = Points[i];
                Debug.DrawLine(lastPoint, currentPoint, Color.blue);
                lastPoint = currentPoint;
            }
        }
    }
}