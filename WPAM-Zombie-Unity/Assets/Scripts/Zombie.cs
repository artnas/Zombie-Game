using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace DefaultNamespace
{
    public class Zombie : MonoBehaviour
    {
        public bool IsAlive = true;
        
        public Path OriginRoad;
        public bool IsFollowingPlayer = false;

        private readonly float _speed = 1;
        private bool _reversePathDirection = false;

        private Coroutine _actionCoroutine;
        public Animation Animation;
        public Transform VisionRadiusTransform;

        public float VisionRadius = 10;
        
        public void Start()
        {
            Animation = GetComponent<Animation>();
            VisionRadiusTransform = transform.GetChild(1);

            VisionRadiusTransform.SetParent(null);
            VisionRadiusTransform.localScale = Vector3.one * (VisionRadius * 2f);
            VisionRadiusTransform.SetParent(transform);
            
            GameManager.Instance.Zombies.Add(this);

            _actionCoroutine = StartCoroutine(FollowPath(OriginRoad, false, true));
        }

        private void OnDestroy()
        {
            GameManager.Instance.Zombies.Remove(this);
        }

        private void Update()
        {
            // czy gracz jest w zasięgu
            var playerInRange = Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position) <=
                                VisionRadius;
            
            if (!IsFollowingPlayer)
            {
                if (playerInRange)
                {
                    // zacznij podążać za graczem
                    GetPathAndNavigate(PlayerCharacter.Instance.transform.position);
                    IsFollowingPlayer = true;
                }
            } 
            else
            {
                if (!playerInRange)
                {
                    StopCoroutine(_actionCoroutine);
                }
            }
        }

        private IEnumerator FollowPath(Path path, bool reversed = false, bool startOnClosestPoint = false)
        {
            var target = reversed ? path.Points.Last() : path.Points.First();
            var currentTargetIndex = reversed ? path.Points.Count - 1 : 0;
            var hasReachedTarget = false;

            if (startOnClosestPoint)
            {
                var minDistance = float.MaxValue;
                for (var i = 0; i < path.Points.Count; i++)
                {
                    var distanceToPoint = Vector3.Distance(transform.position, path.Points[i]);

                    if (distanceToPoint < minDistance)
                    {
                        minDistance = distanceToPoint;
                        currentTargetIndex = i;
                        target = path.Points[i];
                    }
                }
            }

            while (!hasReachedTarget)
            {
                var forwardVector = (target - transform.position).normalized;
                forwardVector.y = 0;

                var targetRotation = forwardVector != Vector3.zero ? Quaternion.LookRotation(forwardVector, Vector3.up) : Quaternion.identity;
                
                // Debug.Log(Quaternion.Angle(transform.rotation, targetRotation));

                if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                {
                    // rotate
                    
                    Animation.Play("Zombie-1-Idle");

                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                }
                else
                {
                    // move
                    
                    Animation.Play("Zombie-1-Walk");
                    
                    Debug.DrawRay(transform.position, forwardVector, Color.yellow, 0);

                    transform.rotation = targetRotation;
                    
                    var distanceToTarget = Vector3.Distance(transform.position, target);
                    var movementVector = forwardVector * Time.deltaTime * _speed;

                    if (movementVector.magnitude >= distanceToTarget)
                    {
                        transform.position = target;

                        if (!reversed)
                        {
                            currentTargetIndex++;

                            if (currentTargetIndex == path.Points.Count)
                            {
                                hasReachedTarget = true;
                            }
                            else
                            {
                                target = path.Points[currentTargetIndex];
                            }
                        }
                        else
                        {
                            currentTargetIndex--;

                            if (currentTargetIndex == -1)
                            {
                                hasReachedTarget = true;
                            }
                            else
                            {
                                target = path.Points[currentTargetIndex];
                            }
                        }
                    }
                    else
                    {
                        transform.position += movementVector;
                    }
                }

                yield return null;
            }

            StartCoroutine(FollowPath(path, !reversed, true));
        }

        public void Die()
        {
            IsAlive = false;
            StopAllCoroutines();
            _actionCoroutine = StartCoroutine(Death());
        }

        private IEnumerator Death()
        {
            Animation.Play("Zombie-1-Death");
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        private void GetPathAndNavigate(Vector3 target)
        {
            NavigationUtilities.Query(transform.position, target, GameManager.Instance.Map, list =>
            {
                if (list != null)
                {
                    Debug.Log(list.Count);
                    StopCoroutine(_actionCoroutine);
                    _actionCoroutine = StartCoroutine(FollowPath(new Path(list, null)));
                }
                else
                {
                    GetPathAndNavigate(target);
                }
            });
        }

        private void OnDrawGizmosSelected()
        {
            OriginRoad?.DebugDraw();
        }
    }
}