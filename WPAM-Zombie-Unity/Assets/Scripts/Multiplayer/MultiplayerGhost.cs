using System;
using UnityEngine;

namespace DefaultNamespace.Multiplayer
{
    public class MultiplayerGhost : MonoBehaviour
    {
        private Animation _animation;
        public Vector3 DesiredPosition;

        private void Start()
        {
            _animation = GetComponent<Animation>();
        }

        private void Update()
        {
            var distance = Vector3.Distance(transform.position, DesiredPosition);
            transform.position = Vector3.Lerp(transform.position, DesiredPosition, Time.deltaTime * 5f);
            
            var directionVector = (DesiredPosition - transform.position).normalized;
            if (directionVector != Vector3.zero)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(directionVector, Vector3.up), Time.deltaTime * 15f);
            }
            
            if (distance < 0.1f)
            {
                _animation.Play("Player-Gun-Idle");
            }
            else
            {
                _animation.Play("Player-Gun-Walk");
            }
        }
    }
}