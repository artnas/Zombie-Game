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
        public bool IsDebug = false;
        public bool IsAlive = true;
        
        public Path OriginRoad;
        public bool IsFollowingPlayer = false;

        private readonly float _speed = 1;
        private bool _reversePathDirection = false;

        private string _coroutineType = "";
        private Coroutine _actionCoroutine;
        public Animation Animation;
        public Transform VisionRadiusTransform;

        public float DespawnRadius = 60;
        public float VisionRadius = 12;
        public float RunAtPlayerRadius = 8;

        private Path _currentPath;
        
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
            if (!IsAlive) return;

            var vectorToPlayer = (PlayerCharacter.Instance.transform.position - transform.position).normalized;
            // czy gracz jest w zasięgu
            var distanceToPlayer = Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position);
            var playerInVisionRadius = distanceToPlayer <= VisionRadius;

            if (distanceToPlayer > DespawnRadius)
            {
                // Despawn
                Destroy(gameObject);
                return;
            }
            
            if (IsDebug) Debug.Log($"Distance to player: {distanceToPlayer}");
            
            if (IsFollowingPlayer)
            {
                if (IsDebug) Debug.Log("IsFollowingPlayer == true");
                
                if (!playerInVisionRadius)
                {
                    if (IsDebug) Debug.Log("playerInVisionRadius == false");
                    
                    var isZombieFarEnoughFromPlayerToLoseTarget = !(distanceToPlayer <= VisionRadius * 3f);
                    if (IsDebug) Debug.Log($"isZombieFarEnoughFromPlayerToLoseTarget == {isZombieFarEnoughFromPlayerToLoseTarget}");

                    if (isZombieFarEnoughFromPlayerToLoseTarget)
                    {
                        // TODO zombie straciło cel, dodać tutaj coś specjalnego?
                        IsFollowingPlayer = false;
                        if (IsDebug) Debug.Log("GoToClosestPointOnPatrolPath");

                        if (!_queryInProgress)
                        {
                            GoToClosestPointOnPatrolPath();
                        }
                    }
                }
                else
                {
                    if (IsDebug) Debug.Log("playerInVisionRadius == true");
                    
                    if (distanceToPlayer < RunAtPlayerRadius)
                    {
                        if (IsDebug) Debug.Log("player in RunAtPlayerRadius");
                        if (_actionCoroutine != null)
                        {
                            StopCoroutine(_actionCoroutine);
                            _actionCoroutine = null;
                        }
                        RunAtPlayer();
                    }
                    else if (_actionCoroutine == null)
                    {
                        if (IsDebug) Debug.Log("_actionCoroutine is null");
                        
                        var isZombieFarEnoughToRepath = distanceToPlayer >= VisionRadius * 1.5f;
                        if (IsDebug) Debug.Log($"isZombieFarEnoughToRepath == {isZombieFarEnoughToRepath}");

                        if (isZombieFarEnoughToRepath)
                        {
                            if (IsDebug) Debug.Log("GetPathAndNavigate to Player");
                            GetPathAndNavigate(PlayerCharacter.Instance.transform.position);
                        }
                        else
                        {
                            if (IsDebug) Debug.Log("Starting Follow Path");

                            if (_actionCoroutine != null)
                            {
                                StopCoroutine(_actionCoroutine);
                                _actionCoroutine = null;
                            }
                            _actionCoroutine = StartCoroutine(FollowPath(new Path(new List<Vector3>(new []{transform.position + vectorToPlayer, PlayerCharacter.Instance.transform.position}), null), false, true, false));
                        }
                    }
                    else
                    {
                        if (IsDebug) Debug.Log("Doing a coroutine");
                    }
                }
            } 
            else
            {
                if (playerInVisionRadius)
                {
                    // zacznij podążać za graczem
                    GetPathAndNavigate(PlayerCharacter.Instance.transform.position);
                    IsFollowingPlayer = true;
                }
                else if (_actionCoroutine == null)
                {
                    if (!_queryInProgress)
                    {
                        GoToClosestPointOnPatrolPath();
                    }
                }
            }
        }

        private IEnumerator FollowPath(Path path, bool reversed = false, bool startOnClosestPoint = false, bool repeatReversedPath = true, Action callback = null)
        {
            _coroutineType = "FollowPath";
            if (IsDebug) Debug.Log($"Coroutine: Follow Path ({path.Points.Count})");
            
            _currentPath = path;
            
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
                if (IsDebug) Debug.Log("Coroutine: Follow Path: hasReachedTarget == false");
                
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
            
            callback?.Invoke();

            _actionCoroutine = repeatReversedPath ? StartCoroutine(FollowPath(path, !reversed, true)) : null;
        }

        private void RunAtPlayer()
        {
            if (IsDebug) Debug.Log("RunAtPlayer");
            
            var forwardVector = (PlayerCharacter.Instance.transform.position - transform.position).normalized;
            forwardVector.y = 0;

            var targetRotation = forwardVector != Vector3.zero ? Quaternion.LookRotation(forwardVector, Vector3.up) : Quaternion.identity;

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10);
            
            if (IsDebug) Debug.DrawLine(transform.position, PlayerCharacter.Instance.transform.position, Color.red, 0f);
            if (IsDebug) Debug.DrawRay(transform.position, forwardVector, Color.yellow, 0f);
            
            transform.position += _speed * Time.deltaTime * forwardVector;
        }

        public void Die()
        {
            IsAlive = false;
            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
                _actionCoroutine = null;
            }
            _actionCoroutine = StartCoroutine(Death());
        }

        private IEnumerator Death()
        {
            _coroutineType = "Death";
            Animation.Play("Zombie-1-Death");
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        private void GoToClosestPointOnPatrolPath()
        {
            // go back to origin
            var minDistance = float.MaxValue;
            
            if (IsDebug) Debug.Log($"GoToClosestPointOnPatrolPath, road exists: {OriginRoad != null}");
            
            var closestPointOnPatrolPath = OriginRoad.Points.First();

            foreach (var point in OriginRoad.Points)
            {
                var distance = Vector3.Distance(point, closestPointOnPatrolPath);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPointOnPatrolPath = point;
                }
            }

            if (!_queryInProgress)
            {
                if (_actionCoroutine != null)
                {
                    StopCoroutine(_actionCoroutine);
                    _actionCoroutine = null;
                }
                _actionCoroutine = StartCoroutine(Wait());
                
                GetPathAndNavigate(closestPointOnPatrolPath, () =>
                {
                    if (_actionCoroutine != null)
                    {
                        StopCoroutine(_actionCoroutine);
                        _actionCoroutine = null;
                    }
                    _actionCoroutine = StartCoroutine(FollowPath(OriginRoad, false, true));
                });
            }
        }

        private bool _queryInProgress = false;
        private void GetPathAndNavigate(Vector3 target, Action customCallback = null)
        {
            _queryInProgress = true;
            if (IsDebug) Debug.Log($"Navigation query: {transform.position} -> {target}");
            NavigationUtilities.Query(transform.position, target, GameManager.Instance.Map, list =>
            {
                if (IsDebug) Debug.Log($"Navigation query completed: {transform.position} -> {target}");
                if (IsDebug) Debug.Log($"Navigation query completed, list exists: {list != null}");
                _queryInProgress = false;
                if (list != null)
                {
                    if (_actionCoroutine != null)
                    {
                        StopCoroutine(_actionCoroutine);
                        _actionCoroutine = null;
                    }

                    if (IsDebug)
                    {
                        foreach (var point in list)
                        {
                            Debug.Log(point);
                        }
                    }

                    var path = new Path(list, null);
                    // path.Simplify();

                    if (_actionCoroutine != null)
                    {
                        StopCoroutine(_actionCoroutine);
                        _actionCoroutine = null;
                    }

                    var callback = customCallback ?? (() =>
                    {
                        var playerInRange =
                            Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position) <
                            VisionRadius;

                        if (playerInRange)
                        {
                            // repath to player
                            if (IsDebug) Debug.Log($"Zombie reached last player position: Repath");
                            GetPathAndNavigate(PlayerCharacter.Instance.transform.position);
                        }
                        else
                        {
                            if (IsDebug) Debug.Log($"Zombie reached last player position: Go back to patrol path");
                            GoToClosestPointOnPatrolPath();
                        }
                    });

                    if (IsAlive)
                    {
                        if (_actionCoroutine != null)
                        {
                            StopCoroutine(_actionCoroutine);
                            _actionCoroutine = null;
                        }
                        _actionCoroutine = StartCoroutine(FollowPath(path, false, false, false, callback));
                    }
                }
                else
                {
                    GetPathAndNavigate(target);
                }
            });
        }

        private IEnumerator Wait()
        {
            _coroutineType = "Wait";
            yield return new WaitForSeconds(5);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_currentPath != null)
            {
                _currentPath?.DebugDraw();
            }
            else
            {
                OriginRoad?.DebugDraw();
            }
        }
    }
}