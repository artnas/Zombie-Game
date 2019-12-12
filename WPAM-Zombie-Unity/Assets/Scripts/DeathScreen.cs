using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class DeathScreen : MonoBehaviour
    {
        public static string DeathDescriptionText;
        public Text DeathCauseText;

        private void Start()
        {
            DeathCauseText.text = DeathDescriptionText;
        }

        public void Replay()
        {
            SceneManager.LoadScene("AstronautGame");
        }
    }
}