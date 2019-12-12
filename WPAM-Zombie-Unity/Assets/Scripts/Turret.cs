using System;
using System.Collections;
using Enemy;
using UnityEngine;

namespace DefaultNamespace
{
    public class Turret : MonoBehaviour
    {
        private float _shootingRange = 10;
        private Zombie _target;
        public LineRenderer ShootLineRenderer;
        public Transform CannonTransform;
        private Coroutine _shootCoroutine;
        
        private void Update()
        {
            if (!_target)
            {
                Spin();
                LookForTargets();
            }
            else if (_target.IsAlive)
            {
                FollowTarget();    
            }
        }

        private void Spin()
        {
            CannonTransform.rotation =
                Quaternion.Lerp(CannonTransform.rotation, Quaternion.Euler(0, CannonTransform.rotation.eulerAngles.y, 0), Time.deltaTime * 2f);
            
            CannonTransform.Rotate(0, Time.deltaTime * 10f, 0);
        }

        private void FollowTarget()
        {
            var fromToVector = _target.transform.position - CannonTransform.position;
            fromToVector.y = 0;
            
            var rotation = Quaternion.LookRotation(fromToVector, Vector3.up);

            if (Quaternion.Angle(CannonTransform.rotation, rotation) > 1f)
            {
                CannonTransform.rotation = Quaternion.Lerp(CannonTransform.rotation, rotation, Time.deltaTime * 25f);
            }
            else if (_shootCoroutine == null)
            {
                CannonTransform.rotation = rotation;
                _shootCoroutine = StartCoroutine(Shoot(_target));
            }
        }

        private void LookForTargets()
        {
            var minDistance = float.MaxValue;
            Zombie closestZombie = null;

            foreach (var zombie in GameManager.Instance.Zombies)
            {
                if (!zombie.IsAlive) continue;

                var distance = Vector3.Distance(zombie.transform.position, transform.position);

                if (distance <= _shootingRange && distance < minDistance)
                {
                    minDistance = distance;
                    closestZombie = zombie;
                }
            }

            if (closestZombie)
            {
                _target = closestZombie;
            }
        }
        
        private IEnumerator Shoot(Zombie zombie)
        {
            var rotation = Quaternion.LookRotation(zombie.transform.position - CannonTransform.position, Vector3.up);

            var fromToVector = rotation * Vector3.forward;

            ShootLineRenderer.gameObject.SetActive(true);
            ShootLineRenderer.SetPositions(new[]{CannonTransform.position + Vector3.up * 0.5f + fromToVector * 1, zombie.transform.position + Vector3.up * 4f});

            yield return new WaitForSeconds(0.1f);

            ShootLineRenderer.gameObject.SetActive(false);
        
            zombie.Die();
            _target = null;
            
            yield return new WaitForSeconds(2f);

            _shootCoroutine = null;
        }
    }
}