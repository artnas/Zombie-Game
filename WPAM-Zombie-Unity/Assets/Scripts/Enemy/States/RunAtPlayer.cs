using DefaultNamespace;
using UnityEngine;

namespace Enemy.States
{
    public class RunAtPlayer : ZombieState
    {
        private bool _isQueryInProgress = false;
        private float _lastAttackTime;
        private readonly float _attackInterval = 2f;
        
        public RunAtPlayer(Zombie zombie) : base(zombie)
        {
        }

        public override void Update()
        {
            var target = PlayerCharacter.Instance.transform.position;
            var distance = Vector3.Distance(Zombie.transform.position, target);

            if (distance >= Zombie.RunAtPlayerRadius + 0.5f)
            {
                // idź w ostatnie miejsce, gdzie był gracz
                
                if (!IsQueryInProgress)
                {
                    DoNavigationQuery(target, path =>
                    {
                        Zombie.ChangeState("FollowPath", new FollowPath.FollowPathArguments
                        {
                            Path = path,
                            StartOnClosestPoint = true,
                            OnFinishedCallback = () => { Zombie.ChangeState("Idle", null); }
                        });
                    }, () => { });
                }
            } else if (distance < 2f)
            {
                if (Time.time - _lastAttackTime > _attackInterval)
                {
                    _lastAttackTime = Time.time;
                    GameManager.Instance.GameState.Player.Health--;

                    if (GameManager.Instance.GameState.Player.Health <= 0)
                    {
                        GameUtilities.EndGame("Zostałeś zabity przez zombie.");
                    }
                }
            }
            else
            {
                Zombie.MoveTowards(target);
            }
        }

        public override void OnDrawGizmosSelected()
        {
            Debug.DrawLine(Zombie.transform.position, PlayerCharacter.Instance.transform.position, Color.yellow, 0f);
        }
    }
}