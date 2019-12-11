using System;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Enemy.States
{
    public class FollowPath : ZombieState
    {
        public class FollowPathArguments
        {
            public Action OnFinishedCallback;
            public Action OnSpottedPlayerCallback;
            public bool StartOnClosestPoint;
            public Path Path;
        }

        private FollowPathArguments _arguments;

        private Vector3 _target;
        private int _currentTargetIndex;
        
        public FollowPath(Zombie zombie) : base(zombie)
        {
        }

        public override void OnTransitionIn(object argument, ZombieState previousState)
        {
            base.OnTransitionIn(argument, previousState);
            
            _arguments = argument as FollowPathArguments;
            OnPathBegan();
        }

        public override void Update()
        {
            var hasReachedTarget = Zombie.MoveTowards(_target);

            if (hasReachedTarget)
            {
                _currentTargetIndex++;

                if (_currentTargetIndex == _arguments.Path.Points.Count)
                {
                    OnPathFinished();
                    return;
                }
                else
                {
                    _target = _arguments.Path.Points[_currentTargetIndex];
                }
            }
            
            var distanceFromPlayer =
                Vector3.Distance(PlayerCharacter.Instance.transform.position, Zombie.transform.position);

            if (distanceFromPlayer <= Zombie.VisionRadius)
            {
                _arguments.OnSpottedPlayerCallback?.Invoke();
            }
        }
        
        private void OnPathBegan()
        {
            _target = _arguments.Path.Points.First();
            _currentTargetIndex = 0;
            
            if (_arguments.StartOnClosestPoint)
            {
                var minDistance = float.MaxValue;
                for (var i = 0; i < _arguments.Path.Points.Count; i++)
                {
                    var distanceToPoint = Vector3.Distance(Zombie.transform.position, _arguments.Path.Points[i]);

                    if (distanceToPoint < minDistance)
                    {
                        minDistance = distanceToPoint;
                        _currentTargetIndex = i;
                        _target = _arguments.Path.Points[i];
                    }
                }
            }
        }

        private void OnPathFinished()
        {
            _arguments.OnFinishedCallback?.Invoke();
        }
        
        public override void OnDrawGizmosSelected()
        {
            _arguments.Path?.DebugDraw();
        }
    }
}