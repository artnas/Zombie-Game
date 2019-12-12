using System.Collections.Generic;
using DefaultNamespace.Models;
using Mapbox.Unity.Location;
using UnityEngine;

namespace DefaultNamespace
{
    public class BaseBuilder : MonoBehaviour
    {
        [HideInInspector]
        public GameObject HideoutInstance;
        [HideInInspector]
        public Dictionary<MOutpost, GameObject> OutpostInstances = new Dictionary<MOutpost, GameObject>();

        public GameObject HideoutPrefab;
        public GameObject OutpostPrefab;
        
        public void UpdateBuildingStates()
        {
            var gameState = GameManager.Instance.GameState;

            if (gameState.Hideout != null && !HideoutInstance)
            {
                HideoutInstance = BuildHideoutInstance(gameState.Hideout);
            }

            if (gameState.Outposts != null && gameState.Outposts.Count > 0)
            {
                foreach (var outpostModel in gameState.Outposts)
                {
                    if (!OutpostInstances.ContainsKey(outpostModel))
                    {
                        var outpostInstance = BuildOutpostInstance(outpostModel);
                        OutpostInstances[outpostModel] = outpostInstance;
                    }
                }
            }
        }

        private GameObject BuildHideoutInstance(MHideout hideoutModel)
        {
            var map = LocationProviderFactory.Instance.mapManager;
            var worldPosition = map.GeoToWorldPosition(hideoutModel.GeoPosition);
            Debug.Log($"{hideoutModel.GeoPosition} -> {worldPosition}");

            var instance = Instantiate(HideoutPrefab, worldPosition, Quaternion.identity);

            instance.GetComponent<Hideout>().Model = hideoutModel;

            return instance;
        }

        private GameObject BuildOutpostInstance(MOutpost outpostModel)
        {
            var map = LocationProviderFactory.Instance.mapManager;
            var worldPosition = map.GeoToWorldPosition(outpostModel.GeoPosition);
            Debug.Log($"{outpostModel.GeoPosition} -> {worldPosition}");

            var instance = Instantiate(OutpostPrefab, worldPosition, Quaternion.identity);
            
            instance.GetComponent<Outpost>().Model = outpostModel;

            return instance;
        }
    }
}