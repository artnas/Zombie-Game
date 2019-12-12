using System;
using DefaultNamespace.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class Item : MonoBehaviour
    {
        private readonly float _floatTowardsPlayerRadius = 10;
        private readonly float _collectRadius = 2;
        private Transform _meshTransform;

        public ItemType Type;
        public float Amount = 1;

        private Vector3 _animationSpeed = new Vector3(1, 1.5f, 1);
        private Vector3 _animationOffset;
        
        public float DespawnRadius = 60;

        private void Start()
        {
            _meshTransform = transform.GetChild(0);
            _animationOffset = new Vector3(Random.value, Random.value, Random.value);
            
            GameManager.Instance.Items.Add(this);
            
            switch (Type)
            {
                case ItemType.Ammo:
                    Amount = Random.Range(1, 3);
                    break;
                case ItemType.Food:
                    Amount = Mathf.Lerp(0.25f, 0.5f, Random.value);
                    break;
                case ItemType.Water:
                    Amount = Mathf.Lerp(0.35f, 0.6f, Random.value);
                    break;
                case ItemType.BuildingMaterial:
                    Amount = Random.Range(5, 8);
                    break;
            }
        }

        private void Update()
        {
            var distanceToPlayer = Vector3.Distance(transform.position, PlayerCharacter.Instance.transform.position);

            if (distanceToPlayer < _floatTowardsPlayerRadius)
            {
                var strength = distanceToPlayer / _floatTowardsPlayerRadius;

                transform.position = Vector3.Lerp(transform.position, PlayerCharacter.Instance.transform.position,
                    Time.deltaTime * (1f - strength) * 5f);
            }

            if (distanceToPlayer < _collectRadius)
            {
                Collect();    
            }
            
            AnimateMeshTransform();

            if (distanceToPlayer > DespawnRadius)
            {
                Destroy(gameObject);
            }
        }

        private void AnimateMeshTransform()
        {
            _meshTransform.localPosition = new Vector3(
                Mathf.Sin((Time.time + _animationOffset.x)*Mathf.PI*_animationSpeed.x) * 0.5f, 
                Mathf.Sin((Time.time + _animationOffset.y)*Mathf.PI*_animationSpeed.y) * 0.25f + 1f,
                Mathf.Sin((Time.time + _animationOffset.z)*Mathf.PI*_animationSpeed.z) * 0.5f);

            _meshTransform.rotation = Quaternion.Euler(new Vector3(
                Mathf.Sin((Time.time + _animationOffset.x) * Mathf.PI * _animationSpeed.x) * 5,
                Mathf.Sin((Time.time + _animationOffset.y) * Mathf.PI * _animationSpeed.y) * 5,
                Mathf.Sin((Time.time + _animationOffset.z) * Mathf.PI * _animationSpeed.z) * 5));
        }

        private void Collect()
        {
            var playerModel = GameManager.Instance.GameState.Player; 
            
            switch (Type)
            {
                case ItemType.Ammo:
                    playerModel.Ammo += (int)Amount;
                    playerModel.Ammo = Mathf.Min(playerModel.Ammo, MPlayer.AmmoMax);
                    break;
                case ItemType.Food:
                    playerModel.FoodSupplies += Amount;
                    break;
                case ItemType.Water:
                    playerModel.WaterSupplies += Amount;
                    break;
                case ItemType.BuildingMaterial:
                    playerModel.BuildingMaterials += (int)Amount;
                    break;
            }
            
            Destroy(gameObject);
        }
        
        private void OnDestroy()
        {
            GameManager.Instance.Items.Remove(this);
        }
    }
}