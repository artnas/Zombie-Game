using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Enemy.States;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy
{
    public class Zombie : MonoBehaviour
    {
        public bool IsDebug = false;
        public bool IsAlive = true;
        
        public Path OriginRoad;
        public bool IsFollowingPlayer = false;

        private readonly float _speed = 1;
        private bool _reversePathDirection = false;

        private string _coroutineType = "";
        public Animation Animation;
        public Transform VisionRadiusTransform;

        public float DespawnRadius = 60;
        public float VisionRadius = 12;
        public float RunAtPlayerRadius = 8;

        private Dictionary<string, ZombieState> _states;
        public ZombieState CurrentState;

        public GameObject DeathParticlesPrefab;
        
        public void Start()
        {
            Animation = GetComponent<Animation>();
            VisionRadiusTransform = transform.GetChild(1);

            VisionRadiusTransform.SetParent(null);
            VisionRadiusTransform.localScale = Vector3.one * (VisionRadius * 2f);
            VisionRadiusTransform.SetParent(transform);
            
            GameManager.Instance.Zombies.Add(this);

            _states = ZombieUtilities.GetStatesDictionary(new List<ZombieState>(new ZombieState[]
            {
                new Idle(this), 
                new PatrolPath(this),
                new FollowPath(this),
                new RunAtPlayer(this), 
                new FollowPlayer(this), 
            }));
            ChangeState("Idle", null);
        }

        private void OnDestroy()
        {
            GameManager.Instance.Zombies.Remove(this);
        }

        private void Update()
        {
            if (!IsAlive) return;
            
            CurrentState.Update();
            
            var distanceToPlayer = Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position);
            var playerInVisionRadius = distanceToPlayer <= VisionRadius;

            if (playerInVisionRadius)
            {
                var playerInRunAtPlayerRadius = distanceToPlayer <= RunAtPlayerRadius;

                if (playerInRunAtPlayerRadius)
                {
                    if (CurrentState.GetName() != "RunAtPlayer")
                    {
                        ChangeState("RunAtPlayer", null);
                    }
                }
                else
                {
                    if (CurrentState.GetName() != "FollowPlayer" && CurrentState.GetName() != "FollowPath")
                    {
                        ChangeState("FollowPlayer", null);
                    }
                }
            }

            if (distanceToPlayer > DespawnRadius)
            {
                // despawn
                Destroy(gameObject);
            }
        }

        public void Die()
        {
            CurrentState = null;
            IsAlive = false;
            StartCoroutine(Death());
        }

        private IEnumerator Death()
        {
            Animation.Play("Zombie-1-Death");
            
            if (Random.value < 0.25f)
            {
                GameManager.Instance.SpawnRandomItem(transform.position);
            }
            
            yield return new WaitForSeconds(0.5f);

            Instantiate(DeathParticlesPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        public bool MoveTowards(Vector3 target)
        {
            var forwardVector = (target - transform.position).normalized;
            forwardVector.y = 0;

            var targetRotation = forwardVector != Vector3.zero ? Quaternion.LookRotation(forwardVector, Vector3.up) : Quaternion.identity;
            
            // Debug.Log(Quaternion.Angle(transform.rotation, targetRotation));

            var angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle > 5f)
            {
                // rotate

                Animation.Play("Zombie-1-Idle");

                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 25f);

                return false;
            }
            else
            {
                // move

                Animation.Play("Zombie-1-Walk");
                
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 25f);
                
                var distanceToTarget = Vector3.Distance(transform.position, target);
                var movementVector = forwardVector * Time.deltaTime * _speed;
                
                // Debug.Log($"movement vector magnitude: {movementVector.magnitude}, distance to target: {distanceToTarget}");

                if (movementVector.magnitude >= distanceToTarget)
                {
                    transform.position = target;
                    return true;
                }
                else
                {
                    transform.position += movementVector;
                    return false;
                }
            }
        }

        public void ChangeState(ZombieState newState, object argument)
        {
            var previousState = CurrentState;
            previousState?.OnTransitionOut(argument, newState);

            CurrentState = newState;
            newState.OnTransitionIn(argument, previousState);
        }

        public void ChangeState(string stateName, object argument)
        {
            ChangeState(_states[stateName], argument);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (CurrentState != null)
            {
                CurrentState.OnDrawGizmosSelected();
            }
            else
            {
                OriginRoad?.DebugDraw();
            }
        }
    }
}