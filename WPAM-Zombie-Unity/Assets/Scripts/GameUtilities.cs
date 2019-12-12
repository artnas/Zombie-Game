using System;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public static class GameUtilities
    {
        public static string GetSupplyTime(float supply)
        {
            var future = DateTime.Now.AddDays(supply);
            var timespan = future - DateTime.Now;

            if (timespan.Days > 1)
            {
                return $"około {timespan.Days} dni";
            }
            else if (timespan.Hours > 1)
            {
                return $"około {timespan.Hours} dni";
            }
            else if (timespan.Minutes > 5)
            {
                return $"około {timespan.Minutes} minut";
            }
            else if (timespan.TotalSeconds > 0)
            {
                return "resztki";
            }
            else
            {
                return "brak";
            }
        }

        public static void EndGame(string description)
        {
            GameManager.Instance.DeleteGameState();
            DeathScreen.DeathDescriptionText = description;
            SceneManager.LoadScene("DeathScreen");
        }
    }
}