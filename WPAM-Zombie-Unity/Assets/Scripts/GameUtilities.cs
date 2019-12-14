using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public static class GameUtilities
    {
        public static string GetSupplyTime(float supply)
        {
            var future = DateTime.Now.AddDays(supply);
            var timespan = future - DateTime.Now;

            if (timespan.TotalDays > 2)
            {
                return $"około {Mathf.RoundToInt((float)timespan.TotalDays)} dni";
            }
            else if (timespan.TotalHours > 1)
            {
                return $"około {Mathf.RoundToInt((float)timespan.TotalHours)} godzin";
            }
            else if (timespan.TotalMinutes > 5)
            {
                return $"około {Mathf.RoundToInt((float)timespan.TotalMinutes)} minut";
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