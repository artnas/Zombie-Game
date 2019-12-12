using System;
using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class GameEventsSpawner : MonoBehaviour
    {
        private Vector2 _updateIntervalRange = new Vector2(1, 2);
        private Coroutine _behaviourCoroutine;

        public GameObject ZombiePrefab;
        private Vector2 _zombieSpawnRadiusRange = new Vector2(25, 60);
        public int MaxZombiesCount = 5;
        
        private Vector2 _itemSpawnRadiusRange = new Vector2(15, 45);
        public int MaxItemsCount = 10;

        private void Start()
        {
            _behaviourCoroutine = StartCoroutine(BehaviourCoroutine());
        }

        private IEnumerator BehaviourCoroutine()
        {
            while (true)
            {
                if (TrySpawnEvent())
                {
                    yield return new WaitForSeconds(Random.Range(_updateIntervalRange.x, _updateIntervalRange.y));
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(_updateIntervalRange.x / 2, _updateIntervalRange.y / 2));
                }
            }
        }

        private void OnDestroy()
        {
            StopCoroutine(_behaviourCoroutine);
        }

        private bool TrySpawnEvent()
        {
            switch (Random.Range(0, 2))
            {
                case 0: return TrySpawnZombie();
                case 1: return TrySpawnItem();
            }

            return false;
        }

        private bool TrySpawnZombie()
        {
            if (GameManager.Instance.Roads.Count > 0 &&
                GameManager.Instance.Zombies.Count < MaxZombiesCount)
            {
                // var playerPosition = PlayerCharacter.Instance.transform.position;
                var randomRoad = GameManager.Instance.GetRoadSuitableForNewZombie(4, _zombieSpawnRadiusRange);

                if (randomRoad == null)
                {
                    // nie znaleziono prawidłowej drogi
                    // Debug.Log("Tried to spawn a zombie, but couldn't find a suitable road.'");
                    return false;
                }

                var spawnedGameObject = Instantiate(ZombiePrefab, randomRoad.GetRandomPoint(), Quaternion.identity);
                var spawnedZombie = spawnedGameObject.GetComponent<Zombie>();

                spawnedZombie.OriginRoad = randomRoad;

                return true;
            }

            return false;
        }
        
        private bool TrySpawnItem()
        {
            if (GameManager.Instance.Roads.Count > 0 &&
                GameManager.Instance.Items.Count < MaxItemsCount)
            {
                var randomPosition = GameManager.Instance.GetRandomPositionForItemSpawn(_itemSpawnRadiusRange);
                if (!randomPosition.HasValue)
                {
                    return false;
                }
                
                if (GameManager.Instance.GetMinDistanceFromItems(randomPosition.Value) < 5)
                {
                    return false;
                }

                GameManager.Instance.SpawnRandomItem(randomPosition.Value);

                return true;
            }
            
            return false;
        }
    }
}