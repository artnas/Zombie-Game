using System;
using DefaultNamespace;
using UnityEngine;

namespace Enemy.States
{
    public class ZombieState
    {
        protected Zombie Zombie;
        protected bool IsQueryInProgress = false;
        private string _name;
        
        public ZombieState(Zombie zombie)
        {
            Zombie = zombie;
        }

        public string GetName()
        {
            return _name ?? (_name = GetType().Name);
        }

        public virtual void Update()
        {
            
        }

        public virtual void OnTransitionIn(object argument, ZombieState previousState)
        {
            if (Zombie.IsDebug)
            {
                Debug.Log($"{GetName()}: On transition in");
            }
        }

        public virtual void OnTransitionOut(object argument, ZombieState nextState)
        {
            if (Zombie.IsDebug)
            {
                Debug.Log($"{GetName()}: On transition out");
            }
        }

        public virtual void OnDrawGizmosSelected()
        {
            
        }
        
        protected void DoNavigationQuery(Vector3 target, Action<Path> onSuccess, Action onFailure)
        {
            var zombiePosition = Zombie.transform.position;
            
            IsQueryInProgress = true;
            NavigationUtilities.Query(zombiePosition, target, GameManager.Instance.Map, list =>
            {
                IsQueryInProgress = false;
                if (Zombie.CurrentState == this)
                {
                    if (list != null)
                    {
                        var path = new Path(list, null);
                        onSuccess(path);
                    }
                    else
                    {
                        onFailure();
                    }
                }
            });
        }
    }
}