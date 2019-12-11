using UnityEngine;

namespace Enemy.States
{
    public class FollowPlayer : ZombieState
    {
        public FollowPlayer(Zombie zombie) : base(zombie)
        {
        }

        public override void Update()
        {
            Zombie.Animation.Play("Zombie-1-Idle");
            
            if (!IsQueryInProgress)
            {
                DoNavigationQuery(PlayerCharacter.Instance.transform.position, path =>
                {
                    Zombie.ChangeState("FollowPath", new FollowPath.FollowPathArguments
                    {
                        Path = path,
                        StartOnClosestPoint = true,
                        OnFinishedCallback = () =>
                        {
                            var distance = Vector3.Distance(Zombie.transform.position, PlayerCharacter.Instance.transform.position);
                            
                            if (Zombie.IsDebug)
                            {
                                Debug.Log($"{GetName()}: distance from player: {distance}");
                            }

                            if (distance <= Zombie.VisionRadius)
                            {
                                Zombie.ChangeState("FollowPlayer", null);
                            }
                            else
                            {
                                Zombie.ChangeState("Idle", null);
                            }
                        }
                    });
                }, () =>
                {
                    Zombie.ChangeState("Idle", null);
                });
            }
        }
    }
}