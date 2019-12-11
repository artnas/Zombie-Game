using UnityEngine;

namespace Enemy.States
{
    public class RunAtPlayer : ZombieState
    {
        private bool _isQueryInProgress = false;
        
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
                // atak?
                
                Debug.Log("TODO Attack");
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