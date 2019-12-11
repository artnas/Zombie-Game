using System;
using DefaultNamespace;

namespace Enemy.States
{
    public class Idle : ZombieState
    {

        public Idle(Zombie zombie) : base(zombie)
        {
        }
        public override void Update()
        {
            Zombie.Animation.Play("Zombie-1-Idle");
            
            if (!IsQueryInProgress)
            {
                DoNavigationQuery(Zombie.OriginRoad.GetClosestNode(Zombie.transform.position).Item2, path =>
                {
                    Zombie.ChangeState("FollowPath", new FollowPath.FollowPathArguments
                    {
                        Path = path,
                        StartOnClosestPoint = true,
                        OnFinishedCallback = () =>
                        {
                            Zombie.ChangeState("PatrolPath", new PatrolPath.PatrolPathArguments
                            {
                                StartOnClosestPoint = true,
                                RepeatReversedPathWhenFinished = true
                            });
                        }
                    });
                }, () => { });
            }
        }

        public override void OnTransitionIn(object argument, ZombieState previousState)
        {
            base.OnTransitionIn(argument, previousState);
        }
    }
}