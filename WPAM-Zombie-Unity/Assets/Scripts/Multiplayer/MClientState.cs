using System;
using Mapbox.Utils;

namespace DefaultNamespace.Multiplayer
{
    [System.Serializable]
    public class MClientState
    {
        public string Identifier;
        public Vector2d GeoPosition;
        public DateTime DateTime;
    }
}