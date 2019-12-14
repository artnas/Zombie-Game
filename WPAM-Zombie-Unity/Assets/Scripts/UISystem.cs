using System;
using DefaultNamespace.Models;
using Mapbox.Unity.Location;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class UISystem : MonoBehaviour
    {
        public static UISystem Instance;

        public EventSystem EventSystem;
        
        public Text PlayerInformationText;
        public Text HideoutInformationText;
        
        public Button CreateBaseButton;
        public Button CreateOutpostButton;

        public InputField IpAddressInputField;

        public Button ConnectButton;
        public Text ConnectButtonText;
        
        private GameObject _currentModeParent;

        public GameObject GameModeParent;
        public GameObject OptionsModeParent;

        ILocationProvider _locationProvider;
        ILocationProvider LocationProvider
        {
            get
            {
                if (_locationProvider == null)
                {
                    _locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
                }

                return _locationProvider;
            }
        }
        
        private void Start()
        {
            Instance = this;

            ChangeMode(true);

            var gameState = GameManager.Instance.GameState;
            
            if (gameState.Hideout == null)
            {
                CreateBaseButton.gameObject.SetActive(true);
                CreateBaseButton.onClick.AddListener(() =>
                {
                    // get geoposition
                    var geoPosition =
                        LocationProviderFactory.Instance.mapManager.WorldToGeoPosition(PlayerCharacter.Instance
                            .transform.position);

                    // create base model
                    gameState.Hideout = new MHideout
                    {
                        GeoPosition = geoPosition,
                        StoredFoodSupplies = 3,
                        StoredWaterSupplies = 3
                    };
                    
                    GameManager.Instance.BaseBuilder.UpdateBuildingStates();
                    GameManager.Instance.SaveGameState();

                    CreateBaseButton.gameObject.SetActive(false);
                });
            }
            
            CreateOutpostButton.onClick.AddListener(() =>
            {
                // get geoposition
                var geoPosition =
                    LocationProviderFactory.Instance.mapManager.WorldToGeoPosition(PlayerCharacter.Instance
                        .transform.position);

                // create base model
                var newOutpostModel = new MOutpost
                {
                    GeoPosition = geoPosition
                };

                // zużyj surowce
                GameManager.Instance.GameState.Player.BuildingMaterials -= 30;
                
                GameManager.Instance.GameState.Outposts.Add(newOutpostModel);
                    
                GameManager.Instance.BaseBuilder.UpdateBuildingStates();
                GameManager.Instance.SaveGameState();

                CreateBaseButton.gameObject.SetActive(false);
            });

            if (PlayerPrefs.HasKey("IpAddress"))
            {
                IpAddressInputField.text = PlayerPrefs.GetString("IpAddress");
            }
        }

        private void FixedUpdate()
        {
            if (_currentModeParent == GameModeParent)
            {
                UpdateBuildOutpostButton();
                UpdatePlayerInformationText();
                UpdateHideoutInformationText();
            }
            else
            {
                UpdateConnectButton();
            }
        }

        private void UpdateBuildOutpostButton()
        {
            var buildOutpostButtonState = GameManager.Instance.GameState.Hideout != null;
            if (CreateOutpostButton.IsActive() != buildOutpostButtonState)
            {
                CreateOutpostButton.gameObject.SetActive(buildOutpostButtonState);
            }

            if (buildOutpostButtonState)
            {
                var hasResources = GameManager.Instance.GameState.Player.BuildingMaterials >= 30;
                if (CreateOutpostButton.IsActive() != hasResources)
                {
                    CreateOutpostButton.gameObject.SetActive(hasResources);
                }
            }
        }

        private void UpdatePlayerInformationText()
        {
            var playerModel = GameManager.Instance.GameState.Player;
            var text = $"Gracz:" +
                       $"\nZdrowie: {playerModel.Health}/{MPlayer.HealthMax}" +
                       $"\nAmunicja: {playerModel.Ammo}" +
                       $"\nZapas jedzenia: {GameUtilities.GetSupplyTime(playerModel.FoodSupplies)}" +
                       $"\nZapas wody: {GameUtilities.GetSupplyTime(playerModel.WaterSupplies)}" +
                       $"\nMateriały budowlane: {playerModel.BuildingMaterials}";

            if (PlayerInformationText.text != text)
            {
                PlayerInformationText.text = text;
            }
        }

        private void UpdateHideoutInformationText()
        {
            var hideoutModel = GameManager.Instance.GameState.Hideout;

            if (!PlayerCharacter.Instance.IsNearHideout || hideoutModel == null)
            {
                HideoutInformationText.enabled = false;
                return;
            }
            
            HideoutInformationText.enabled = true;
            
            var text = $"Baza:" +
                       $"\nZapas jedzenia: {GameUtilities.GetSupplyTime(hideoutModel.StoredFoodSupplies)}" +
                       $"\nZapas wody: {GameUtilities.GetSupplyTime(hideoutModel.StoredWaterSupplies)}";

            if (HideoutInformationText.text != text)
            {
                HideoutInformationText.text = text;
            }
        }

        public void ChangeMode(bool isGame)
        {
            _currentModeParent = isGame ? GameModeParent : OptionsModeParent;

            GameModeParent.SetActive(_currentModeParent == GameModeParent);
            OptionsModeParent.SetActive(_currentModeParent == OptionsModeParent);
        }

        public void OnIpAddressInputChanged()
        {
            var text = IpAddressInputField.text;
            
            PlayerPrefs.SetString("IpAddress", text);
            PlayerPrefs.Save();
        }

        public void ConnectToMultiplayer()
        {
            var ipAddress = IpAddressInputField.text;

            GameManager.Instance.GameClient.IpAddress = ipAddress;
            GameManager.Instance.GameClient.Connect();
        }

        private void UpdateConnectButton()
        {
            // TODO
        }
    }
}