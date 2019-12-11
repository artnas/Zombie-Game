using System;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Enemy.States
{
    public class PatrolPath : ZombieState
    {
        public class PatrolPathArguments
        {
            public Action OnFinishedCallback;
            public bool StartOnClosestPoint;
            public bool ReversePath;
            public bool RepeatReversedPathWhenFinished;
        }

        private PatrolPathArguments _arguments;
        private Path _path;

        private Vector3 _target;
        private int _currentTargetIndex;
        
        public PatrolPath(Zombie zombie) : base(zombie)
        {
        }

        public override void OnTransitionIn(object argument, ZombieState previousState)
        {
            base.OnTransitionIn(argument, previousState);

            _arguments = argument as PatrolPathArguments;
            _path = Zombie.OriginRoad;

            OnPathBegan();
        }

        public override void Update()
        {
            var hasReachedPathNode = Zombie.MoveTowards(_target);

            if (hasReachedPathNode)
            {
                if (!_arguments.ReversePath)
                {
                    _currentTargetIndex++;
                    if (_currentTargetIndex >= _path.Points.Count)
                    {
                        OnPathFinished();
                    }
                    else
                    {
                        _target = _path.Points[_currentTargetIndex];
                    }
                }
                else
                {
                    _currentTargetIndex--;
                    if (_currentTargetIndex < 0)
                    {
                        OnPathFinished();
                    }
                    else
                    {
                        _target = _path.Points[_currentTargetIndex];
                    }
                }
            }
        }

        private void OnPathBegan()
        {
            _target = _arguments.ReversePath ? _path.Points.Last() : _path.Points.First();
            _currentTargetIndex = _arguments.ReversePath ? _path.Points.Count - 1 : 0;
            
            if (_arguments.StartOnClosestPoint)
            {
                var minDistance = float.MaxValue;
                for (var i = 0; i < _path.Points.Count; i++)
                {
                    var distanceToPoint = Vector3.Distance(Zombie.transform.position, _path.Points[i]);

                    if (distanceToPoint < minDistance)
                    {
                        minDistance = distanceToPoint;
                        _currentTargetIndex = i;
                        _target = _path.Points[i];
                    }
                }
            }
        }

        private void OnPathFinished()
        {
            _arguments.OnFinishedCallback?.Invoke();

            if (_arguments.RepeatReversedPathWhenFinished)
            {
                _arguments.ReversePath = !_arguments.ReversePath;
                OnPathBegan();
            }
            else
            {
                Zombie.ChangeState("Idle", null);
            }
        }
        
        public override void OnDrawGizmosSelected()
        {
            Zombie.OriginRoad?.DebugDraw();
        }
    }
}